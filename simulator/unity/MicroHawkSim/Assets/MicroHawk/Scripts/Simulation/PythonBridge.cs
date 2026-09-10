using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MicroHawk.Contracts;
using MicroHawk.Transport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MicroHawk.Simulation
{
    public sealed class PythonBridge : MonoBehaviour
    {
        private IFlightOperations operations;
        private FlightRuntime runtime;
        private DroneRgbSensor sensor;
        private WireProtocol protocol;
        private long localSession;
        private string authority,token;
        private readonly ConcurrentQueue<string> incoming=new(),outgoing=new();
        private readonly Dictionary<long,string> ids=new();
        private readonly List<CommandResult> immediate=new();
        private bool submitting;
        private CancellationTokenSource stop;
        private volatile bool connected,lost,overflow;
        private string latestTelemetry;
        private double lastFrame=-100;
        public void Initialize(FlightRuntime flight,string credential)
        {
            runtime=flight;operations=flight;token=credential;
            sensor=gameObject.AddComponent<DroneRgbSensor>();sensor.Initialize();
            stop=new CancellationTokenSource();operations.CommandChanged+=OnOutcome;operations.TelemetryPublished+=OnTelemetry;
            NewSession();_=ConnectLoop(stop.Token);
        }
        private void NewSession()
        {
            localSession=operations.Telemetry.Session;protocol=new WireProtocol(Guid.NewGuid().ToString("N"));ids.Clear();authority=Guid.NewGuid().ToString("N");lastFrame=-100;
        }
        private void Send(JObject value)
        {
            if(outgoing.Count>=256){overflow=true;return;}
            outgoing.Enqueue(value.ToString(Formatting.None));
        }
        private JObject Handshake()=>new JObject{{"type","handshake"},{"schema_version",1},{"session_id",protocol.Session},{"authority_id",authority},{"current_tick",operations.Telemetry.Tick},{"simulator_version",Application.unityVersion},{"world_revision",WireProtocol.WorldRevision},{"coordinate_frame",TelemetrySnapshot.CoordinateFrame},{"capabilities",new JArray("Arm","Disarm","Takeoff","MoveTo","Hold","Land","ReturnHome","rgb_gimbal")}};
        private void Update()
        {
            if(operations.Telemetry.Session!=localSession){NewSession();Send(Handshake());}
            if(lost){lost=false;runtime.ConnectionLost();}
            for(int count=0;count<16&&incoming.TryDequeue(out var text);count++)
            {
                if(text=="CONNECTED"){authority=Guid.NewGuid().ToString("N");protocol.BeginAuthority();Send(Handshake());continue;}
                JObject request=null;
                try
                {
                    request=WireProtocol.Parse(text);
                    if((string)request["type"]=="heartbeat")continue;
                    if((string)request["type"]=="capture")
                    {
                        WireProtocol.Fields(request,"type","session_id","authority_id","request_id","look_at");
                        if((string)request["session_id"]!=protocol.Session||(string)request["authority_id"]!=authority)throw new ArgumentException("StaleAuthority");
                        if(operations.Telemetry.SimulationSeconds-lastFrame<.4)throw new ArgumentException("FrameRateLimit");
                        var v=request["look_at"] as JObject??throw new ArgumentException("InvalidLookAt");WireProtocol.Fields(v,"east","north","up");
                        var look=new FlightVector((double)v["east"],(double)v["north"],(double)v["up"]);if(!look.IsFinite||look.Length>150)throw new ArgumentException("InvalidLookAt");
                        lastFrame=operations.Telemetry.SimulationSeconds;Send(sensor.Capture(operations.Telemetry,protocol.Session,(string)request["request_id"],look));continue;
                    }
                    var command=protocol.Admit(request,operations.Telemetry.Tick,authority,out var replay);
                    if(replay!=null){Send(replay);continue;}
                    string id=(string)request["command_id"];Send(protocol.Outcome(id,operations.Telemetry.Tick,"Received","None"));
                    submitting=true;
                    try{long internalId=operations.Submit(command,localSession);ids[internalId]=id;}
                    finally{submitting=false;}
                    foreach(var early in immediate)OnOutcome(early);immediate.Clear();
                }
                catch(Exception e)
                {
                    string id=(string)request?["command_id"]??"invalid";
                    // Reject without overwriting a previous idempotent command result.
                    Send(new JObject{{"type","outcome"},{"schema_version",1},{"session_id",protocol.Session},{"command_id",id},{"tick",operations.Telemetry.Tick},{"status","Rejected"},{"reason",e.Message}});
                }
            }
        }
        private void OnOutcome(CommandResult r)
        {
            if(submitting){immediate.Add(r);return;}
            string id=ids.TryGetValue(r.Id,out var external)?external:"local-"+r.Id;
            if(!ids.ContainsKey(r.Id)&&r.Status==ExecutionStatus.Accepted&&r.Reason==ReasonCode.None)
            {authority=Guid.NewGuid().ToString("N");protocol.BeginAuthority();Send(Handshake());}
            Send(protocol.Outcome(id,r.Tick,r.Status.ToString(),r.Reason.ToString()));
        }
        private void OnTelemetry(TelemetrySnapshot t)
        {
            latestTelemetry=new JObject{{"type","telemetry"},{"schema_version",1},{"session_id",protocol.Session},{"tick",t.Tick},{"simulation_seconds",t.SimulationSeconds},{"position",DroneRgbSensor.Vector(t.Body.Position)},{"velocity",DroneRgbSensor.Vector(t.Body.Velocity)},{"heading",t.Body.Heading},{"altitude",t.Altitude},{"battery",t.BatteryPercent},{"state",t.State.ToString()},{"armed",t.Armed},{"command",t.CurrentCommand?.ToString()},{"target",t.Target.HasValue?DroneRgbSensor.Vector(t.Target.Value):null},{"home",DroneRgbSensor.Vector(t.Home)},{"safety_status",t.Safety.Status.ToString()},{"safety_reason",t.Safety.Reason.ToString()}}.ToString(Formatting.None);
        }
        private async Task ConnectLoop(CancellationToken cancel)
        {
            while(!cancel.IsCancellationRequested)
            {
                using var socket=new ClientWebSocket();socket.Options.SetRequestHeader("Authorization","Bearer "+token);
                using var connection=CancellationTokenSource.CreateLinkedTokenSource(cancel);
                try
                {
                    await socket.ConnectAsync(new Uri("ws://127.0.0.1:8765/simulator"),cancel);connected=true;incoming.Enqueue("CONNECTED");
                    var receive=Receive(socket,connection.Token);
                    while(socket.State==WebSocketState.Open&&!receive.IsCompleted&&!cancel.IsCancellationRequested&&!overflow)
                    {
                        while(outgoing.TryDequeue(out var message))await Write(socket,message,connection.Token);
                        var telemetry=Interlocked.Exchange(ref latestTelemetry,null);if(telemetry!=null)await Write(socket,telemetry,connection.Token);
                        await Task.Delay(20,connection.Token);
                    }
                    connection.Cancel();socket.Abort();try{await receive;}catch(OperationCanceledException){}
                }
                catch(Exception e) when(e is WebSocketException or OperationCanceledException or System.IO.IOException){}
                finally{if(connected){connected=false;lost=true;}connection.Cancel();socket.Abort();while(incoming.TryDequeue(out _)){}while(outgoing.TryDequeue(out _)){}overflow=false;}
                try{await Task.Delay(1500,cancel);}catch(OperationCanceledException){return;}
            }
        }
        private async Task Receive(ClientWebSocket socket,CancellationToken cancel)
        {
            byte[] buffer=new byte[16384];
            while(!cancel.IsCancellationRequested)
            {
                int count=0;WebSocketReceiveResult result;
                using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel);deadline.CancelAfter(5000);
                do{result=await socket.ReceiveAsync(new ArraySegment<byte>(buffer,count,buffer.Length-count),deadline.Token);count+=result.Count;if(result.MessageType!=WebSocketMessageType.Text||count>=buffer.Length)throw new System.IO.IOException("InvalidMessage");}while(!result.EndOfMessage);
                if(incoming.Count>=32)throw new System.IO.IOException("IngressOverflow");incoming.Enqueue(Encoding.UTF8.GetString(buffer,0,count));
            }
        }
        private static Task Write(ClientWebSocket socket,string text,CancellationToken cancel)=>socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),WebSocketMessageType.Text,true,cancel);
        private void OnDestroy(){stop?.Cancel();if(operations!=null){operations.CommandChanged-=OnOutcome;operations.TelemetryPublished-=OnTelemetry;}}
    }
}
