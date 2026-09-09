using System;

namespace MicroHawk.Contracts
{
    public readonly struct BodySnapshot
    {
        public readonly FlightVector Position, Velocity, BodyUp, Forward, AngularVelocity;
        public readonly bool GroundContact;
        public BodySnapshot(FlightVector p,FlightVector v,FlightVector up,FlightVector forward,FlightVector angular,bool grounded)
        {Position=p;Velocity=v;BodyUp=up;Forward=forward;AngularVelocity=angular;GroundContact=grounded;}
        public double Heading=>Math.Atan2(Forward.North,Forward.East);
        public bool IsFinite=>Position.IsFinite&&Velocity.IsFinite&&BodyUp.IsFinite&&Forward.IsFinite&&AngularVelocity.IsFinite;
    }
    public readonly struct Actuation
    {
        public readonly double ThrustAcceleration;
        public readonly FlightVector AngularAcceleration;
        public Actuation(double thrust,FlightVector angular){ThrustAcceleration=thrust;AngularAcceleration=angular;}
    }
    public interface IRouteGeometry
    {
        bool ClearSegment(FlightVector from,FlightVector to,double margin=0,bool allowSupport=false);
        bool TryLanding(FlightVector position,bool emergency,out FlightVector contactPosition);
    }
    /// <summary>Physics-specific adapter seam inside a trusted execution endpoint, never an AI tool.</summary>
    public interface ISimulatorAdapter : IRouteGeometry
    {
        BodySnapshot Read();
        void Apply(Actuation actuation);
        void Step(double seconds);
        void RestoreHome();
    }
    /// <summary>Only this narrow command/telemetry interface is intended for future transport.</summary>
    public interface IFlightOperations
    {
        long Submit(FlightCommand command,long expectedSession);
        TelemetrySnapshot Telemetry { get; }
        event Action<CommandResult> CommandChanged;
        event Action<TelemetrySnapshot> TelemetryPublished;
    }
    // Local lifecycle administration. Keep it out of autonomous command tools.
    public interface ISimulationReset { void ResetSimulation(); }
    public sealed class TelemetrySnapshot
    {
        public const int SchemaVersion=1;
        public const string CoordinateFrame="facility_enu_meters";
        public long Session { get; }
        public long Tick { get; }
        public double SimulationSeconds { get; }
        public BodySnapshot Body { get; }
        public double Altitude { get; }
        public double BatteryPercent { get; }
        public FlightState State { get; }
        public bool Armed=>State!=FlightState.Landed;
        public CommandKind? CurrentCommand { get; }
        public long? CurrentCommandId { get; }
        public FlightVector? Target { get; }
        public FlightVector Home { get; }
        public SafetyDecision Safety { get; }
        public TelemetrySnapshot(long session,long tick,double seconds,BodySnapshot body,double battery,FlightState state,CommandKind? command,long? id,FlightVector? target,FlightVector home,SafetyDecision safety)
        {Session=session;Tick=tick;SimulationSeconds=seconds;Body=body;BatteryPercent=battery;State=state;CurrentCommand=command;CurrentCommandId=id;Target=target;Home=home;Altitude=body.Position.Up-home.Up;Safety=safety;}
    }
}
