using System;
using MicroHawk.Contracts;
using MicroHawk.Safety;
using MicroHawk.Flight;
using NUnit.Framework;

namespace MicroHawk.Tests
{
    public sealed class FlightPolicyTests
    {
        private sealed class Geometry : IRouteGeometry
        {
            public bool Blocked;
            public bool ClearSegment(FlightVector from,FlightVector to,double margin=0,bool allowSupport=false)=>!Blocked;
            public bool TryLanding(FlightVector position,bool emergency,out FlightVector contact){contact=new(position.East,position.North,.535);return true;}
        }
        private static readonly FlightVector Home=new(-20,-15,.535);
        private static BodySnapshot Body(double up=4.535)=>new(new FlightVector(-20,-15,up),FlightVector.Zero,FlightVector.Vertical,new FlightVector(1,0,0),FlightVector.Zero,true);
        private static SafetyDecision Admit(FlightCommand command,FlightState state=FlightState.Hovering,double battery=100)
            =>new SafetyEngine(new SafetySettings(),new Geometry()).Authorize(command,state,Body(),battery,Home,out _);

        [TestCase(FlightState.Landed,CommandKind.MoveTo,false)]
        [TestCase(FlightState.Landed,CommandKind.Takeoff,false)]
        [TestCase(FlightState.Flying,CommandKind.Disarm,false)]
        [TestCase(FlightState.Armed,CommandKind.Takeoff,true)]
        [TestCase(FlightState.Hovering,CommandKind.MoveTo,true)]
        [TestCase(FlightState.Flying,CommandKind.ReturnHome,true)]
        [TestCase(FlightState.Emergency,CommandKind.MoveTo,false)]
        public void StateCommandRules(FlightState state,CommandKind command,bool allowed)=>Assert.That(FlightStateMachine.CanCommand(state,command),Is.EqualTo(allowed));

        [Test]
        public void TransitionSequenceRejectsIllegalEvents()
        {
            var m=new FlightStateMachine();
            Assert.That(m.TryAdvance(FlightEvent.Move),Is.False);
            foreach(var e in new[]{FlightEvent.Arm,FlightEvent.Takeoff,FlightEvent.Settled,FlightEvent.Move,FlightEvent.Return,FlightEvent.Land,FlightEvent.Touchdown})
                Assert.That(m.TryAdvance(e),Is.True,e.ToString());
            Assert.That(m.State,Is.EqualTo(FlightState.Landed));
        }
        [TestCase(13,ReasonCode.AltitudeLimit)]
        [TestCase(.5,ReasonCode.AltitudeLimit)]
        [TestCase(double.NaN,ReasonCode.NonFinite)]
        public void TakeoffAltitude(double altitude,ReasonCode reason)=>Assert.That(Admit(new TakeoffCommand(altitude),FlightState.Armed).Reason,Is.EqualTo(reason));
        [TestCase(32,0)] [TestCase(0,27)] [TestCase(-32,0)] [TestCase(0,-27)]
        public void GeofenceIncludesDroneEnvelope(double east,double north)=>Assert.That(Admit(new MoveToCommand(new FlightVector(east,north,4))).Reason,Is.EqualTo(ReasonCode.Geofence));
        [TestCase(0)] [TestCase(-1)] [TestCase(6)]
        public void InvalidSpeed(double speed)=>Assert.That(Admit(new MoveToCommand(new FlightVector(0,0,4),speed)).Reason,Is.EqualTo(ReasonCode.SpeedLimit));
        [Test]
        public void MalformedCommandsRejected()
        {
            Assert.That(Admit(null).Reason,Is.EqualTo(ReasonCode.InvalidCommand));
            Assert.That(Admit(new MoveToCommand(new FlightVector(double.NaN,0,4))).Reason,Is.EqualTo(ReasonCode.NonFinite));
            Assert.That(Admit(new HoldCommand(-1)).Reason,Is.EqualTo(ReasonCode.InvalidCommand));
        }
        [TestCase(25,SafetyStatus.ReturnHomeRequired)]
        [TestCase(12,SafetyStatus.EmergencyLandRequired)]
        [TestCase(26,SafetyStatus.Approved)]
        public void BatteryThresholds(double percent,SafetyStatus expected)=>Assert.That(new SafetyEngine(new SafetySettings(),new Geometry()).BatteryDecision(percent,FlightState.Flying).Status,Is.EqualTo(expected));
        [Test]
        public void LowBatteryCannotTakeoffOrOverridePolicy()
        {
            Assert.That(Admit(new TakeoffCommand(4),FlightState.Armed,29).Reason,Is.EqualTo(ReasonCode.BatteryTooLow));
            Assert.That(Admit(new MoveToCommand(new FlightVector(0,0,4)),FlightState.Hovering,24).Reason,Is.EqualTo(ReasonCode.BatteryTooLow));
        }
        [Test]
        public void BlockedRouteProducesNoAuthorization()
        {
            var engine=new SafetyEngine(new SafetySettings(),new Geometry{Blocked=true});
            var result=engine.Authorize(new MoveToCommand(new FlightVector(0,0,4)),FlightState.Hovering,Body(),100,Home,out var token);
            Assert.That(result.Reason,Is.EqualTo(ReasonCode.BlockedRoute));Assert.That(token,Is.Null);
        }
        [Test]
        public void ControllerNeedsPermissionAndBoundsOutput()
        {
            var c=new PositionController(new FlightTuning());
            Assert.Throws<ArgumentNullException>(()=>c.Calculate(null,Body(),Home,0,false,.02));
            new SafetyEngine(new SafetySettings(),new Geometry()).Authorize(new MoveToCommand(new FlightVector(0,0,8)),FlightState.Hovering,Body(),100,Home,out var token);
            var output=c.Calculate(token,Body(),new FlightVector(0,0,8),2,false,.02);
            Assert.That(output.ThrustAcceleration,Is.InRange(0,20));Assert.That(output.AngularAcceleration.Length,Is.LessThanOrEqualTo(15.00001));
        }
        [Test]
        public void BatteryConsumptionIsDeterministicAndMotionCostsMore()
        {
            var a=new BatteryModel(new BatterySettings());var b=new BatteryModel(new BatterySettings());
            for(int i=0;i<1000;i++){a.Tick(FlightState.Flying,2,.02);b.Tick(FlightState.Flying,2,.02);}
            Assert.That(a.Percent,Is.EqualTo(b.Percent));Assert.That(a.Percent,Is.LessThan(100));
            var hover=new BatteryModel(new BatterySettings());hover.Tick(FlightState.Hovering,0,20);
            Assert.That(hover.Percent,Is.GreaterThan(a.Percent));
        }
    }
}
