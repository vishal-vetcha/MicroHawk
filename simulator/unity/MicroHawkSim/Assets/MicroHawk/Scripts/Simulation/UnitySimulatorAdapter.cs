using System;
using System.Linq;
using MicroHawk.Contracts;
using UnityEngine;

namespace MicroHawk.Simulation
{
    /// <summary>The only runtime class allowed to actuate or reset the drone Rigidbody.</summary>
    public sealed class UnitySimulatorAdapter : ISimulatorAdapter
    {
        private readonly Rigidbody body;
        private readonly DroneContacts contacts;
        private readonly Vector3 home;
        private readonly Quaternion rotation;
        private readonly Vector3[] landingPads;
        private readonly Vector3 envelope=new(.65f,.36f,.65f);
        public UnitySimulatorAdapter(Rigidbody body,Transform spawn,Vector3[] pads)
        {
            this.body=body;home=spawn.position;rotation=spawn.rotation;landingPads=pads;
            contacts=body.GetComponent<DroneContacts>()??body.gameObject.AddComponent<DroneContacts>();
        }
        public static FlightVector ToDomain(Vector3 value)=>new(value.x,value.z,value.y);
        public static Vector3 ToUnity(FlightVector value)=>new((float)value.East,(float)value.Up,(float)value.North);
        public BodySnapshot Read()=>new(ToDomain(body.position),ToDomain(body.linearVelocity),ToDomain(body.transform.up),ToDomain(body.transform.forward),ToDomain(-body.angularVelocity),contacts.Supported);
        public void Apply(Actuation output)
        {
            if(!FlightVector.Finite(output.ThrustAcceleration)||!output.AngularAcceleration.IsFinite)throw new ArgumentException("Nonfinite actuation.");
            body.AddForce(body.transform.up*(float)output.ThrustAcceleration,ForceMode.Acceleration);
            // ENU/Unity basis has determinant -1: angular vectors are axial, hence the extra minus.
            body.AddTorque(-ToUnity(output.AngularAcceleration),ForceMode.Acceleration);
        }
        public void Step(double dt)=>Physics.Simulate((float)dt);
        public void RestoreHome()
        {
            body.position=home;body.rotation=rotation;
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            contacts.Clear();body.Sleep();body.WakeUp();Physics.SyncTransforms();
        }
        private bool Obstacle(Collider collider)=>collider&&collider.attachedRigidbody!=body&&!collider.isTrigger;
        public bool ClearSegment(FlightVector from,FlightVector to,double margin=0,bool allowSupport=false)
        {
            if(!from.IsFinite||!to.IsFinite)return false;
            var start=ToUnity(from);var end=ToUnity(to);var difference=end-start;
            var half=envelope+Vector3.one*(float)margin;
            float supportHeight=Mathf.Min(start.y,end.y)-.34f;
            bool Blocks(Collider c)=>Obstacle(c)&&!(allowSupport&&c.bounds.max.y<=supportHeight+.015f);
            if(Physics.OverlapBox(start,half,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(Blocks))return false;
            if(Physics.OverlapBox(end,half,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(Blocks))return false;
            if(difference.magnitude<.001f)return true;
            return !Physics.BoxCastAll(start,half,difference.normalized,Quaternion.identity,difference.magnitude,~0,QueryTriggerInteraction.Ignore).Any(hit=>Blocks(hit.collider));
        }
        public bool TryLanding(FlightVector position,bool emergency,out FlightVector contactPosition)
        {
            contactPosition=position;
            var center=ToUnity(position);
            if(!emergency&&!landingPads.Any(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(center.x,center.z))<1.1f))return false;
            float? height=null;
            // Require a flat, clear footprint, not merely a single ray to a crate/person.
            foreach(float x in new[]{-.6f,0,.6f})foreach(float z in new[]{-.6f,0,.6f})
            {
                var origin=center+new Vector3(x,-.3f,z);
                var hits=Physics.RaycastAll(origin,Vector3.down,30,~0,QueryTriggerInteraction.Ignore).Where(h=>Obstacle(h.collider)).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0||hits[0].normal.y<.98f)return false;
                float y=hits[0].point.y;
                if(height.HasValue&&Math.Abs(height.Value-y)>.035f)return false;
                height=y;
            }
            contactPosition=new FlightVector(position.East,position.North,height.Value+.35);
            return ClearSegment(position,contactPosition,0,true);
        }
    }
}

