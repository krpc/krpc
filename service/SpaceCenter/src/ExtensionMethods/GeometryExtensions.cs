using System;
using System.Collections.Generic;
using KRPC.Utils;
using UnityEngine;
using Tuple2 = System.Tuple<double, double>;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;
using TupleV3 = System.Tuple<Vector3d, Vector3d>;
using TupleT3 = System.Tuple<System.Tuple<double, double, double>, System.Tuple<double, double, double>>;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Extensions for geometry classes
    /// </summary>
    public static class GeometryExtensions
    {
        /// <summary>
        /// Convert a vector to a tuple
        /// </summary>
        public static Tuple2 ToTuple (this Vector2 v)
        {
            return new Tuple2 (v.x, v.y);
        }

        /// <summary>
        /// Convert a vector to a tuple
        /// </summary>
        public static Tuple3 ToTuple (this Vector3d v)
        {
            return new Tuple3 (v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a vector to a tuple
        /// </summary>
        public static Tuple3 ToTuple (this Vector3 v)
        {
            return new Tuple3 (v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a tuple to a vector
        /// </summary>
        public static Vector2 ToVector (this Tuple2 t)
        {
            if (t == null)
                throw new ArgumentNullException (nameof (t));
            return new Vector2 ((float)t.Item1, (float)t.Item2);
        }

        /// <summary>
        /// Convert a tuple to a vector
        /// </summary>
        public static Vector3d ToVector (this Tuple3 t)
        {
            if (t == null)
                throw new ArgumentNullException (nameof (t));
            return new Vector3d (t.Item1, t.Item2, t.Item3);
        }

        /// <summary>
        /// Convert a quaternion to tuple
        /// </summary>
        public static Tuple4 ToTuple (this Quaternion q)
        {
            return new Tuple4 (q.x, q.y, q.z, q.w);
        }

        /// <summary>
        /// Convert a quaternion to tuple
        /// </summary>
        public static Tuple4 ToTuple (this QuaternionD q)
        {
            return new Tuple4 (q.x, q.y, q.z, q.w);
        }

        /// <summary>
        /// Convert a tuple to a quaternion
        /// </summary>
        public static QuaternionD ToQuaternion (this Tuple4 t)
        {
            if (t == null)
                throw new ArgumentNullException (nameof (t));
            return new QuaternionD (t.Item1, t.Item2, t.Item3, t.Item4);
        }

        /// <summary>
        /// Convert a Matrix4x4 (simulating a Matrix3x3) to a tuple of tuples
        /// </summary>
        public static IList<double> ToList (this Matrix4x4 m)
        {
            return new List<double> (new double[] {
                m [0, 0], m [0, 1], m [0, 2],
                m [1, 0], m [1, 1], m [1, 2],
                m [2, 0], m [2, 1], m [2, 2]
            });
        }

        /// <summary>
        /// Convert a pair of vectors to a pair of tuples
        /// </summary>
        public static TupleT3 ToTuple (this TupleV3 v)
        {
            if (v == null)
                throw new ArgumentNullException (nameof (v));
            return new TupleT3 (v.Item1.ToTuple (), v.Item2.ToTuple ());
        }

        /// <summary>
        /// Swap the Y and Z components of a vector
        /// </summary>
        public static Vector3d SwapYZ (this Vector3d v)
        {
            return new Vector3d (v.x, v.z, v.y);
        }

        /// <summary>
        /// Normalize an angle to the range (-180,180)
        /// </summary>
        public static double NormAngle (double angle)
        {
            return angle - 360d * Math.Floor ((angle + 180d) / 360d);
        }

        /// <summary>
        /// Normalize an angle to the range (-180,180)
        /// </summary>
        public static float NormAngle (float angle)
        {
            return angle - 360f * Mathf.Floor ((angle + 180f) / 360f);
        }

        /// <summary>
        /// Clamp the elements of a vector to the given range
        /// </summary>
        public static Vector3 Clamp (this Vector3 v, float min, float max)
        {
            return new Vector3 (
                Mathf.Clamp (v.x, min, max),
                Mathf.Clamp (v.y, min, max),
                Mathf.Clamp (v.z, min, max));
        }

        /// <summary>
        /// Clamp the magnitude of a vector to the given range
        /// </summary>
        public static Vector3 ClampMagnitude (this Vector3 v, float min, float max)
        {
            if (v.sqrMagnitude < min * min)
                return v.normalized * min;
            if (v.sqrMagnitude > max * max)
                return v.normalized * max;
            return v;
        }

        /// <summary>
        /// Clamp a value to the given range
        /// </summary>
        public static T Clamp<T> (this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo (min) < 0)
                return min;
            if (value.CompareTo (max) > 0)
                return max;
            return value;
        }

        /// <summary>
        /// Clamps the given angle to the range [0,360]
        /// </summary>
        public static double ClampAngle360 (double angle)
        {
            angle = angle % 360d;
            if (angle < 0d)
                angle += 360d;
            return angle;
        }

        /// <summary>
        /// Clamps the given angle to the range [-180,180]
        /// </summary>
        public static double ClampAngle180 (double angle)
        {
            angle = ClampAngle360 (angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        /// <summary>
        /// Convert radians to degrees.
        /// </summary>
        public static float ToDegrees (float radians)
        {
            return radians * (180f / (float)Math.PI);
        }

        /// <summary>
        /// Convert radians to degrees.
        /// </summary>
        public static double ToDegrees (double radians)
        {
            return radians * (180d / Math.PI);
        }

        /// <summary>
        /// Convert degrees to radians.
        /// </summary>
        public static float ToRadians (float degrees)
        {
            return degrees * ((float)Math.PI / 180f);
        }

        /// <summary>
        /// Convert degrees to radians.
        /// </summary>
        public static double ToRadians (double degrees)
        {
            return degrees * (Math.PI / 180d);
        }

        /// <summary>
        /// Axis ordering
        /// </summary>
        [Serializable]
        public enum AxisOrder
        {
            /// <summary>
            /// y-z-x axis order
            /// </summary>
            YZX
        }

        /// <summary>
        /// Extract euler angles from a quarternion using the specified axis order.
        /// </summary>
        public static Vector3d EulerAngles (this QuaternionD q, AxisOrder order)
        {
            // Unity3d euler angle extraction order is ZXY
            Vector3d result;
            switch (order) {
            case AxisOrder.YZX:
                {
                    // FIXME: use double precision arithmetic
                    var angles = new Quaternion ((float)q.z, (float)q.x, (float)q.y, (float)q.w).eulerAngles;
                    result = new Vector3d (angles.z, angles.x, angles.y);
                    break;
                }
            default:
                throw new ArgumentException ("Axis order not supported");
            }
            // Clamp angles to range (0,360)
            result.x = ClampAngle360 (result.x);
            result.y = ClampAngle360 (result.y);
            result.z = ClampAngle360 (result.z);
            return result;
        }

        /// <summary>
        /// Compute the pitch, heading and roll angles of a quaternion in degrees.
        /// </summary>
        public static Vector3d PitchHeadingRoll (this QuaternionD q)
        {
            // Extract angles in order: roll (y), pitch (z), heading (x)
            Vector3d eulerAngles = q.EulerAngles (AxisOrder.YZX);
            var pitch = eulerAngles.y > 180d ? 360d - eulerAngles.y : -eulerAngles.y;
            var heading = eulerAngles.z;
            var roll = eulerAngles.x >= 90d ? 270d - eulerAngles.x : -90d - eulerAngles.x;
            return new Vector3d (pitch, heading, roll);
        }

        /// <summary>
        /// Create a quaternion rotation from the given euler angles, and axis order.
        /// </summary>
        public static QuaternionD QuaternionFromEuler (Vector3d eulerAngles, AxisOrder order)
        {
            QuaternionD result;
            switch (order) {
            case AxisOrder.YZX:
                // FIXME: use double precision arithmetic
                var angles = new Vector3 ((float)eulerAngles.y, (float)eulerAngles.z, (float)eulerAngles.x);
                var tmp = Quaternion.Euler (angles);
                result = new QuaternionD (tmp.y, tmp.z, tmp.x, tmp.w);
                break;
            default:
                throw new ArgumentException ("Axis order not supported");
            }
            return result;
        }

        /// <summary>
        /// Create a quarternion rotation for the given pitch, heading and roll angles.
        /// </summary>
        public static QuaternionD QuaternionFromPitchHeadingRoll (Vector3d phr)
        {
            var pitch = phr.x;
            var heading = phr.y;
            var roll = phr.z;
            var y = pitch < 0d ? -pitch : 360d - pitch;
            var z = heading;
            var x = roll <= -90d ? -roll - 90d : 270d - roll;
            return QuaternionFromEuler (new Vector3d (x, y, z), AxisOrder.YZX);
        }

        /// <summary>
        /// Double-precision rotation from one vector to another.
        /// Uses the half-angle trick: q = (cross, 1 + dot), normalized.
        /// Handles near-antiparallel vectors by picking an arbitrary perpendicular axis.
        /// </summary>
        public static QuaternionD FromToRotation (Vector3d from, Vector3d to)
        {
            from = from.normalized;
            to = to.normalized;
            var dot = Vector3d.Dot (from, to);
            if (dot >= 1.0 - 1e-10)
                return QuaternionD.identity;
            if (dot <= -1.0 + 1e-10) {
                // 180° rotation — pick an arbitrary perpendicular axis
                var perp = Math.Abs (from.x) < 0.9 ? Vector3d.Cross (from, Vector3d.right) : Vector3d.Cross (from, Vector3d.up);
                perp.Normalize ();
                return new QuaternionD (perp.x, perp.y, perp.z, 0);
            }
            var axis = Vector3d.Cross (from, to);
            return new QuaternionD (axis.x, axis.y, axis.z, 1.0 + dot).Normalize ();
        }

        /// <summary>
        /// Double-precision rotation of <paramref name="angle"/> degrees about <paramref name="axis"/>
        /// (right-handed). Mirrors Unity's <c>Quaternion.AngleAxis</c> (angle first).
        /// </summary>
        public static QuaternionD AngleAxis (double angle, Vector3d axis)
        {
            axis = axis.normalized;
            var half = ToRadians (angle) * 0.5;
            var s = Math.Sin (half);
            return new QuaternionD (axis.x * s, axis.y * s, axis.z * s, Math.Cos (half));
        }

        /// <summary>
        /// The signed angle, in degrees, to rotate <paramref name="from"/> onto <paramref name="to"/>
        /// about <paramref name="axis"/> (right-handed: positive when <c>from × to</c> points along
        /// <paramref name="axis"/>). Both vectors are projected onto the plane perpendicular to the
        /// axis first; returns 0 if either projection vanishes.
        /// </summary>
        public static double SignedAngle (Vector3d from, Vector3d to, Vector3d axis)
        {
            axis = axis.normalized;
            var f = from - Vector3d.Dot (from, axis) * axis;
            var t = to - Vector3d.Dot (to, axis) * axis;
            if (f.magnitude < 1e-10 || t.magnitude < 1e-10)
                return 0.0;
            f.Normalize ();
            t.Normalize ();
            var unsigned = ToDegrees (Math.Acos (Vector3d.Dot (f, t).Clamp (-1.0, 1.0)));
            return Vector3d.Dot (axis, Vector3d.Cross (f, t)) < 0 ? -unsigned : unsigned;
        }

        /// <summary>
        /// Double-precision quaternion to angle-axis decomposition.
        /// Angle is returned in degrees (matching Unity convention).
        /// </summary>
        public static void ToAngleAxis (this QuaternionD q, out double angle, out Vector3d axis)
        {
            q = q.Normalize ();
            var w = q.w.Clamp (-1.0, 1.0);
            var sinHalfAngle = Math.Sqrt (1.0 - w * w);
            angle = ToDegrees (2.0 * Math.Acos (w));
            if (sinHalfAngle < 1e-10) {
                axis = Vector3d.up;
                angle = 0;
            } else {
                axis = new Vector3d (q.x / sinHalfAngle, q.y / sinHalfAngle, q.z / sinHalfAngle);
            }
        }

        /// <summary>
        /// Canonicalize a rotation quaternion's sign so that <c>q</c> and <c>-q</c> — which are the
        /// same rotation — decompose identically. <see cref="ToAngleAxis"/> maps them to opposite
        /// axes at exactly 180 degrees (where w is zero), which flips the sign of the decomposed
        /// axis*angle vector. Choose w &gt;= 0, and at w == 0 make the largest-magnitude component
        /// positive, so the axis is deterministic and does not flip under noise in a minor component.
        /// For any rotation away from 180 degrees this leaves the resulting axis*angle vector
        /// unchanged (ToAngleAxis followed by <see cref="ClampAngle180"/> already canonicalizes those);
        /// it only disambiguates the 180-degree singular point.
        /// </summary>
        public static QuaternionD CanonicalSign (this QuaternionD q)
        {
            if (q.w < 0 || (q.w == 0 && LargestComponentIsNegative (q.x, q.y, q.z)))
                return new QuaternionD (-q.x, -q.y, -q.z, -q.w);
            return q;
        }

        static bool LargestComponentIsNegative (double x, double y, double z)
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

        /// <summary>
        /// Spherical linear interpolation between two unit quaternions, along the shortest geodesic
        /// (constant angular velocity). t is clamped to [0,1]. Reimplemented here (rather than using
        /// the stubbed KSP QuaternionD.Slerp) using the half-angle of the delta rotation, so it works
        /// against the stripped stub DLLs as well as the real in-game QuaternionD.
        /// </summary>
        public static QuaternionD Slerp (QuaternionD from, QuaternionD to, double t)
        {
            if (t <= 0)
                return from;
            if (t >= 1)
                return to;
            var delta = to * from.Inverse ();
            double angle;
            Vector3d axis;
            ToAngleAxis (delta, out angle, out axis);
            angle = ClampAngle180 (angle);
            // ToAngleAxis collapses a near-zero rotation to angle=0 (unit axis), so no need to guard
            // against an infinite/degenerate axis here.
            if (Math.Abs (angle) < 1e-9)
                return to;
            var half = ToRadians (angle * t) * 0.5;
            var s = Math.Sin (half);
            var partial = new QuaternionD (axis.x * s, axis.y * s, axis.z * s, Math.Cos (half)).Normalize ();
            return (partial * from).Normalize ();
        }

        /// <summary>
        /// The angle, in degrees, of the shortest rotation between two unit quaternions.
        /// </summary>
        public static double Angle (QuaternionD a, QuaternionD b)
        {
            var delta = b * a.Inverse ();
            double angle;
            Vector3d axis;
            ToAngleAxis (delta, out angle, out axis);
            return Math.Abs (ClampAngle180 (angle));
        }

        /// <summary>
        /// Rotate from one orientation toward another by at most maxDegrees (along the shortest
        /// geodesic). Reaches and clamps at <paramref name="to"/> once within maxDegrees.
        /// </summary>
        public static QuaternionD RotateTowards (QuaternionD from, QuaternionD to, double maxDegrees)
        {
            var angle = Angle (from, to);
            if (angle <= maxDegrees || angle < 1e-9)
                return to;
            return Slerp (from, to, maxDegrees / angle);
        }

        /// <summary>
        /// Normalize a quaternion to unit length.
        /// </summary>
        public static QuaternionD Normalize (this QuaternionD q)
        {
            var mag = Math.Sqrt (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new QuaternionD (q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }

        /// <summary>
        /// Compute the inverse quaternion. Assumes the input is a unit quaternion.
        /// </summary>
        public static QuaternionD Inverse (this QuaternionD q)
        {
            return new QuaternionD (-q.x, -q.y, -q.z, q.w);
        }

        /// <summary>
        /// Implementation of QuaternionD.OrthoNormalize, using stabilized Gram-Schmidt
        /// </summary>
        public static void OrthoNormalize2 (ref Vector3d normal, ref Vector3d tangent)
        {
            normal.Normalize ();
            tangent.Normalize (); // Additional normalization, avoids large tangent norm
            var proj = normal * Vector3d.Dot (tangent, normal);
            tangent = tangent - proj;
            tangent.Normalize ();
        }

        /// <summary>
        /// Implementation of QuaternionD.LookRotation
        /// </summary>
        public static QuaternionD LookRotation2 (Vector3d forward, Vector3d up)
        {
            OrthoNormalize2 (ref forward, ref up);
            Vector3d right = Vector3d.Cross (up, forward);
            // Shepperd's method: pick the component with the largest magnitude first
            // so we never take sqrt of a negative or divide by near-zero.
            // The four squared-magnitude candidates sum to 4, so at least one is >= 1.
            double m00 = right.x, m11 = up.y, m22 = forward.z;
            double tw = 1.0d + m00 + m11 + m22;
            double tx = 1.0d + m00 - m11 - m22;
            double ty = 1.0d - m00 + m11 - m22;
            double tz = 1.0d - m00 - m11 + m22;
            double x, y, z, w;
            if (tw >= tx && tw >= ty && tw >= tz) {
                w = Math.Sqrt (tw) * 0.5d;
                var r = 0.25d / w;
                x = (up.z - forward.y) * r;
                y = (forward.x - right.z) * r;
                z = (right.y - up.x) * r;
            } else if (tx >= ty && tx >= tz) {
                x = Math.Sqrt (tx) * 0.5d;
                var r = 0.25d / x;
                y = (right.y + up.x) * r;
                z = (right.z + forward.x) * r;
                w = (up.z - forward.y) * r;
            } else if (ty >= tz) {
                y = Math.Sqrt (ty) * 0.5d;
                var r = 0.25d / y;
                x = (right.y + up.x) * r;
                z = (up.z + forward.y) * r;
                w = (forward.x - right.z) * r;
            } else {
                z = Math.Sqrt (tz) * 0.5d;
                var r = 0.25d / z;
                x = (right.z + forward.x) * r;
                y = (up.z + forward.y) * r;
                w = (right.y - up.x) * r;
            }
            return new QuaternionD (x, y, z, w);
        }

        /// <summary>
        /// Project a vector onto a plane, defined by its normal
        /// </summary>
        public static Vector3d ProjectVectorOntoPlane (Vector3d normal, Vector3d v)
        {
            return v - normal * Vector3d.Dot (normal, v);
        }

        /// <summary>
        /// Add a 4x4 Matrix into another one (does not allocate)
        /// </summary>
        public static Matrix4x4 Add (this Matrix4x4 left, Matrix4x4 right)
        {
            Matrix4x4 m = Matrix4x4.zero;
            for (int i = 0; i < 4; i++)
                m.SetColumn (i, left.GetColumn (i) + right.GetColumn (i));
            return m;
        }

        /// <summary>
        /// Multiply a 4x4 Matrix by a scalar (simulating 3x3 matrix) (does not allocate)
        /// </summary>
        public static Matrix4x4 MultiplyScalar (this Matrix4x4 left, float right)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    left [i, j] = left [i, j] * right;
            return left;
        }

        /// <summary>
        /// Returns the diagonal 3-vector from the 4x4 matrix (simulating 3x3 matrix)
        /// </summary>
        public static Vector3d Diagonal (this Matrix4x4 m)
        {
            return new Vector3d (m [0, 0], m [1, 1], m [2, 2]);
        }

        /// <summary>
        /// Constructs diagonal matrix from a float (Identity * val)
        /// </summary>
        public static Matrix4x4 ToDiagonalMatrix (this float v)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (int i = 0; i < 4; i++)
                m [i, i] = v;
            return m;
        }

        /// <summary>
        /// Constructs diagonal matrix from a 3-vector (simulating 3x3 matrix)
        /// </summary>
        public static Matrix4x4 ToDiagonalMatrix (this Vector3 v)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (int i = 0; i < 3; i++)
                m [i, i] = v [i];
            return m;
        }

        /// <summary>
        /// Construct the outer product of two 3-vectors as a 4x4 matrix
        /// </summary>
        public static Matrix4x4 OuterProduct (this Vector3 left, Vector3 right)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    m [i, j] = left [i] * right [j];
            return m;
        }

        /// <summary>
        /// Compute the vertices for an axis-aligned bounding box.
        /// </summary>
        public static Vector3[] ToVertices (this Bounds box)
        {
            return new [] {
                box.max,
                box.min,
                box.center + new Vector3 (-box.extents.x, box.extents.y, box.extents.z),
                box.center + new Vector3 (box.extents.x, -box.extents.y, box.extents.z),
                box.center + new Vector3 (box.extents.x, box.extents.y, -box.extents.z),
                box.center + new Vector3 (-box.extents.x, -box.extents.y, box.extents.z),
                box.center + new Vector3 (-box.extents.x, box.extents.y, -box.extents.z),
                box.center + new Vector3 (box.extents.x, -box.extents.y, -box.extents.z)
            };
        }

        /// <summary>
        /// Convert an axis-aligned bounding box to its min and max positions as tuples.
        /// </summary>
        public static TupleT3 ToTuples (this Bounds bounds)
        {
            return new TupleT3 (bounds.min.ToTuple (), bounds.max.ToTuple ());
        }
    }
}
