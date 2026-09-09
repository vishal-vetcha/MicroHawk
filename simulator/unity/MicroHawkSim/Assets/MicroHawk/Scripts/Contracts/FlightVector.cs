using System;

namespace MicroHawk.Contracts
{
    /// <summary>Right-handed ENU vector in SI units; no engine dependency.</summary>
    public readonly struct FlightVector
    {
        public readonly double East, North, Up;
        public FlightVector(double east,double north,double up){East=east;North=north;Up=up;}
        public bool IsFinite => Finite(East)&&Finite(North)&&Finite(Up);
        public double Length => Math.Sqrt(East*East+North*North+Up*Up);
        public double HorizontalLength => Math.Sqrt(East*East+North*North);
        public FlightVector Normalized => Length>1e-9 ? this/Length : Zero;
        public static FlightVector Zero => new(0,0,0);
        public static FlightVector Vertical => new(0,0,1);
        public static FlightVector operator +(FlightVector a,FlightVector b)=>new(a.East+b.East,a.North+b.North,a.Up+b.Up);
        public static FlightVector operator -(FlightVector a,FlightVector b)=>new(a.East-b.East,a.North-b.North,a.Up-b.Up);
        public static FlightVector operator *(FlightVector a,double b)=>new(a.East*b,a.North*b,a.Up*b);
        public static FlightVector operator /(FlightVector a,double b)=>a*(1/b);
        public static double Dot(FlightVector a,FlightVector b)=>a.East*b.East+a.North*b.North+a.Up*b.Up;
        public static FlightVector Cross(FlightVector a,FlightVector b)=>new(a.North*b.Up-a.Up*b.North,a.Up*b.East-a.East*b.Up,a.East*b.North-a.North*b.East);
        public static FlightVector Clamp(FlightVector v,double max)=>v.Length>max?v.Normalized*max:v;
        public static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        public static double Limit(double v,double min,double max)=>Math.Max(min,Math.Min(max,v));
        public override string ToString()=>$"({East:F2}, {North:F2}, {Up:F2})";
    }
}
