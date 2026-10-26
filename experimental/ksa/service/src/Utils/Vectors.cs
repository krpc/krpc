using System;
using Brutal.Numerics;
using KSA;

namespace KRPC.KSA.Utils
{
    static class Vectors
    {
        public static Tuple<double, double, double> ToTuple (double3 vector)
        {
            return Tuple.Create (vector.X, vector.Y, vector.Z);
        }

        public static double3 ToVector (Tuple<double, double, double> vector)
        {
            if (vector == null)
                throw new ArgumentNullException (nameof (vector));
            return new double3 (vector.Item1, vector.Item2, vector.Item3);
        }

        public static Tuple<double, double, double, double> ToTuple (doubleQuat rotation)
        {
            return Tuple.Create (rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static doubleQuat ToRotation (Tuple<double, double, double, double> rotation)
        {
            if (rotation == null)
                throw new ArgumentNullException (nameof (rotation));
            return new doubleQuat (rotation.Item1, rotation.Item2, rotation.Item3, rotation.Item4).Normalized ();
        }

        /// <summary>
        /// The vehicle's angular velocity in the ecliptic frame. The game gives the rates in
        /// the vehicle's body frame.
        /// </summary>
        public static double3 AngularVelocityEcl (Vehicle vehicle)
        {
            return vehicle.GetBodyRates ().Transform (vehicle.GetBodyFixed2Ecl ());
        }
    }
}
