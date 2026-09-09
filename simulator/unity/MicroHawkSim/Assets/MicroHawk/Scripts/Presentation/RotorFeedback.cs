using System.Linq;
using MicroHawk.Contracts;
using UnityEngine;

namespace MicroHawk.Presentation
{
    /// <summary>Cosmetic child meshes only; has no Rigidbody or controller access.</summary>
    public sealed class RotorFeedback : MonoBehaviour
    {
        private IFlightOperations telemetry;
        private Transform[] rotors;
        private Quaternion[] resting;
        private long session=-1;
        private float angle;
        public void Bind(IFlightOperations source,Transform drone)
        {
            telemetry=source;
            rotors=drone.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Propeller_")).ToArray();
            resting=rotors.Select(t=>t.localRotation).ToArray();
        }
        private void Update()
        {
            var state=telemetry?.Telemetry;if(state==null)return;
            if(session!=state.Session){session=state.Session;angle=0;}
            if(state.Armed)angle=(angle+Time.deltaTime*1700)%360;
            for(int i=0;i<rotors.Length;i++)rotors[i].localRotation=resting[i]*Quaternion.Euler(0,angle*(i%2==0?1:-1),0);
        }
    }
}
