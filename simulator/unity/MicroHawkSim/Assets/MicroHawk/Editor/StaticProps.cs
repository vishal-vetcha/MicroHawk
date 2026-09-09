using UnityEngine;

namespace MicroHawk.Editor
{
    internal static class StaticProps
    {
        internal static void Worker(FacilityAuthoring a, Vector3 position, string name)
        {
            var p=a.Group("StaticMannequin_"+name,position);
            a.Shape(p,"Torso",new Vector3(0,1.15f,0),new Vector3(.46f,.55f,.27f),"Orange");
            a.Shape(p,"Head",new Vector3(0,1.62f,0),new Vector3(.25f,.3f,.25f),"Concrete",PrimitiveType.Sphere);
            a.Shape(p,"HardHat",new Vector3(0,1.77f,0),new Vector3(.34f,.08f,.34f),"Yellow",PrimitiveType.Cylinder);
            for(int side=-1;side<=1;side+=2)
            {
                a.Shape(p,"Leg",new Vector3(side*.13f,.45f,0),new Vector3(.16f,.85f,.18f),"Dark");
                a.Shape(p,"Boot",new Vector3(side*.13f,.07f,-.04f),new Vector3(.19f,.14f,.3f),"Dark");
                a.Beam(p,"Arm",new Vector3(side*.3f,1.36f,0),new Vector3(side*.38f,.8f,0),.13f,"Orange");
            }
            a.Shape(p,"ReflectiveVest",new Vector3(0,1.1f,-.143f),new Vector3(.46f,.07f,.012f),"Yellow",PrimitiveType.Cube,false);
        }

        internal static void Forklift(FacilityAuthoring a, Vector3 position)
        {
            var p=a.Group("StaticForklift",position);
            a.Shape(p,"Chassis",new Vector3(0,.62f,0),new Vector3(1.35f,.75f,2.1f),"Yellow");
            a.Shape(p,"Seat",new Vector3(0,1.12f,.35f),new Vector3(.65f,.28f,.65f),"Dark");
            for(int side=-1;side<=1;side+=2)
            {
                for(int z=-1;z<=1;z+=2)
                {
                    var wheel=a.Shape(p,"Wheel",new Vector3(side*.72f,.36f,z*.68f),new Vector3(.65f,.16f,.65f),"Dark",PrimitiveType.Cylinder);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                    a.Shape(p,"CabPost",new Vector3(side*.58f,1.62f,z*.7f),new Vector3(.07f,1.6f,.07f),"Steel");
                }
                a.Shape(p,"Mast",new Vector3(side*.42f,1.55f,-1.13f),new Vector3(.14f,3.1f,.15f),"Steel");
                a.Shape(p,"Fork",new Vector3(side*.42f,.18f,-1.8f),new Vector3(.14f,.12f,1.5f),"Steel");
            }
            a.Shape(p,"CabRoof",new Vector3(0,2.5f,0),new Vector3(1.4f,.12f,1.7f),"Dark");
        }

        internal static void Vehicle(FacilityAuthoring a, Vector3 position)
        {
            var p=a.Group("StaticServiceVan",position);
            a.Shape(p,"VanBody",new Vector3(0,1.25f,0),new Vector3(2,1.75f,4.5f),"White");
            a.Shape(p,"Windshield",new Vector3(0,1.65f,-2.26f),new Vector3(1.8f,.7f,.03f),"Glass");
            a.Shape(p,"Bumper",new Vector3(0,.55f,-2.35f),new Vector3(2.1f,.25f,.2f),"Dark");
            for(int side=-1;side<=1;side+=2)
            {
                for(int z=-1;z<=1;z+=2)
                {
                    var wheel=a.Shape(p,"Wheel",new Vector3(side*1.03f,.45f,z*1.4f),new Vector3(.85f,.16f,.85f),"Dark",PrimitiveType.Cylinder);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                }
                a.Shape(p,"Headlight",new Vector3(side*.72f,.9f,-2.27f),new Vector3(.35f,.2f,.05f),"Yellow",PrimitiveType.Cube,false);
                a.Shape(p,"Livery",new Vector3(side*1.01f,1.15f,0),new Vector3(.02f,.35f,4),"Teal",PrimitiveType.Cube,false);
            }
        }
    }
}
