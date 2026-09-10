using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MicroHawk.Contracts;
using MicroHawk.Simulation;
using MicroHawk.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MicroHawk.Tests
{
    public sealed class PhysicalFlightTests
    {
        private FlightRuntime runtime;
        private SceneConfiguration config;
        private readonly List<CommandResult> results=new();
        private readonly List<TelemetrySnapshot> samples=new();
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("IndustrialTest",LoadSceneMode.Single);
            yield return null;
            config=UnityEngine.Object.FindAnyObjectByType<SceneConfiguration>();runtime=config.Runtime;
            Assert.That(runtime,Is.Not.Null);runtime.AutomaticStepping=false;
            runtime.CommandChanged+=r=>results.Add(r);runtime.TelemetryPublished+=t=>samples.Add(t);
            runtime.ResetSimulation();Step(100);results.Clear();samples.Clear();
        }
        private void Step(int count){for(int i=0;i<count;i++)runtime.AdvanceOneTick();}
        private long Send(FlightCommand command)
        {
            long id=runtime.Submit(command,runtime.Telemetry.Session);Step(5);
            var reject=results.Find(r=>r.Id==id&&r.Session==runtime.Telemetry.Session&&r.Status==ExecutionStatus.Rejected);
            Assert.That(reject.Status==ExecutionStatus.Rejected,Is.False,$"{command.Kind} rejected: {reject.Reason}");return id;
        }
        private void Until(Func<bool> condition,int maxTicks=9000)
        {
            for(int i=0;i<maxTicks&&!condition();i++)runtime.AdvanceOneTick();
            Assert.That(condition(),Is.True,$"Timeout: {runtime.State}, p={runtime.Telemetry.Body.Position}, v={runtime.Telemetry.Body.Velocity}, safety={runtime.Telemetry.Safety.Reason}, target={runtime.Telemetry.Target}");
        }
        private void Takeoff()
        {
            Send(new ArmCommand());Assert.That(runtime.State,Is.EqualTo(FlightState.Armed));
            Send(new TakeoffCommand(4));Until(()=>runtime.State==FlightState.Hovering,2500);
        }
        [UnityTest]
        public IEnumerator TakeoffHoverMoveHoldReturnAndLandDemonstration()
        {
            Takeoff();Step(200);
            Assert.That(runtime.Telemetry.Altitude,Is.EqualTo(4).Within(.2));
            Assert.That(runtime.Telemetry.Body.Velocity.Length,Is.LessThan(.2));
            Capture("flight-hover");
            var target=new FlightVector(22,-11,runtime.Telemetry.Home.Up+4);
            Send(new MoveToCommand(target));Until(()=>runtime.State==FlightState.Hovering);
            Assert.That((runtime.Telemetry.Body.Position-target).Length,Is.LessThan(.25));Capture("flight-inspection");
            Send(new HoldCommand(1));Until(()=>runtime.State==FlightState.Hovering);
            Send(new ReturnHomeCommand());Until(()=>runtime.State==FlightState.Landing);Capture("flight-return-descent");
            Until(()=>runtime.State==FlightState.Landed);Step(20);Capture("flight-landed");
            Assert.That((runtime.Telemetry.Body.Position-runtime.Telemetry.Home).Length,Is.LessThan(.2));
            Assert.That(runtime.Telemetry.Armed,Is.False);Assert.That(runtime.Telemetry.BatteryPercent,Is.LessThan(100));
            Assert.That(results.Exists(r=>r.Command==CommandKind.ReturnHome&&r.Status==ExecutionStatus.Completed&&r.Reason==ReasonCode.Touchdown),Is.True);
            WriteEvidence();yield return null;
        }
        [UnityTest]
        public IEnumerator LandAndIllegalCommandsUseSameBoundary()
        {
            var id=runtime.Submit(new MoveToCommand(new FlightVector(0,0,4)),runtime.Telemetry.Session);Step(5);
            Assert.That(results.Exists(r=>r.Id==id&&r.Status==ExecutionStatus.Rejected&&r.Reason==ReasonCode.InvalidState),Is.True);
            Takeoff();Send(new LandCommand());Until(()=>runtime.State==FlightState.Landed,1250);
            Assert.That(runtime.Telemetry.Body.GroundContact,Is.True);yield return null;
        }
        [UnityTest]
        public IEnumerator BlockedPhysicalDestinationAndStaleSessionReject()
        {
            Takeoff();long id=runtime.Submit(new MoveToCommand(new FlightVector(-13,21,4.535)),runtime.Telemetry.Session);Step(5);
            Assert.That(results.Exists(r=>r.Id==id&&r.Status==ExecutionStatus.Rejected&&r.Reason==ReasonCode.BlockedRoute),Is.True);
            var epoch=runtime.Telemetry.Session;runtime.ResetSimulation();runtime.Submit(new ArmCommand(),epoch);
            Assert.That(results.Exists(r=>r.Reason==ReasonCode.StaleSession),Is.True);yield return null;
        }
        [UnityTest]
        public IEnumerator RuntimeLowBatteryReturnsAndLands()
        {
            config.BatteryTuning.InitialPercent=35;config.BatteryTuning.HoverDrain=.8;config.BatteryTuning.MovementDrain=0;
            config.SafetyTuning.CriticalBattery=1;
            runtime.ResetSimulation();Step(100);Takeoff();
            Until(()=>runtime.State==FlightState.ReturningHome,3000);
            Assert.That(results.Exists(r=>r.Reason==ReasonCode.LowBattery&&r.Status==ExecutionStatus.Accepted),Is.True);
            Until(()=>runtime.State==FlightState.Landed);yield return null;
        }
        [UnityTest]
        public IEnumerator CriticalBatteryOverridesReturnWithControlledEmergencyLanding()
        {
            config.BatteryTuning.InitialPercent=35;config.BatteryTuning.HoverDrain=1.5;config.BatteryTuning.MovementDrain=0;
            runtime.ResetSimulation();Step(100);Takeoff();
            Until(()=>runtime.Telemetry.State==FlightState.Emergency,3000);
            Assert.That(runtime.Telemetry.Safety.Status,Is.EqualTo(SafetyStatus.EmergencyLandRequired));
            runtime.Submit(new MoveToCommand(new FlightVector(0,0,4)),runtime.Telemetry.Session);Step(5);
            Assert.That(results.Exists(r=>r.Reason==ReasonCode.FailsafeActive),Is.True);
            Until(()=>runtime.State==FlightState.Landed);
            Assert.That(runtime.Telemetry.Body.GroundContact,Is.True);yield return null;
        }
        [UnityTest]
        public IEnumerator ResetRepeatsPhysicalMissionAndClearsExecution()
        {
            FlightVector? baseline=null;
            for(int run=0;run<10;run++)
            {
                runtime.ResetSimulation();Assert.That(runtime.Telemetry.Tick,Is.Zero);Assert.That(runtime.Telemetry.BatteryPercent,Is.EqualTo(100));
                Assert.That(runtime.Telemetry.CurrentCommand,Is.Null);Assert.That(runtime.Telemetry.Body.Velocity.Length,Is.Zero);
                Step(100);Send(new ArmCommand());Send(new TakeoffCommand(4));Step(600);
                if(baseline.HasValue)Assert.That((runtime.Telemetry.Body.Position-baseline.Value).Length,Is.LessThan(.05));
                baseline=runtime.Telemetry.Body.Position;
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator TelemetryIsTickedBoundedAndStructured()
        {
            samples.Clear();Step(50);
            Assert.That(samples.Count,Is.EqualTo(10));
            for(int i=1;i<samples.Count;i++)Assert.That(samples[i].Tick-samples[i-1].Tick,Is.EqualTo(5));
            Assert.That(samples[0].Body.IsFinite,Is.True);Assert.That(TelemetrySnapshot.CoordinateFrame,Is.EqualTo("facility_enu_meters"));
            Assert.That(samples[0].SimulationSeconds,Is.EqualTo(samples[0].Tick*.02));yield return null;
        }
        [UnityTest]
        public IEnumerator RuntimeObstacleInterruptsAndBrakesBeforeContact()
        {
            Takeoff();Send(new MoveToCommand(new FlightVector(0,-15,4.535)));Step(50);
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                obstacle.transform.position=new Vector3(-8,4.535f,-15);obstacle.transform.localScale=new Vector3(2,3,2);
                var before=runtime.Telemetry;
                double speed=before.Body.Velocity.Length;
                double stoppingAllowance=speed*speed/(2*config.SafetyTuning.BrakingAcceleration)+speed*config.SafetyTuning.ReactionSeconds+.2;
                double peak=before.Body.Position.East;
                Physics.SyncTransforms();
                for(int i=0;i<400;i++){Step(1);peak=Math.Max(peak,runtime.Telemetry.Body.Position.East);}
                Assert.That(peak-before.Body.Position.East,Is.LessThan(stoppingAllowance+.2),"Stay within braking + reaction envelope.");
                Assert.That(runtime.State,Is.EqualTo(FlightState.Holding));
                Assert.That(results.Exists(r=>r.Status==ExecutionStatus.Failed&&r.Reason==ReasonCode.Obstacle),Is.True);
                Assert.That(runtime.Telemetry.Body.Position.East,Is.LessThan(-10));
                Assert.That(runtime.Telemetry.Body.Velocity.Length,Is.LessThan(.25));
            }
            finally { UnityEngine.Object.Destroy(obstacle); }
            yield return null;
        }
        [UnityTest]
        public IEnumerator ResetDuringLandingInterruptsAndRestoresAllState()
        {
            Takeoff();Send(new LandCommand());Step(100);
            long previous=runtime.Telemetry.Session;runtime.ResetSimulation();
            var t=runtime.Telemetry;
            Assert.That(t.Session,Is.GreaterThan(previous));Assert.That(t.Tick,Is.Zero);
            Assert.That(t.State,Is.EqualTo(FlightState.Landed));Assert.That(t.CurrentCommand,Is.Null);Assert.That(t.Target,Is.Null);
            Assert.That(t.Body.Velocity.Length,Is.Zero);Assert.That(t.Body.AngularVelocity.Length,Is.Zero);
            Assert.That(t.BatteryPercent,Is.EqualTo(100));Assert.That(t.Safety.Status,Is.EqualTo(SafetyStatus.Approved));
            Assert.That((t.Body.Position-t.Home).Length,Is.LessThan(.0001));
            Assert.That(results.Exists(r=>r.Status==ExecutionStatus.Interrupted&&r.Reason==ReasonCode.Reset),Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator DisconnectUsesPhysicalBrakeAndDoesNotTeleport()
        {
            Takeoff();Send(new MoveToCommand(new FlightVector(0,-15,4.535)));Step(50);
            runtime.ConnectionLost();Step(400);
            Assert.That(runtime.State,Is.EqualTo(FlightState.Holding));
            Assert.That(runtime.Telemetry.Body.Velocity.Length,Is.LessThan(.25));
            Assert.That(results.Exists(r=>r.Status==ExecutionStatus.Failed&&r.Reason==ReasonCode.ConnectionLoss),Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator LocalBatteryInjectionDrivesRealSafetyOverrides()
        {
            Takeoff();runtime.InjectBatteryForDemo(24);Step(5);
            Assert.That(runtime.Telemetry.Safety.Status,Is.EqualTo(SafetyStatus.ReturnHomeRequired));
            runtime.InjectBatteryForDemo(11);Step(5);
            Assert.That(runtime.State,Is.EqualTo(FlightState.Emergency));
            Until(()=>runtime.State==FlightState.Landed);
            Assert.That(runtime.Telemetry.Body.GroundContact,Is.True);yield return null;
        }
        private void Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var cameras=UnityEngine.Object.FindAnyObjectByType<FacilityCameras>();
            var camera=cameras.Follow;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var position=UnitySimulatorAdapter.ToUnity(runtime.Telemetry.Body.Position);
            camera.transform.position=position+new Vector3(2.4f,1.5f,-3.4f);camera.transform.LookAt(position);
            var target=new RenderTexture(1280,720,24){antiAliasing=4};var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();Directory.CreateDirectory(Output);
                File.WriteAllBytes(Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            }
            finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);camera.transform.SetPositionAndRotation(oldPosition,oldRotation);}
        }
        private static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts/visuals"));
        private void WriteEvidence()
        {
            Directory.CreateDirectory(Output);
            using var file=new StreamWriter(Path.Combine(Output,"flight-telemetry.csv"));
            file.WriteLine("session,tick,seconds,state,east,north,up,altitude,speed,battery,command,safety");
            foreach(var t in samples)file.WriteLine(FormattableString.Invariant($"{t.Session},{t.Tick},{t.SimulationSeconds:F2},{t.State},{t.Body.Position.East:F4},{t.Body.Position.North:F4},{t.Body.Position.Up:F4},{t.Altitude:F4},{t.Body.Velocity.Length:F4},{t.BatteryPercent:F3},{t.CurrentCommand},{t.Safety.Reason}"));
        }
    }
}





