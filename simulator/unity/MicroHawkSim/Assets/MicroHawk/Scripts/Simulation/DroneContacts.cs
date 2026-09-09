using System.Collections.Generic;
using UnityEngine;

namespace MicroHawk.Simulation
{
    public sealed class DroneContacts : MonoBehaviour
    {
        private readonly HashSet<Collider> support=new();
        public bool Supported=>support.Count>0;
        public void Clear()=>support.Clear();
        private void OnCollisionEnter(Collision collision)=>Record(collision);
        private void OnCollisionStay(Collision collision)=>Record(collision);
        private void OnCollisionExit(Collision collision)=>support.Remove(collision.collider);
        private void Record(Collision collision)
        {
            bool ground=false;
            for(int i=0;i<collision.contactCount;i++)if(collision.GetContact(i).normal.y>.8f)ground=true;
            Collider id=collision.collider;
            if(ground)support.Add(id);else support.Remove(id);
        }
    }
}

