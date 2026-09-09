using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MicroHawk.Editor
{
    public static class FlightDemoBuild
    {
        public static void Build()
        {
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/build/MicroHawk.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{IndustrialSceneBuilder.ScenePath},locationPathName=path,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("MicroHawk player build failed: "+report.summary.result);
            Debug.Log("MicroHawk flight demo build: "+path);
        }
    }
}
