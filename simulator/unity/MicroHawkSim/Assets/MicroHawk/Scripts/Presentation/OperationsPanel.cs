using MicroHawk.Contracts;
using UnityEngine;

namespace MicroHawk.Presentation
{
    public sealed class OperationsPanel : MonoBehaviour
    {
        private IFlightOperations operations;
        private ISimulationReset reset;
        private CommandResult? last;
        private GUIStyle heading,label,status;
        public void Bind(IFlightOperations commands,ISimulationReset lifecycle)
        {operations=commands;reset=lifecycle;operations.CommandChanged+=OnResult;}
        private void OnDestroy(){if(operations!=null)operations.CommandChanged-=OnResult;}
        private void OnResult(CommandResult result)=>last=result;
        private void Send(FlightCommand command)=>operations.Submit(command,operations.Telemetry.Session);
        private void OnGUI()
        {
            var t=operations?.Telemetry;if(t==null)return;
            heading??=new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            label??=new GUIStyle(GUI.skin.label){fontSize=12,normal={textColor=new Color(.83f,.89f,.94f)}};
            status??=new GUIStyle(label){wordWrap=true};
            float x=Screen.width-340;
            GUI.Box(new Rect(x,18,322,620),GUIContent.none);
            GUILayout.BeginArea(new Rect(x+14,28,294,595));
            GUILayout.Label("MICROHAWK  /  FLIGHT",heading);
            GUILayout.Label($"STATE  {t.State}    ARMED  {t.Armed}",label);
            GUILayout.Label($"BATTERY  {t.BatteryPercent:F1}%    ALT  {t.Altitude:F2} m",label);
            GUILayout.Label($"ENU  {t.Body.Position}",label);
            GUILayout.Label($"VELOCITY  {t.Body.Velocity} m/s",label);
            GUILayout.Label($"YAW  {t.Body.Heading*180/System.Math.PI:F0}°",label);
            GUILayout.Label($"COMMAND  {t.CurrentCommand?.ToString()??"None"}",label);
            GUILayout.Label($"TARGET  {t.Target?.ToString()??"None"}",label);
            GUILayout.Label($"SAFETY  {t.Safety.Status} / {t.Safety.Reason}",status);
            GUILayout.Label($"SIM  {t.SimulationSeconds:F1}s    SESSION  {t.Session}",label);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();if(GUILayout.Button("ARM"))Send(new ArmCommand());if(GUILayout.Button("DISARM"))Send(new DisarmCommand());GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();if(GUILayout.Button("TAKEOFF 4m"))Send(new TakeoffCommand(4));if(GUILayout.Button("HOLD"))Send(new HoldCommand());GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();if(GUILayout.Button("LAND"))Send(new LandCommand());if(GUILayout.Button("RETURN HOME"))Send(new ReturnHomeCommand());GUILayout.EndHorizontal();
            GUILayout.Space(7);GUILayout.Label("DEMONSTRATION DESTINATIONS",label);
            Destination("Warehouse vicinity",-12,7,t);
            Destination("Loading Bay 1 approach",-23,10,t);
            Destination("Loading Bay 2 approach",-13,10,t);
            Destination("Inspection wall stand-off",22,-11,t);
            Destination("Restricted Zone A vicinity",9,0,t);
            Destination("Gate 1 approach",24,-21,t);
            Destination("Inspection Pad 1",12,-18,t);
            GUILayout.Space(5);
            if(GUILayout.Button("RESET SIMULATION")){reset.ResetSimulation();last=null;}
            if(last.HasValue)GUILayout.Label($"#{last.Value.Id}  {last.Value.Command}  {last.Value.Status}\n{last.Value.Reason}",status);
            GUILayout.Label("Static props are ground truth, not detections.",label);
            GUILayout.EndArea();
        }
        private void Destination(string title,double east,double north,TelemetrySnapshot telemetry)
        {
            if(GUILayout.Button(title))Send(new MoveToCommand(new FlightVector(east,north,telemetry.Home.Up+4)));
        }
    }
}
