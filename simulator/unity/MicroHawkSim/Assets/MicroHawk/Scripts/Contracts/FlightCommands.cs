namespace MicroHawk.Contracts
{
    public enum CommandKind { Arm, Disarm, Takeoff, MoveTo, Hold, Land, ReturnHome }
    public enum FlightState { Landed, Armed, TakingOff, Hovering, Flying, Holding, ReturningHome, Landing, Emergency }
    public enum ExecutionStatus { Accepted, Rejected, Executing, Completed, Failed, Interrupted }
    public enum SafetyStatus { Approved, Rejected, ReturnHomeRequired, EmergencyLandRequired }
    public enum ReasonCode { None, InvalidCommand, InvalidState, NonFinite, AltitudeLimit, Geofence, SpeedLimit, BlockedRoute, NoLandingSite, BatteryTooLow, LowBattery, CriticalBattery, FailsafeActive, Busy, Timeout, Obstacle, Reset, Superseded, Touchdown, QueueFull, StaleSession, ConnectionLoss }

    public abstract class FlightCommand
    {
        public abstract CommandKind Kind { get; }
        internal FlightCommand() { }
    }
    public sealed class ArmCommand : FlightCommand { public override CommandKind Kind=>CommandKind.Arm; }
    public sealed class DisarmCommand : FlightCommand { public override CommandKind Kind=>CommandKind.Disarm; }
    /// <summary>Altitude above the recorded home body-reference height, meters.</summary>
    public sealed class TakeoffCommand : FlightCommand
    {
        public override CommandKind Kind=>CommandKind.Takeoff;
        public double Altitude { get; }
        public TakeoffCommand(double altitude){Altitude=altitude;}
    }
    public sealed class MoveToCommand : FlightCommand
    {
        public override CommandKind Kind=>CommandKind.MoveTo;
        public FlightVector Position { get; }
        public double? Speed { get; }
        public MoveToCommand(FlightVector position,double? speed=null){Position=position;Speed=speed;}
    }
    public sealed class HoldCommand : FlightCommand
    {
        public override CommandKind Kind=>CommandKind.Hold;
        public double? DurationSeconds { get; }
        public HoldCommand(double? durationSeconds=null){DurationSeconds=durationSeconds;}
    }
    public sealed class LandCommand : FlightCommand { public override CommandKind Kind=>CommandKind.Land; }
    public sealed class ReturnHomeCommand : FlightCommand { public override CommandKind Kind=>CommandKind.ReturnHome; }

    public readonly struct CommandResult
    {
        public readonly long Session, Id, Tick;
        public readonly CommandKind? Command;
        public readonly ExecutionStatus Status;
        public readonly ReasonCode Reason;
        public CommandResult(long session,long id,long tick,CommandKind? command,ExecutionStatus status,ReasonCode reason=ReasonCode.None)
        {Session=session;Id=id;Tick=tick;Command=command;Status=status;Reason=reason;}
    }
    public readonly struct SafetyDecision
    {
        public readonly SafetyStatus Status;
        public readonly ReasonCode Reason;
        public bool Approved=>Status==SafetyStatus.Approved;
        public SafetyDecision(SafetyStatus status,ReasonCode reason=ReasonCode.None){Status=status;Reason=reason;}
    }
}

