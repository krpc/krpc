using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Torque = KRPC.Utils.Tuple<Vector3d, Vector3d>;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Extension methods for torque providers
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectPrefixRule")]
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
                pos += torque.Item1;
                neg += torque.Item2;
            }
            return new Torque (pos, neg);
        }
    }
}
