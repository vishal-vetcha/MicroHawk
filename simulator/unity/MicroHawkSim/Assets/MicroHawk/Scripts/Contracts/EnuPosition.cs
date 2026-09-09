using System;

namespace MicroHawk.Contracts
{
    /// <summary>Facility-local meters: east, north, up. Not a Unity transform.</summary>
    public readonly struct EnuPosition
    {
        public double East { get; }
        public double North { get; }
        public double Up { get; }

        public EnuPosition(double east, double north, double up)
        {
            if (!Finite(east) || !Finite(north) || !Finite(up))
                throw new ArgumentOutOfRangeException(nameof(east), "Coordinates must be finite meters.");
            East = east;
            North = north;
            Up = up;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
