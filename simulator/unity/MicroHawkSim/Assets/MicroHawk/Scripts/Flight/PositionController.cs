using System;
using MicroHawk.Contracts;
using MicroHawk.Safety;

namespace MicroHawk.Flight
{
    /// <summary>Pure bounded position/velocity and attitude controller. Permission is mandatory.</summary>
    public sealed class PositionController
    {
        private readonly FlightTuning tuning;
        private double verticalIntegral;
        public PositionController(FlightTuning tuning){this.tuning=tuning.ValidatedCopy();}
        public void Reset()=>verticalIntegral=0;
        public Actuation Calculate(AuthorizedFlightAction permission,BodySnapshot body,FlightVector target,double heading,bool landing,double dt)
        {
            if(permission==null)throw new ArgumentNullException(nameof(permission));
            var error=target-body.Position;
            var horizontal=new FlightVector(error.East,error.North,0)*tuning.PositionGain;
            horizontal=FlightVector.Clamp(horizontal,permission.Speed);
            double vz=FlightVector.Limit(error.Up*tuning.PositionGain,-(landing?tuning.LandingSpeed:tuning.MaxDescentSpeed),tuning.MaxClimbSpeed);
            if(landing&&error.Up>-.08)vz=-.12; // Gentle contact request; completion still requires actual support and settling.
            var velocityError=new FlightVector(horizontal.East,horizontal.North,vz)-body.Velocity;
            var lateral=FlightVector.Clamp(new FlightVector(velocityError.East,velocityError.North,0)*tuning.VelocityGain,tuning.MaxHorizontalAcceleration);
            verticalIntegral=FlightVector.Limit(verticalIntegral+velocityError.Up*dt,-1.5,1.5);
            double az=FlightVector.Limit(velocityError.Up*tuning.VelocityGain+verticalIntegral*tuning.VerticalIntegralGain,-3,3);
            var desiredUp=new FlightVector(lateral.East,lateral.North,9.81+az).Normalized;
            var tilt=FlightVector.Cross(body.BodyUp,desiredUp)*tuning.AttitudeGain;
            var forward=new FlightVector(body.Forward.East,body.Forward.North,0).Normalized;
            var desiredForward=new FlightVector(Math.Cos(heading),Math.Sin(heading),0);
            double yaw=Math.Atan2(FlightVector.Cross(forward,desiredForward).Up,FlightVector.Dot(forward,desiredForward));
            var angular=tilt+FlightVector.Vertical*(yaw*tuning.AttitudeGain*.5)-body.AngularVelocity*tuning.AngularDamping;
            angular=FlightVector.Clamp(angular,tuning.MaxAngularAcceleration);
            double thrust=(9.81+az)/Math.Max(.7,body.BodyUp.Up);
            return new Actuation(FlightVector.Limit(thrust,0,tuning.MaxThrustAcceleration),angular);
        }
    }
}

