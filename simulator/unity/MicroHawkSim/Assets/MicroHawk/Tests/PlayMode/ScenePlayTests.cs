using System.Collections;
using MicroHawk.Presentation;
using MicroHawk.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MicroHawk.Tests
{
    public sealed class ScenePlayTests
    {
        [UnityTest]
        public IEnumerator DroneSettlesOnHomeAndCameraSwitchDoesNotMoveIt()
        {
            yield return SceneManager.LoadSceneAsync("IndustrialTest",LoadSceneMode.Single);
            var config=Object.FindAnyObjectByType<SceneConfiguration>();
            Assert.That(config,Is.Not.Null);
            var initial=config.Drone.position;
            for(int i=0;i<100;i++)yield return new WaitForFixedUpdate();
            Assert.That(Vector2.Distance(new Vector2(initial.x,initial.z),new Vector2(config.Drone.position.x,config.Drone.position.z)),Is.LessThan(.05f));
            Assert.That(config.Drone.position.y,Is.InRange(.50f,.55f),"Drone should settle on its skids, not fall through the pad.");
            Assert.That(config.Drone.linearVelocity.magnitude,Is.LessThan(.05f));
            Assert.That(Quaternion.Angle(config.Drone.rotation,config.HomeSpawn.rotation),Is.LessThan(2f));
            var cameras=Object.FindAnyObjectByType<FacilityCameras>();
            var before=config.Drone.position;
            cameras.ShowOverview(false);
            Assert.That(cameras.Follow.enabled,Is.True);Assert.That(cameras.Overview.enabled,Is.False);
            Assert.That(config.Drone.position,Is.EqualTo(before));
            cameras.ShowOverview(true);
            Assert.That(cameras.Overview.enabled,Is.True);Assert.That(cameras.Follow.enabled,Is.False);
        }
    }
}

