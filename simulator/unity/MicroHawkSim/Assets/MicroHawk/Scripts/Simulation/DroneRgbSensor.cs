using System;
using MicroHawk.Contracts;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MicroHawk.Simulation
{
    // Gimballed RGB sensor. Reads telemetry pose only; never reads WorldEntity or object labels.
    public sealed class DroneRgbSensor : MonoBehaviour
    {
        private Camera sensor;
        private RenderTexture target;
        private Texture2D pixels;
        public void Initialize()
        {
            var go=new GameObject("Drone RGB gimbal");go.transform.SetParent(transform);
            sensor=go.AddComponent<Camera>();sensor.enabled=false;sensor.fieldOfView=70;sensor.nearClipPlane=.1f;sensor.farClipPlane=100;
            sensor.clearFlags=CameraClearFlags.SolidColor;sensor.backgroundColor=new Color(.12f,.17f,.2f);
            target=new RenderTexture(640,480,24);pixels=new Texture2D(640,480,TextureFormat.RGB24,false);sensor.targetTexture=target;
        }
        public JObject Capture(TelemetrySnapshot t,string session,string request,FlightVector look)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)throw new InvalidOperationException("RgbRequiresGraphics");
            Vector3 aim=UnitySimulatorAdapter.ToUnity(look),position=UnitySimulatorAdapter.ToUnity(t.Body.Position);
            var forward=(aim-position).normalized;
            // Camera gimbal mounted below/forward of the body; it cannot relocate the drone.
            sensor.transform.position=position+Vector3.down*.24f+forward*.55f;sensor.transform.LookAt(aim);
            var old=RenderTexture.active;
            try{sensor.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,480),0,0);pixels.Apply();}
            finally{RenderTexture.active=old;}
            return new JObject{{"type","frame"},{"schema_version",1},{"session_id",session},{"request_id",request},{"tick",t.Tick},{"simulation_seconds",t.SimulationSeconds},{"camera_id","rgb_gimbal"},{"width",640},{"height",480},{"vertical_fov",70},{"position",Vector(t.Body.Position)},{"camera_position",Vector(UnitySimulatorAdapter.ToDomain(sensor.transform.position))},{"forward",Vector(UnitySimulatorAdapter.ToDomain(sensor.transform.forward))},{"right",Vector(UnitySimulatorAdapter.ToDomain(sensor.transform.right))},{"up",Vector(UnitySimulatorAdapter.ToDomain(sensor.transform.up))},{"jpeg",Convert.ToBase64String(pixels.EncodeToJPG(85))}};
        }
        public static JObject Vector(FlightVector v)=>new JObject{{"east",v.East},{"north",v.North},{"up",v.Up}};
        private void OnDestroy(){if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);if(sensor)Destroy(sensor.gameObject);}
    }
}
