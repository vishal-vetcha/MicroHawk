using System;
using MicroHawk.Contracts;

namespace MicroHawk.Safety
{
    [Serializable]
    public sealed class SafetySettings
    {
        public double MaxAltitude=12, MinFlightAltitude=1.5;
        public double MinEast=-32, MaxEast=32, MinNorth=-27, MaxNorth=27;
        public double MaxSpeed=3, DefaultSpeed=2.5, BrakingAcceleration=2, ReactionSeconds=.6;
        public double ReturnAltitude=7, FootprintRadius=.65;
        public double TakeoffBattery=30, LowBattery=25, CriticalBattery=12;
        public SafetySettings ValidatedCopy()
        {
            var c=(SafetySettings)MemberwiseClone();
            foreach(var value in new[]{MaxAltitude,MinFlightAltitude,MinEast,MaxEast,MinNorth,MaxNorth,MaxSpeed,DefaultSpeed,BrakingAcceleration,ReactionSeconds,ReturnAltitude,FootprintRadius,TakeoffBattery,LowBattery,CriticalBattery})
                if(!FlightVector.Finite(value)) throw new ArgumentException("Safety settings must be finite.");
            if(MinFlightAltitude<=0||MaxAltitude<=MinFlightAltitude||ReturnAltitude<MinFlightAltitude||ReturnAltitude>MaxAltitude||MaxEast-MinEast<2*FootprintRadius||MaxNorth-MinNorth<2*FootprintRadius||FootprintRadius<.6||MaxSpeed<=0||MaxSpeed>5||DefaultSpeed<=0||DefaultSpeed>MaxSpeed||BrakingAcceleration<=0||ReactionSeconds<.2||CriticalBattery<=0||LowBattery<=CriticalBattery||TakeoffBattery<LowBattery||TakeoffBattery>100)
                throw new ArgumentException("Inconsistent safety limits.");
            return c;
        }
    }

    public enum FlightEvent { Arm, Disarm, Takeoff, Move, Hold, Return, Land, EmergencyLand, SafetyHold, Settled, Touchdown }
    /// <summary>One transition table shared by admission and execution.</summary>
    public sealed class FlightStateMachine
    {
        public FlightState State { get; private set; }=FlightState.Landed;
        public static bool CanCommand(FlightState state,CommandKind command)=>command switch
        {
            CommandKind.Arm=>state==FlightState.Landed,
            CommandKind.Disarm=>state==FlightState.Armed,
            CommandKind.Takeoff=>state==FlightState.Armed,
            CommandKind.MoveTo=>state is FlightState.Hovering or FlightState.Holding or FlightState.Flying,
            CommandKind.Hold=>state is FlightState.Hovering or FlightState.Flying or FlightState.Holding or FlightState.TakingOff or FlightState.ReturningHome,
            CommandKind.Land=>state is FlightState.Hovering or FlightState.Holding or FlightState.Flying,
            CommandKind.ReturnHome=>state is FlightState.Hovering or FlightState.Holding or FlightState.Flying or FlightState.TakingOff,
            _=>false
        };
        public static bool Airborne(FlightState state)=>state!=FlightState.Landed&&state!=FlightState.Armed;
        public bool TryAdvance(FlightEvent transition)
        {
            FlightState? next=transition switch
            {
                FlightEvent.Arm when CanCommand(State,CommandKind.Arm)=>FlightState.Armed,
                FlightEvent.Disarm when CanCommand(State,CommandKind.Disarm)=>FlightState.Landed,
                FlightEvent.Takeoff when CanCommand(State,CommandKind.Takeoff)=>FlightState.TakingOff,
                FlightEvent.Move when CanCommand(State,CommandKind.MoveTo)=>FlightState.Flying,
                FlightEvent.Hold when CanCommand(State,CommandKind.Hold)=>FlightState.Holding,
                FlightEvent.Return when CanCommand(State,CommandKind.ReturnHome)=>FlightState.ReturningHome,
                FlightEvent.Land when CanCommand(State,CommandKind.Land)||State==FlightState.ReturningHome=>FlightState.Landing,
                FlightEvent.EmergencyLand when Airborne(State)=>FlightState.Emergency,
                FlightEvent.SafetyHold when Airborne(State)=>FlightState.Holding,
                FlightEvent.Settled when State is FlightState.TakingOff or FlightState.Flying or FlightState.Holding=>FlightState.Hovering,
                FlightEvent.Touchdown when State is FlightState.Landing or FlightState.Emergency=>FlightState.Landed,
                _=>null
            };
            if(!next.HasValue)return false;
            State=next.Value;return true;
        }
        public void Reset()=>State=FlightState.Landed;
    }
}


