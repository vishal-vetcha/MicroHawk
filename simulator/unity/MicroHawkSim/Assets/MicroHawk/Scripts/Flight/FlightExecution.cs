using System;
using MicroHawk.Contracts;
using MicroHawk.Safety;

namespace MicroHawk.Flight
{
    /// <summary>One authorized action at a time; outcomes require measured physical settling.</summary>
    public sealed class FlightExecution
    {
        private readonly FlightStateMachine machine=new();
        private readonly PositionController controller;
        private readonly FlightTuning tuning;
        private AuthorizedFlightAction permission;
        private int leg;
        private double settled,elapsed,holdElapsed,heading;
        public FlightState State=>machine.State;
        public AuthorizedFlightAction Permission=>permission;
        public long? ActiveId { get; private set; }
        public CommandKind? ActiveCommand=>ActiveId.HasValue?permission?.Command.Kind:null;
        public FlightVector? Target=>permission!=null&&permission.Targets.Count>0?permission.Targets[leg]:null;
        public bool IsLanding=>State==FlightState.Landing||(State==FlightState.Emergency&&permission?.Command is LandCommand);
        public event Action<long,CommandKind,ExecutionStatus,ReasonCode> Result;
        public FlightExecution(FlightTuning tuning){this.tuning=tuning.ValidatedCopy();controller=new PositionController(tuning);}
        public void Reset(){machine.Reset();controller.Reset();permission=null;ActiveId=null;leg=0;settled=elapsed=holdElapsed=heading=0;}
        public void Interrupt(ReasonCode reason,ExecutionStatus status=ExecutionStatus.Interrupted)
        {
            if(ActiveId.HasValue)Result?.Invoke(ActiveId.Value,permission.Command.Kind,status,reason);
            ActiveId=null;
        }
        public void Accept(AuthorizedFlightAction action,long id,BodySnapshot body)
        {
            Interrupt(action.Cause==ReasonCode.None?ReasonCode.Superseded:action.Cause);
            var transition=action.Emergency?FlightEvent.EmergencyLand:action.Command.Kind switch
            {
                CommandKind.Arm=>FlightEvent.Arm,CommandKind.Disarm=>FlightEvent.Disarm,CommandKind.Takeoff=>FlightEvent.Takeoff,
                CommandKind.MoveTo=>FlightEvent.Move,CommandKind.Hold=>action.Cause==ReasonCode.None?FlightEvent.Hold:FlightEvent.SafetyHold,
                CommandKind.Land=>FlightEvent.Land,CommandKind.ReturnHome=>FlightEvent.Return,_=>throw new ArgumentOutOfRangeException()
            };
            if(!machine.TryAdvance(transition))throw new InvalidOperationException("Authorization/state mismatch.");
            permission=action;ActiveId=id;leg=0;settled=elapsed=holdElapsed=0;heading=body.Heading;controller.Reset();
            if(action.Targets.Count>0)
            {
                var direction=action.Targets[action.Targets.Count>1?1:0]-body.Position;
                if(direction.HorizontalLength>.4)heading=Math.Atan2(direction.North,direction.East);
            }
            Result?.Invoke(id,action.Command.Kind,ExecutionStatus.Executing,action.Cause);
            if(action.Command is ArmCommand or DisarmCommand)Complete();
        }
        private void Complete(ReasonCode reason=ReasonCode.None)
        {
            if(ActiveId.HasValue)Result?.Invoke(ActiveId.Value,permission.Command.Kind,ExecutionStatus.Completed,reason);
            ActiveId=null;
        }
        public Actuation Tick(BodySnapshot body,double dt)
        {
            if(!FlightStateMachine.Airborne(State)||permission==null||!Target.HasValue)return new Actuation(0,FlightVector.Zero);
            elapsed+=dt;
            if(ActiveId.HasValue&&permission.Command is not HoldCommand&&elapsed>180)
            {
                Interrupt(ReasonCode.Timeout,ExecutionStatus.Failed);
                // Runtime receives the failed outcome and replaces the objective with an authorized brake.
                return new Actuation(9.81,FlightVector.Zero);
            }
            bool atTarget=(Target.Value-body.Position).Length<tuning.PositionTolerance&&body.Velocity.Length<tuning.SpeedTolerance;
            bool landed=body.GroundContact&&body.Velocity.Length<.14&&body.BodyUp.Up>.97;
            settled=(IsLanding?landed:atTarget)?settled+dt:0;
            if(IsLanding&&settled>=.8)
            {
                machine.TryAdvance(FlightEvent.Touchdown);Complete(ReasonCode.Touchdown);controller.Reset();
                return new Actuation(0,FlightVector.Zero);
            }
            if(!IsLanding&&settled>=tuning.SettlingSeconds)
            {
                if(permission.Command is ReturnHomeCommand&&leg<permission.Targets.Count-1)
                {
                    leg++;settled=0;controller.Reset();
                    if(leg==permission.Targets.Count-1)machine.TryAdvance(FlightEvent.Land);
                }
                else if(permission.Command is HoldCommand hold)
                {
                    holdElapsed+=dt;
                    if(hold.DurationSeconds.HasValue&&holdElapsed>=hold.DurationSeconds.Value&&State!=FlightState.Emergency)
                    {machine.TryAdvance(FlightEvent.Settled);Complete();}
                }
                else if(State is FlightState.TakingOff or FlightState.Flying)
                {machine.TryAdvance(FlightEvent.Settled);Complete();}
            }
            return controller.Calculate(permission,body,Target.Value,heading,IsLanding,dt);
        }
    }
}
