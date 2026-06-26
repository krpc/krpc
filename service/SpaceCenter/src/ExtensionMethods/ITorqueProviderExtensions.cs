using System;
using System.Collections.Generic;
using UnityEngine;
using Torque = System.Tuple<Vector3d, Vector3d>;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Extension methods for torque providers
    /// </summary>
    static class ITorqueProviderExtensions
    {
        /// <summary>
        /// Get the largest potential torque provided by the part, in Newton meters in the pitch, yaw and roll axes.
        /// Returns a pair of torques: the positive torque around each axis, and the negative torque.
        /// </summary>
        public static Torque GetPotentialTorque (this ITorqueProvider torqueProvider)
        {
            Vector3 pos;
            Vector3 neg;
            torqueProvider.GetPotentialTorque (out pos, out neg);
            Vector3d posd = pos;
            Vector3d negd = neg;
            return new Torque (posd * 1000.0d, negd * 1000.0d);
        }

        /// <summary>
        /// Zero pos/neg torque values.
        /// </summary>
        public static readonly Torque zero = new Torque (Vector3d.zero, Vector3d.zero);

        /// <summary>
        /// Add the given pos/neg torque vectors
        /// </summary>
        public static Torque Sum (IEnumerable<Torque> torques)
        {
            var pos = Vector3d.zero;
            var neg = Vector3d.zero;
            foreach (var torque in torques) {
                // Use abs() to normalise sign conventions: KSP's ITorqueProvider implementations
                // are inconsistent (e.g. ModuleControlSurface negates the roll axis, while
                // ModuleReactionWheel returns the same positive value in both pos and neg).
                // This matches how KSP's own VesselSAS.GetTotalVesselTorque uses Max(pos,neg).
                pos.x += Math.Abs (torque.Item1.x);
                pos.y += Math.Abs (torque.Item1.y);
                pos.z += Math.Abs (torque.Item1.z);
                neg.x -= Math.Abs (torque.Item2.x);
                neg.y -= Math.Abs (torque.Item2.y);
                neg.z -= Math.Abs (torque.Item2.z);
            }
            return new Torque (pos, neg);
        }
    }
}
