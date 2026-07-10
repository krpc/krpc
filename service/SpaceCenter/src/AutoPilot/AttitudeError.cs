using System;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// A rotation vector in degrees.
    /// </summary>
    struct RotationVector
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public RotationVector (double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// Pure mathematical helpers for converting attitude errors into controller inputs.
    /// </summary>
    static class AttitudeError
    {
        /// <summary>
        /// Convert a quaternion to the shortest rotation vector, in degrees.
        /// Equivalent quaternion signs produce the same result, including at exactly 180 degrees.
        /// </summary>
        public static RotationVector ToRotationVector (double x, double y, double z, double w)
        {
            if (!IsFinite (x) || !IsFinite (y) || !IsFinite (z) || !IsFinite (w))
                throw new ArgumentException ("Quaternion components must be finite");

            // Scale first so normalization cannot overflow or underflow for finite non-zero input.
            var maxComponent = Math.Max (Math.Max (Math.Abs (x), Math.Abs (y)),
                                         Math.Max (Math.Abs (z), Math.Abs (w)));
            if (maxComponent == 0)
                throw new ArgumentException ("Quaternion must have non-zero magnitude");
            x /= maxComponent;
            y /= maxComponent;
            z /= maxComponent;
            w /= maxComponent;
            var magnitude = Math.Sqrt (x * x + y * y + z * z + w * w);
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
            w /= magnitude;

            // q and -q represent the same rotation. Choose w >= 0 for the shortest branch.
            // At exactly 180 degrees w is zero, so make the largest axis component positive.
            // This makes the ambiguous axis deterministic and sign-invariant without letting
            // noise in a minor component flip the dominant control direction.
            if (w < 0 || (w == 0 && AxisIsNegative (x, y, z))) {
                x = -x;
                y = -y;
                z = -z;
                w = -w;
            }

            var vectorScale = Math.Max (Math.Max (Math.Abs (x), Math.Abs (y)), Math.Abs (z));
            if (vectorScale == 0)
                return new RotationVector (0, 0, 0);
            var scaledX = x / vectorScale;
            var scaledY = y / vectorScale;
            var scaledZ = z / vectorScale;
            var vectorMagnitude = vectorScale * Math.Sqrt (
                scaledX * scaledX + scaledY * scaledY + scaledZ * scaledZ);

            var angle = 2.0 * Math.Atan2 (vectorMagnitude, w) * (180.0 / Math.PI);
            var scale = angle / vectorMagnitude;
            return new RotationVector (x * scale, y * scale, z * scale);
        }

        static bool AxisIsNegative (double x, double y, double z)
        {
            var absX = Math.Abs (x);
            var absY = Math.Abs (y);
            var absZ = Math.Abs (z);
            if (absX >= absY && absX >= absZ)
                return x < 0;
            if (absY >= absZ)
                return y < 0;
            return z < 0;
        }

        static bool IsFinite (double value)
        {
            return !double.IsNaN (value) && !double.IsInfinity (value);
        }
    }
}
