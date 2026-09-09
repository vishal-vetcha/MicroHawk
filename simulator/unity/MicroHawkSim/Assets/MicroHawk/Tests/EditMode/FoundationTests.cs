using System;
using MicroHawk.Contracts;
using MicroHawk.Simulation;
using MicroHawk.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MicroHawk.Tests
{
    public sealed class FoundationTests
    {
        [Test]
        public void EnuMapsEastNorthUpAndRoundTrips()
        {
            var enu=new EnuPosition(12,-7,3);
            var actual=UnityCoordinates.ToUnity(enu);
            Assert.That(actual,Is.EqualTo(new Vector3(12,3,-7)));
            var back=UnityCoordinates.ToEnu(actual);
            Assert.That(back.East,Is.EqualTo(12));Assert.That(back.North,Is.EqualTo(-7));Assert.That(back.Up,Is.EqualTo(3));
        }

        [TestCase(0,1,0)]
        [TestCase(Math.PI/2,0,1)]
        [TestCase(Math.PI,-1,0)]
        [TestCase(-Math.PI/2,0,-1)]
        public void EnuYawUsesEastZeroAndTurnsNorth(double yaw,float east,float north)
        {
            var direction=UnityCoordinates.Heading(yaw);
            Assert.That(Vector3.Distance(direction,new Vector3(east,0,north)),Is.LessThan(1e-5f));
        }

        [Test]
        public void NonFiniteCoordinatesAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new EnuPosition(double.NaN,0,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new EnuPosition(0,double.PositiveInfinity,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>UnityCoordinates.ToUnity(new EnuPosition(double.MaxValue,0,0)));
        }

        [TestCase("")][TestCase("Loading Bay 1")][TestCase("home-pad")]
        public void InvalidWorldIdsAreRejected(string id) => Assert.Throws<ArgumentException>(()=>WorldEntity.ValidateId(id));

        [Test]
        public void RegistryRejectsDuplicateIds()
        {
            var root=new GameObject("RegistryTest");
            try
            {
                var one=new GameObject("one");one.transform.SetParent(root.transform);
                var two=new GameObject("two");two.transform.SetParent(root.transform);
                var a=one.AddComponent<WorldEntity>();a.ConfigureForAuthoring("home_pad");
                var b=two.AddComponent<WorldEntity>();b.ConfigureForAuthoring("home_pad");
                var registry=root.AddComponent<WorldRegistry>();
                Assert.Throws<InvalidOperationException>(()=>registry.ConfigureForAuthoring(new[]{a,b}));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void SavedSceneHasRequiredRegistryHomeAndDynamicPrefab()
        {
            EditorSceneManager.OpenScene("Assets/MicroHawk/Scenes/IndustrialTest.unity");
            var config=UnityEngine.Object.FindAnyObjectByType<SceneConfiguration>();
            Assert.That(config,Is.Not.Null);config.ValidateConfiguration();
            foreach(var id in new[]{"home_pad","warehouse","loading_bay_1","loading_bay_2","loading_bay_3","restricted_zone_a","security_gate_1","inspection_wall_1","utility_area","navigation_course","inspection_pad_1","inspection_pad_2"})
                Assert.That(config.World.TryGet(id,out _),Is.True,id);
            Assert.That(config.World.Count,Is.EqualTo(12));
            Assert.That(Vector3.Distance(config.Drone.position,config.HomeSpawn.position),Is.LessThan(.001f));
            Assert.That(Vector3.Distance(config.Drone.transform.forward,Vector3.right),Is.LessThan(.001f));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(config.Drone),Is.True);
            Assert.That(config.Drone.GetComponentsInChildren<Collider>().Length,Is.GreaterThanOrEqualTo(3));
            Assert.That(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline,Is.Not.Null);
            Assert.That(EditorBuildSettings.scenes[0].path,Does.EndWith("IndustrialTest.unity"));
        }
    }
}

