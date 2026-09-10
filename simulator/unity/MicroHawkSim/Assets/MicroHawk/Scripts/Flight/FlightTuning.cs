using System;
using MicroHawk.Contracts;

namespace MicroHawk.Flight
{
    [Serializable]
    public sealed class FlightTuning
    {
        public double PositionGain=1.1, VelocityGain=2.7, VerticalIntegralGain=.7;
        public double MaxHorizontalAcceleration=2.5, MaxThrustAcceleration=20;
        public double MaxClimbSpeed=1.6, MaxDescentSpeed=.7, LandingSpeed=.45;
        public double AttitudeGain=26, AngularDamping=9, MaxAngularAcceleration=15;
        public double PositionTolerance=.18, SpeedTolerance=.18, SettlingSeconds=.6;
        public FlightTuning ValidatedCopy()
        {
            foreach(var v in new[]{PositionGain,VelocityGain,VerticalIntegralGain,MaxHorizontalAcceleration,MaxThrustAcceleration,MaxClimbSpeed,MaxDescentSpeed,LandingSpeed,AttitudeGain,AngularDamping,MaxAngularAcceleration,PositionTolerance,SpeedTolerance,SettlingSeconds})
                if(!FlightVector.Finite(v)||v<=0)throw new ArgumentException("Flight tuning must be positive and finite.");
            if(MaxThrustAcceleration<10||MaxThrustAcceleration>25||MaxHorizontalAcceleration>3||LandingSpeed>1)throw new ArgumentException("Unsupported flight envelope.");
            return (FlightTuning)MemberwiseClone();
        }
    }
    [Serializable]
    public sealed class BatterySettings
    {
        public double InitialPercent=100, ArmedDrain=.005, HoverDrain=.08, MovementDrain=.025;
        public BatterySettings ValidatedCopy()
        {
            foreach(var v in new[]{InitialPercent,ArmedDrain,HoverDrain,MovementDrain})if(!FlightVector.Finite(v)||v<0)throw new ArgumentException("Invalid battery settings.");
            if(InitialPercent>100)throw new ArgumentException("Battery must be a percentage.");
            return (BatterySettings)MemberwiseClone();
        }
    }
    public sealed class BatteryModel
    {
        private readonly BatterySettings settings;
        public double Percent { get; private set; }
        public BatteryModel(BatterySettings settings){this.settings=settings.ValidatedCopy();Percent=this.settings.InitialPercent;}
        public void InjectLowerCharge(double percent)
        {
            if(!FlightVector.Finite(percent)||percent<0||percent>Percent)throw new ArgumentException("Demo injection may only decrease charge.");
            Percent=percent;
        }
        public void Tick(FlightState state,double speed,double dt)
        {
            double drain=state==FlightState.Landed?0:state==FlightState.Armed?settings.ArmedDrain:settings.HoverDrain+Math.Max(0,speed)*settings.MovementDrain;
            Percent=Math.Max(0,Percent-drain*dt);
        }
    }
}
