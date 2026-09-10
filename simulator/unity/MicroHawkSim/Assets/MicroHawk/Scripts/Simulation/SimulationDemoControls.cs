using MicroHawk.Contracts;
using UnityEngine;
namespace MicroHawk.Simulation
{
    // Local simulator administration, deliberately absent from network/agent capabilities.
    public sealed class SimulationDemoControls : MonoBehaviour
    {
        private FlightRuntime runtime;
        public void Bind(FlightRuntime flight)=>runtime=flight;
        private void OnGUI()
        {
            GUI.Box(new Rect(18,140,355,93),"LOCAL FAILURE INJECTION / REAL SAFETY");
            if(GUI.Button(new Rect(30,170,155,25),"Battery to 24%"))runtime.InjectBatteryForDemo(24);
            if(GUI.Button(new Rect(195,170,164,25),"Battery to 11%"))runtime.InjectBatteryForDemo(11);
            GUI.Label(new Rect(30,202,320,22),"Drops battery only. Reset restores the simulator.");
        }
    }
}
