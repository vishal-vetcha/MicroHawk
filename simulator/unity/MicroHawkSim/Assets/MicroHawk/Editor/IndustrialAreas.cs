using UnityEngine;

namespace MicroHawk.Editor
{
    internal static class IndustrialAreas
    {
        internal static void Build(FacilityAuthoring a)
        {
            var yard = a.Group("SiteInfrastructure", Vector3.zero);
            a.Shape(yard,"Ground",new Vector3(0,-.25f,0),new Vector3(72,.5f,64),"Concrete");
            a.Shape(yard,"YardPavement",new Vector3(0,-.02f,0),new Vector3(68,.04f,60),"Pavement");
            a.Outline(yard,new Vector3(0,.015f,0),67,59,"White");
            for(int z=-27;z<27;z+=4)
                a.Shape(yard,"RoadDash",new Vector3(1,.015f,z),new Vector3(.14f,.02f,1.8f),"White",PrimitiveType.Cube,false);
            for(int x=-30;x<=30;x+=5)
            {
                a.Shape(yard,"NorthFencePost",new Vector3(x,1.1f,29),new Vector3(.1f,2.2f,.1f),"Steel");
                a.Shape(yard,"SouthFencePost",new Vector3(x,1.1f,-29),new Vector3(.1f,2.2f,.1f),"Steel");
            }
            for(int y=1;y<=2;y++)
            {
                a.Shape(yard,"FenceRailN",new Vector3(0,y,29),new Vector3(65,.06f,.06f),"Steel");
                a.Shape(yard,"FenceRailS",new Vector3(0,y,-29),new Vector3(65,.06f,.06f),"Steel");
            }
            for(int x=-28;x<=28;x+=14)
            {
                a.Shape(yard,"LightPole",new Vector3(x,3.3f,27),new Vector3(.15f,6.6f,.15f),"Steel");
                a.Shape(yard,"LightHead",new Vector3(x,6.5f,26.6f),new Vector3(1.1f,.15f,.6f),"White",PrimitiveType.Cube,false);
            }
            Warehouse(a);
            Utilities(a);
            Inspection(a);
            Navigation(a);
            Restricted(a);
            Security(a);
            var ops=a.Group("MicroHawkOperations",new Vector3(-23,0,-23));
            a.Shape(ops,"OperationsCabin",new Vector3(0,1.5f,0),new Vector3(7,3,3),"Steel");
            a.Shape(ops,"Window",new Vector3(-1,1.7f,-1.51f),new Vector3(3,1.1f,.03f),"Glass");
            a.Shape(ops,"Door",new Vector3(2,.95f,-1.52f),new Vector3(1,1.9f,.04f),"Teal");
            a.Sign(ops,"MICROHAWK  /  OPERATIONS",new Vector3(0,3.5f,-1.5f),8,1);
            a.Text(yard,"SiteTitle","MICROHAWK  /  FIELD LAB 01",new Vector3(-14,.03f,-26),.8f,Color.white,true);
            a.Pad("home_pad",new Vector3(-20,0,-15),"HOME  /  H01",6);
            a.Pad("inspection_pad_1",new Vector3(12,0,-18),"INSPECTION  /  P01",4);
            a.Pad("inspection_pad_2",new Vector3(26,0,4),"INSPECTION  /  P02",4);
            StaticProps.Worker(a,new Vector3(-12,0,9),"worker_1");
            StaticProps.Worker(a,new Vector3(9,0,6),"worker_2");
            StaticProps.Worker(a,new Vector3(22,0,18),"worker_3");
            StaticProps.Forklift(a,new Vector3(-18,0,8));
            StaticProps.Vehicle(a,new Vector3(-6,0,7));
        }

        private static void Warehouse(FacilityAuthoring a)
        {
            var w=a.Group("Warehouse",new Vector3(-13,0,21),"warehouse");
            a.Shape(w,"WarehouseShell",new Vector3(0,4,0),new Vector3(32,8,12),"Cladding");
            a.Shape(w,"RoofCap",new Vector3(0,8.1f,0),new Vector3(32.5f,.25f,12.5f),"Steel");
            a.Shape(w,"FacadeBand",new Vector3(0,6.7f,-6.05f),new Vector3(32,.85f,.1f),"Teal");
            a.Sign(w,"MICROHAWK   /   INDUSTRIAL SYSTEMS",new Vector3(0,7.4f,-6.2f),24,1);
            for(int x=-15;x<=15;x++)
                a.Shape(w,"CladdingRib",new Vector3(x,4,-6.05f),new Vector3(.04f,8,.04f),"Steel",PrimitiveType.Cube,false);
            for(int i=0;i<3;i++)
            {
                float x=-23+i*10;
                var bay=a.Group($"LoadingBay_{i+1}",new Vector3(x,0,14.8f),$"loading_bay_{i+1}");
                a.Shape(bay,"DockDoor",new Vector3(0,2.5f,0),new Vector3(5,5,.25f),"Dark");
                for(int j=1;j<10;j++)
                    a.Shape(bay,"DoorSlat",new Vector3(0,j*.5f,-.14f),new Vector3(4.8f,.05f,.03f),"Steel",PrimitiveType.Cube,false);
                a.Sign(bay,$"BAY  0{i+1}",new Vector3(0,5.65f,-.1f),5,.85f);
                a.Outline(bay,new Vector3(0,.02f,-4),6,8);
                for(int side=-1;side<=1;side+=2)
                {
                    a.Shape(bay,"DockBumper",new Vector3(side*2.4f,.6f,-.3f),new Vector3(.25f,1.2f,.4f),"Dark");
                    a.Shape(bay,"Bollard",new Vector3(side*3,.6f,-.8f),new Vector3(.22f,.6f,.22f),"Yellow",PrimitiveType.Cylinder);
                }
            }
            a.Crate(w,new Vector3(-14,0,-8));a.Crate(w,new Vector3(14,0,-8));
        }

        private static void Restricted(FacilityAuthoring a)
        {
            var z=a.Group("RestrictedZoneA",new Vector3(9,0,6),"restricted_zone_a");
            a.Outline(z,new Vector3(0,.03f,0),10,9);
            for(float x=-4.5f;x<5;x+=.7f)
            {
                var stripe=a.Shape(z,"Hatch",new Vector3(x,.025f,-4),new Vector3(.24f,.02f,1),"Yellow",PrimitiveType.Cube,false);
                stripe.transform.localRotation=Quaternion.Euler(0,-35,0);
            }
            a.Text(z,"ZoneFloor","RESTRICTED\nZONE A",new Vector3(0,.04f,0),1.05f,new Color(1,.7f,.12f),true);
            a.Sign(z,"RESTRICTED ZONE A",new Vector3(0,2.2f,4.4f),9,1);
            a.Shape(z,"SignPost",new Vector3(0,1,4.4f),new Vector3(.12f,2,.12f),"Steel");
        }

        private static void Utilities(FacilityAuthoring a)
        {
            var u=a.Group("UtilityArea",new Vector3(23,0,20),"utility_area");
            a.Shape(u,"UtilitySlab",new Vector3(0,.1f,0),new Vector3(17,.2f,13),"Concrete");
            for(int i=0;i<3;i++)
            {
                float x=-5+i*5;
                a.Shape(u,"Tank",new Vector3(x,2.7f,1),new Vector3(3.6f,2.6f,3.6f),"Cladding",PrimitiveType.Cylinder);
                a.Shape(u,"TankCap",new Vector3(x,5.32f,1),new Vector3(3.7f,.12f,3.7f),"Steel",PrimitiveType.Cylinder);
                a.Shape(u,"TankBand",new Vector3(x,3.5f,1),new Vector3(3.65f,.15f,3.65f),"Teal",PrimitiveType.Cylinder,false);
                a.Sign(u,$"T-0{i+1}",new Vector3(x,2.8f,-.85f),1.7f,.7f);
                a.Beam(u,"PipeDrop",new Vector3(x,2.2f,-1),new Vector3(x,.6f,-4),.18f,"Steel");
            }
            a.Beam(u,"Manifold",new Vector3(-6,.6f,-4),new Vector3(6,.6f,-4),.24f,"Teal");
            a.Shape(u,"PumpSkid",new Vector3(5,.65f,-5),new Vector3(2,1.1f,1.5f),"Steel");
            a.Sign(u,"UTILITIES  /  04",new Vector3(0,1.3f,-6.2f),8,.85f);
        }

        private static void Inspection(FacilityAuthoring a)
        {
            var w=a.Group("StructuralInspectionWall",new Vector3(22,0,-7),"inspection_wall_1");
            a.Shape(w,"ConcreteWall",new Vector3(0,2.5f,0),new Vector3(17,5,.45f),"Concrete");
            a.Shape(w,"WallCap",new Vector3(0,5.1f,0),new Vector3(17.3f,.2f,.6f),"Steel");
            a.Sign(w,"STRUCTURAL INSPECTION  /  WALL 01",new Vector3(0,5.8f,0),18,1);
            for(int i=0;i<3;i++)
            {
                float x=-5+i*5;
                var points=new[]{new Vector3(x-.4f,3.8f,-.235f),new Vector3(x,3.2f,-.235f),new Vector3(x-.15f,2.8f,-.235f),new Vector3(x+.3f,2.2f,-.235f),new Vector3(x+.12f,1.6f,-.235f)};
                for(int j=0;j<points.Length-1;j++)a.Beam(w,$"AuthoredCrack_{i+1}_{j}",points[j],points[j+1],.045f,"Dark",false);
                a.Beam(w,"CrackBranch",points[2],points[2]+new Vector3(-.65f,-.2f,0),.025f,"Dark",false);
                a.Sign(w,$"I-0{i+1}",new Vector3(x,1,-.3f),1.4f,.6f);
            }
            a.Outline(w,new Vector3(0,.025f,-3),18,5,"White");
            a.Text(w,"InspectionStandOff","INSPECTION STAND-OFF",new Vector3(0,.04f,-5),.7f,Color.white,true);
        }

        private static void Navigation(FacilityAuthoring a)
        {
            var n=a.Group("NavigationCourse",new Vector3(-21,0,-1),"navigation_course");
            a.Outline(n,new Vector3(0,.025f,0),17,12,"Teal");
            for(int i=0;i<4;i++)
            {
                a.Shape(n,"Pylon",new Vector3(-6+i*4,1.8f,1),new Vector3(.5f,1.8f,.5f),"Yellow",PrimitiveType.Cylinder);
                a.Shape(n,"PylonCap",new Vector3(-6+i*4,3.4f,1),new Vector3(.53f,.15f,.53f),"Dark",PrimitiveType.Cylinder);
            }
            for(int i=0;i<2;i++)
            {
                float x=-5+i*9;
                a.Shape(n,"CorridorRailL",new Vector3(x,1,-3),new Vector3(5,2,.25f),"Steel");
                a.Shape(n,"CorridorRailR",new Vector3(x,1,-.2f),new Vector3(5,2,.25f),"Steel");
            }
            a.Text(n,"NavigationLabel","NAVIGATION  /  05",new Vector3(0,.04f,-5),.85f,Color.white,true);
        }

        private static void Security(FacilityAuthoring a)
        {
            var g=a.Group("SecurityGate1",new Vector3(24,0,-25),"security_gate_1");
            a.Shape(g,"GateBooth",new Vector3(6,1.5f,0),new Vector3(3,3,3),"Cladding");
            a.Shape(g,"BoothWindow",new Vector3(6,1.9f,-1.52f),new Vector3(2.5f,1,.05f),"Glass");
            a.Shape(g,"GatePivot",new Vector3(-5,.7f,0),new Vector3(.5f,1.4f,.5f),"Steel");
            a.Shape(g,"GateArm",new Vector3(0,1.15f,0),new Vector3(10,.2f,.2f),"White");
            for(int x=-4;x<=4;x+=2)a.Shape(g,"GateStripe",new Vector3(x,1.15f,-.11f),new Vector3(.6f,.21f,.015f),"Red",PrimitiveType.Cube,false);
            a.Sign(g,"GATE 1  /  SECURITY",new Vector3(0,3.5f,0),10,1.1f);
            a.Shape(g,"GateSignPost",new Vector3(-5,1.7f,0),new Vector3(.15f,3.4f,.15f),"Steel");
        }
    }
}
