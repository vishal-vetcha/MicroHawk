using System;
using MicroHawk.Contracts;
using UnityEngine;

namespace MicroHawk.Simulation
{
    public static class UnityCoordinates
    {
        public static Vector3 ToUnity(EnuPosition enu)
        {
            if (Math.Abs(enu.East) > float.MaxValue || Math.Abs(enu.North) > float.MaxValue || Math.Abs(enu.Up) > float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(enu));
            return new Vector3((float)enu.East, (float)enu.Up, (float)enu.North);
        }

        public static EnuPosition ToEnu(Vector3 position) => new(position.x, position.z, position.y);

        // Body-forward direction from ENU yaw; zero points east, positive turns north.
        public static Vector3 Heading(double yawRadians)
        {
            if (double.IsNaN(yawRadians) || double.IsInfinity(yawRadians))
                throw new ArgumentOutOfRangeException(nameof(yawRadians));
            return new Vector3((float)Math.Cos(yawRadians), 0f, (float)Math.Sin(yawRadians));
        }
    }
}
