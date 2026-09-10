using System;
using System.Collections.Generic;
using System.Linq;
using MicroHawk.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicroHawk.Transport
{
    // No engine reference: strict ingress cannot call a Unity component.
    public sealed class WireProtocol
    {
        public const string WorldRevision="industrial-test-v1";
        public readonly string Session;
        private long sequence;
        private readonly Dictionary<string,(string payload,JObject result)> history=new();
        public WireProtocol(string session){Session=session;}
        public void BeginAuthority()=>sequence=0;
        public static JObject Parse(string text)
        {
            if(text==null||text.Length>16384)throw new ArgumentException("MessageTooLarge");
            using var reader=new JsonTextReader(new System.IO.StringReader(text)){MaxDepth=12,DateParseHandling=DateParseHandling.None};
            var value=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(reader.Read())throw new ArgumentException("TrailingData");
            return value;
        }
        public static void Fields(JObject value,params string[] names)
        {
            if(value.Properties().Any(p=>!names.Contains(p.Name)))throw new ArgumentException("UnknownField");
        }
        private static string Text(JObject o,string key)
        {
            if(o[key]?.Type!=JTokenType.String)throw new ArgumentException("InvalidField");
            return (string)o[key];
        }
        private static long Integer(JObject o,string key)
        {if(o[key]?.Type!=JTokenType.Integer)throw new ArgumentException("InvalidField");return (long)o[key];}
        private static double Number(JObject o,string key)
        {
            if(o[key]?.Type is not (JTokenType.Integer or JTokenType.Float))throw new ArgumentException("InvalidNumber");
            double v=(double)o[key];if(!FlightVector.Finite(v))throw new ArgumentException("NonFinite");return v;
        }
        public static FlightCommand Decode(JObject o)
        {
            Fields(o,"type","schema_version","session_id","authority_id","command_id","sequence","issued_at_tick","expires_at_tick","action_type","parameters");
            if(Text(o,"type")!="command"||Integer(o,"schema_version")!=1)throw new ArgumentException("SchemaVersion");
            foreach(var key in new[]{"session_id","authority_id","command_id"})if(Text(o,key).Length is <1 or >100)throw new ArgumentException("InvalidId");
            if(!System.Text.RegularExpressions.Regex.IsMatch(Text(o,"command_id"),"^[a-zA-Z0-9_-]{1,64}$"))throw new ArgumentException("InvalidId");
            long issued=Integer(o,"issued_at_tick"),expires=Integer(o,"expires_at_tick");
            if(Integer(o,"sequence")<1||issued<0||expires<=issued||expires-issued>1500)throw new ArgumentException("InvalidLifetime");
            var p=o["parameters"] as JObject??throw new ArgumentException("ParametersRequired");
            switch(Text(o,"action_type"))
            {
                case "Arm":Fields(p);return new ArmCommand();
                case "Disarm":Fields(p);return new DisarmCommand();
                case "Land":Fields(p);return new LandCommand();
                case "ReturnHome":Fields(p);return new ReturnHomeCommand();
                case "Takeoff":Fields(p,"altitude");return new TakeoffCommand(Number(p,"altitude"));
                case "Hold":Fields(p,"duration");return new HoldCommand(p["duration"]==null||p["duration"].Type==JTokenType.Null?null:Number(p,"duration"));
                case "MoveTo":
                    Fields(p,"position","speed");var v=p["position"] as JObject??throw new ArgumentException("PositionRequired");Fields(v,"east","north","up");
                    return new MoveToCommand(new FlightVector(Number(v,"east"),Number(v,"north"),Number(v,"up")),p["speed"]==null||p["speed"].Type==JTokenType.Null?null:Number(p,"speed"));
                default:throw new ArgumentException("UnknownAction");
            }
        }
        public FlightCommand Admit(JObject o,long tick,string authority,out JObject replay)
        {
            replay=null;var command=Decode(o);string id=(string)o["command_id"],payload=o.ToString(Formatting.None);
            if((string)o["session_id"]!=Session)throw new ArgumentException("StaleSession");
            if((string)o["authority_id"]!=authority)throw new ArgumentException("StaleAuthority");
            if(history.TryGetValue(id,out var old))
            {if(old.payload!=payload)throw new ArgumentException("IdConflict");replay=(JObject)old.result.DeepClone();return null;}
            if((long)o["expires_at_tick"]<=tick)throw new ArgumentException("Expired");
            if((long)o["issued_at_tick"]>tick)throw new ArgumentException("FutureTick");
            if((long)o["sequence"]<=sequence)throw new ArgumentException("StaleSequence");
            if(history.Count>=4096)throw new ArgumentException("SessionCapacity");
            sequence=(long)o["sequence"];history.Add(id,(payload,Outcome(id,tick,"Received","None")));return command;
        }
        public JObject Outcome(string id,long tick,string status,string reason)
        {
            var result=new JObject{{"type","outcome"},{"schema_version",1},{"session_id",Session},{"command_id",id},{"tick",tick},{"status",status},{"reason",reason}};
            if(history.TryGetValue(id,out var old))history[id]=(old.payload,result);
            return result;
        }
    }
}
