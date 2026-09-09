using System;
using System.IO;
using MicroHawk.Presentation;
using MicroHawk.Simulation;
using MicroHawk.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MicroHawk.Editor
{
    public static class IndustrialSceneBuilder
    {
        public const string ScenePath="Assets/MicroHawk/Scenes/IndustrialTest.unity";

        [MenuItem("MicroHawk/Rebuild Authored Facility")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild facility", "Replace the generated IndustrialTest scene, drone prefab and authored palette? Save custom work separately first.", "Rebuild", "Cancel")) return;
            Build();
        }

        // Explicit batch authoring entry point. Never runs automatically on import or Play.
        public static void Build()
        {
            foreach(var dir in new[]{"Scenes","Prefabs","Materials","Settings"}) Directory.CreateDirectory(FacilityAuthoring.Root+"/"+dir);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            ConfigureProject();
            var root=new GameObject("IndustrialFacility");
            var author=new FacilityAuthoring(root.transform);
            IndustrialAreas.Build(author);
            var prefab=DroneAuthoring.CreatePrefab(author);
            var home=author.Entities.Find(e=>e.StableId=="home_pad");
            var spawn=new GameObject("HomeSpawn_BodyReference").transform;
            spawn.SetParent(home.transform,false);
            spawn.localPosition=new Vector3(0,.535f,0); // Surface .175 + skid depth .350 + .010 settling gap.
            spawn.localRotation=Quaternion.LookRotation(UnityCoordinates.Heading(0),Vector3.up);
            var drone=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            drone.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
            var registry=root.AddComponent<WorldRegistry>();registry.ConfigureForAuthoring(author.Entities.ToArray());
            var config=new GameObject("SimulationConfiguration").AddComponent<SceneConfiguration>();
            config.ConfigureForAuthoring(registry,home,spawn,drone.GetComponent<Rigidbody>());
            CreateLighting();
            var overview=CreateCamera("Overview Camera",new Vector3(54,48,-63),new Vector3(0,0,0));
            overview.fieldOfView=48;
            var follow=CreateCamera("Drone Follow Camera",drone.transform.position+new Vector3(1.8f,1.1f,-2.1f),drone.transform.position);
            follow.fieldOfView=43;
            var controls=new GameObject("CameraPresentation").AddComponent<FacilityCameras>();
            controls.ConfigureForAuthoring(overview,follow,drone.transform);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("MicroHawk: authored IndustrialTest scene and drone prefab. No flight controller or perception present.");
        }

        private static void ConfigureProject()
        {
            EditorSettings.serializationMode=SerializationMode.ForceText;
            UnityEditor.VersionControlSettings.mode="Visible Meta Files";
            PlayerSettings.companyName="MicroHawk";
            PlayerSettings.productName="MicroHawk Industrial Test Facility";
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth=1600;
            PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            Time.fixedDeltaTime=.02f;
            Time.maximumDeltaTime=.1f;
            Physics.gravity=new Vector3(0,-9.81f,0);
            // Bootstrap uses normal fixed-step settling only. Explicit stepping arrives with flight host.
            Physics.simulationMode=SimulationMode.FixedUpdate;
            const string rendererPath="Assets/MicroHawk/Settings/FacilityRenderer.asset";
            const string pipelinePath="Assets/MicroHawk/Settings/FacilityURP.asset";
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,rendererPath);}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(!pipeline){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,pipelinePath);}
            pipeline.renderScale=1;
            pipeline.msaaSampleCount=4;
            pipeline.shadowDistance=130;
            pipeline.shadowCascadeCount=4;
            pipeline.supportsHDR=true;
            GraphicsSettings.defaultRenderPipeline=pipeline;
            QualitySettings.renderPipeline=pipeline;
            QualitySettings.vSyncCount=1;
            EditorUtility.SetDirty(pipeline);
        }

        private static Camera CreateCamera(string name,Vector3 pos,Vector3 lookAt)
        {
            var go=new GameObject(name);
            go.transform.position=pos;go.transform.LookAt(lookAt);
            var camera=go.AddComponent<Camera>();
            camera.nearClipPlane=.05f;camera.farClipPlane=350;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.14f,.2f,.25f);
            camera.allowHDR=true;
            go.AddComponent<AudioListener>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            return camera;
        }

        private static void CreateLighting()
        {
            var sun=new GameObject("Facility Sun").AddComponent<Light>();
            sun.type=LightType.Directional;sun.intensity=2.2f;
            sun.color=new Color(1f,.92f,.8f);sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.56f,.66f,.77f);
            RenderSettings.ambientEquatorColor=new Color(.32f,.39f,.44f);
            RenderSettings.ambientGroundColor=new Color(.17f,.2f,.23f);
            RenderSettings.fog=false;
        }

        public static void CaptureViews()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var cameras=UnityEngine.Object.FindAnyObjectByType<FacilityCameras>();
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/visuals"));
            Directory.CreateDirectory(directory);
            Capture(cameras.Overview,Path.Combine(directory,"overview.png"));
            var drone=UnityEngine.Object.FindAnyObjectByType<SceneConfiguration>().Drone;
            cameras.Follow.transform.position=drone.position+new Vector3(1.5f,.9f,-1.8f);
            cameras.Follow.transform.LookAt(drone.position);
            Capture(cameras.Follow,Path.Combine(directory,"drone.png"));
            cameras.Overview.transform.position=new Vector3(16,7,-20);
            cameras.Overview.transform.LookAt(new Vector3(22,2.3f,-7));
            Capture(cameras.Overview,Path.Combine(directory,"inspection.png"));
            Debug.Log("MicroHawk visual captures: "+directory);
        }

        private static void Capture(Camera camera,string path)
        {
            var oldTarget=camera.targetTexture;
            var oldActive=RenderTexture.active;
            var target=new RenderTexture(1600,900,24) { antiAliasing = 4 };
            var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;
                camera.Render();
                camera.Render();
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}




