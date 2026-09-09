using UnityEditor;
using UnityEngine;

namespace MicroHawk.Editor
{
    internal static class DroneAuthoring
    {
        internal static GameObject CreatePrefab(FacilityAuthoring a)
        {
            var root=new GameObject("MicroHawkDrone");
            var visuals=new GameObject("Visuals").transform;
            visuals.SetParent(root.transform,false);
            a.Shape(visuals,"Body",Vector3.zero,new Vector3(.34f,.16f,.46f),"Dark",PrimitiveType.Cube,false);
            a.Shape(visuals,"TopShell",new Vector3(0,.09f,0),new Vector3(.29f,.07f,.35f),"White",PrimitiveType.Cube,false);
            a.Shape(visuals,"IdentityStripe",new Vector3(0,.13f,0),new Vector3(.07f,.012f,.32f),"Teal",PrimitiveType.Cube,false);
            foreach(var item in new[]{("FL",-1,1),("FR",1,1),("RL",-1,-1),("RR",1,-1)})
            {
                var motor=new Vector3(item.Item2*.35f,.04f,item.Item3*.34f);
                a.Beam(visuals,"Arm_"+item.Item1,new Vector3(item.Item2*.11f,0,item.Item3*.12f),motor,.065f,"Steel",false);
                a.Shape(visuals,"Motor_"+item.Item1,motor,new Vector3(.09f,.055f,.09f),"Dark",PrimitiveType.Cylinder,false);
                var prop=a.Shape(visuals,"Propeller_"+item.Item1,motor+Vector3.up*.065f,new Vector3(.37f,.009f,.035f),"Dark",PrimitiveType.Cube,false);
                prop.transform.localRotation=Quaternion.Euler(0,item.Item3*28,0);
                a.Shape(visuals,"MotorHub_"+item.Item1,motor+Vector3.up*.075f,new Vector3(.035f,.012f,.035f),"Steel",PrimitiveType.Cylinder,false);
                a.Shape(visuals,"StatusLight_"+item.Item1,motor-Vector3.up*.04f,new Vector3(.03f,.015f,.035f),item.Item3>0?"Teal":"Red",PrimitiveType.Cube,false);
            }
            var gear=new GameObject("LandingGear").transform;gear.SetParent(visuals,false);
            for(int side=-1;side<=1;side+=2)
            {
                a.Beam(gear,"StrutFront",new Vector3(side*.13f,-.04f,.13f),new Vector3(side*.23f,-.31f,.17f),.026f,"Steel",false);
                a.Beam(gear,"StrutRear",new Vector3(side*.13f,-.04f,-.13f),new Vector3(side*.23f,-.31f,-.17f),.026f,"Steel",false);
                a.Shape(gear,"Skid",new Vector3(side*.23f,-.325f,0),new Vector3(.045f,.05f,.54f),"Dark",PrimitiveType.Cube,false);
            }
            a.Shape(visuals,"ForwardCameraMount",new Vector3(0,-.1f,.24f),new Vector3(.14f,.13f,.14f),"Steel",PrimitiveType.Cube,false);
            var lens=a.Shape(visuals,"InspectionLens",new Vector3(0,-.1f,.32f),new Vector3(.075f,.025f,.075f),"Glass",PrimitiveType.Cylinder,false);
            lens.transform.localRotation=Quaternion.Euler(90,0,0);
            a.Shape(visuals,"DownwardSensorMount",new Vector3(0,-.12f,-.07f),new Vector3(.11f,.06f,.1f),"Glass",PrimitiveType.Cube,false);
            var colliders=new GameObject("Colliders").transform;colliders.SetParent(root.transform,false);
            AddCollider(colliders,"BodyCollider",Vector3.zero,new Vector3(.35f,.18f,.47f));
            for(int side=-1;side<=1;side+=2)
            {
                AddCollider(colliders,"SkidCollider",new Vector3(side*.23f,-.325f,0),new Vector3(.045f,.05f,.54f));
                AddCollider(colliders,"ArmEnvelope",new Vector3(0,.035f,side*.34f),new Vector3(.8f,.1f,.12f));
            }
            var body=root.AddComponent<Rigidbody>();
            body.mass=1.8f;
            body.useGravity=true;
            body.isKinematic=false;
            body.linearDamping=.1f;
            body.angularDamping=.3f;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.solverIterations=12;
            body.solverVelocityIterations=4;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,FacilityAuthoring.Root+"/Prefabs/MicroHawkDrone.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddCollider(Transform parent,string name,Vector3 center,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;
        }
    }
}
