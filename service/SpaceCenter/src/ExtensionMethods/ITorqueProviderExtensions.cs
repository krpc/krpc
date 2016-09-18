using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Extension methods for torque providers
    /// </summary>
    public static class ITorqueProviderExtensions
    {
        /// <summary>
        /// Get the largest potential torque provided by the part.
        /// </summary>
        public static Vector3 GetPotentialTorque (this ITorqueProvider torqueProvider)
        {
            Vector3 pos;
            Vector3 neg;
            torqueProvider.GetPotentialTorque (out pos, out neg);
            return pos.sqrMagnitude >= neg.sqrMagnitude ? pos : neg;
        }
    }
}
