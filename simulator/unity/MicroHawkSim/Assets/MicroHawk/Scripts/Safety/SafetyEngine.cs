using System;
using System.Collections.ObjectModel;
using MicroHawk.Contracts;

namespace MicroHawk.Safety
{
    /// <summary>Only Safety can construct authorization; flight code cannot fabricate permission.</summary>
    public sealed class AuthorizedFlightAction
    {
        public FlightCommand Command { get; }
        public ReadOnlyCollection<FlightVector> Targets { get; }
        public double Speed { get; }
        public ReasonCode Cause { get; }
        public bool Emergency=>Cause==ReasonCode.CriticalBattery;
        internal AuthorizedFlightAction(FlightCommand command,FlightVector[] targets,double speed,ReasonCode cause)
        {Command=command;Targets=Array.AsReadOnly((FlightVector[])targets.Clone());Speed=speed;Cause=cause;}
    }

    public sealed class SafetyEngine
    {
        private readonly SafetySettings limits;
        private readonly IRouteGeometry geometry;
        public SafetyEngine(SafetySettings settings,IRouteGeometry geometry){limits=settings.ValidatedCopy();this.geometry=geometry??throw new ArgumentNullException(nameof(geometry));}
        public SafetyDecision BatteryDecision(double percent,FlightState state)
        {
            if(!FlightVector.Finite(percent))return new(SafetyStatus.EmergencyLandRequired,ReasonCode.CriticalBattery);
            if(!FlightStateMachine.Airborne(state))return new(SafetyStatus.Approved);
            if(percent<=limits.CriticalBattery)return new(SafetyStatus.EmergencyLandRequired,ReasonCode.CriticalBattery);
            if(percent<=limits.LowBattery)return new(SafetyStatus.ReturnHomeRequired,ReasonCode.LowBattery);
            return new(SafetyStatus.Approved);
        }
        public ReasonCode ValidateTarget(FlightVector target,FlightVector home)
        {
            if(!target.IsFinite)return ReasonCode.NonFinite;
            if(target.Up-home.Up<limits.MinFlightAltitude||target.Up-home.Up>limits.MaxAltitude)return ReasonCode.AltitudeLimit;
            return InsideFence(target)?ReasonCode.None:ReasonCode.Geofence;
        }
        public bool InsideFence(FlightVector p)=>p.East>=limits.MinEast+limits.FootprintRadius&&p.East<=limits.MaxEast-limits.FootprintRadius&&p.North>=limits.MinNorth+limits.FootprintRadius&&p.North<=limits.MaxNorth-limits.FootprintRadius;

        public SafetyDecision Authorize(FlightCommand command,FlightState state,BodySnapshot body,double battery,FlightVector home,out AuthorizedFlightAction action,ReasonCode cause=ReasonCode.None)
        {
            action=null;
            SafetyDecision Reject(ReasonCode reason)=>new(SafetyStatus.Rejected,reason);
            if(command==null)return Reject(ReasonCode.InvalidCommand);
            if(!body.IsFinite||!FlightVector.Finite(battery))return Reject(ReasonCode.NonFinite);
            bool emergency=cause==ReasonCode.CriticalBattery;
            if(!emergency&&!FlightStateMachine.CanCommand(state,command.Kind))return Reject(ReasonCode.InvalidState);
            if(state==FlightState.Emergency&&!emergency)return Reject(ReasonCode.FailsafeActive);
            if(!InsideFence(body.Position))return Reject(ReasonCode.Geofence);
            if(!emergency&&battery<=limits.LowBattery&&command.Kind is not (CommandKind.ReturnHome or CommandKind.Land or CommandKind.Disarm))return Reject(ReasonCode.BatteryTooLow);
            double speed=limits.DefaultSpeed;
            FlightVector[] targets=Array.Empty<FlightVector>();
            switch(command)
            {
                case ArmCommand:
                    if(!body.GroundContact)return Reject(ReasonCode.InvalidState);
                    break;
                case DisarmCommand:break;
                case TakeoffCommand takeoff:
                    if(!FlightVector.Finite(takeoff.Altitude))return Reject(ReasonCode.NonFinite);
                    if(battery<limits.TakeoffBattery)return Reject(ReasonCode.BatteryTooLow);
                    targets=new[]{new FlightVector(body.Position.East,body.Position.North,home.Up+takeoff.Altitude)};
                    break;
                case MoveToCommand move:
                    speed=move.Speed??limits.DefaultSpeed;
                    if(!FlightVector.Finite(speed))return Reject(ReasonCode.NonFinite);
                    if(speed<=0||speed>limits.MaxSpeed)return Reject(ReasonCode.SpeedLimit);
                    targets=new[]{move.Position};break;
                case HoldCommand hold:
                    if(hold.DurationSeconds.HasValue&&(!FlightVector.Finite(hold.DurationSeconds.Value)||hold.DurationSeconds.Value<=0||hold.DurationSeconds.Value>3600))return Reject(ReasonCode.InvalidCommand);
                    targets=new[]{body.Position};break;
                case LandCommand:
                    if(!geometry.TryLanding(body.Position,emergency,out var contact))return Reject(ReasonCode.NoLandingSite);
                    targets=new[]{contact};break;
                case ReturnHomeCommand:
                    double altitude=Math.Max(body.Position.Up,home.Up+limits.ReturnAltitude);
                    targets=new[]{new FlightVector(body.Position.East,body.Position.North,altitude),new FlightVector(home.East,home.North,altitude),home};break;
                default:return Reject(ReasonCode.InvalidCommand);
            }
            var previous=body.Position;
            for(int i=0;i<targets.Length;i++)
            {
                bool landing=command is LandCommand||(command is ReturnHomeCommand&&i==targets.Length-1);
                bool hold=command is HoldCommand;
                if(!landing&&!hold){var reason=ValidateTarget(targets[i],home);if(reason!=ReasonCode.None)return Reject(reason);}
                if(!InsideFence(targets[i]))return Reject(ReasonCode.Geofence);
                if(!geometry.ClearSegment(previous,targets[i],0,landing||command is TakeoffCommand))return Reject(ReasonCode.BlockedRoute);
                previous=targets[i];
            }
            if(command is MoveToCommand or HoldCommand or ReturnHomeCommand)
            {
                var stop=body.Position+body.Velocity.Normalized*(body.Velocity.Length*body.Velocity.Length/(2*limits.BrakingAcceleration)+body.Velocity.Length*limits.ReactionSeconds+.2);
                if(!InsideFence(stop)||!geometry.ClearSegment(body.Position,stop))return Reject(ReasonCode.BlockedRoute);
            }
            action=new AuthorizedFlightAction(command,targets,speed,cause);
            return new(SafetyStatus.Approved,cause);
        }

        public AuthorizedFlightAction AuthorizeBrake(BodySnapshot body,ReasonCode cause)
        {
            if(!body.IsFinite)throw new ArgumentException("Cannot control invalid physical state.");
            return new AuthorizedFlightAction(new HoldCommand(),new[]{body.Position},limits.DefaultSpeed,cause);
        }

        public bool RuntimeRouteClear(BodySnapshot body,FlightVector target,bool support)
        {
            if(!body.IsFinite||!InsideFence(body.Position))return false;
            var stop=body.Position+body.Velocity.Normalized*(body.Velocity.Length*body.Velocity.Length/(2*limits.BrakingAcceleration)+body.Velocity.Length*limits.ReactionSeconds+.15);
            if(support&&stop.Up<Math.Min(body.Position.Up,target.Up))stop=new FlightVector(stop.East,stop.North,Math.Min(body.Position.Up,target.Up));
            return InsideFence(stop)&&geometry.ClearSegment(body.Position,stop,0,support)&&geometry.ClearSegment(body.Position,target,0,support);
        }
    }
}




