using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using System;

namespace KRPC.Trajectories
{
    /// <summary>
    /// Trajectories descent profile node.
    /// </summary>
    [KRPCClass(Service = "Trajectories")]
    public class DescentProfileNode : Equatable<DescentProfileNode>
    {
        private readonly API.DescentProfile.Node InternalNode;

        internal DescentProfileNode(API.DescentProfile.Node original)
        {
            InternalNode = original;
        }

        /// <summary>
        /// Angle in degrees from prograde, <see cref="KRPC.Trajectories.DescentProfileMode"/> determines if this is angle of attack or angle from horizon.
        /// 
        /// Default is 180 degrees aka retrograde.
        /// </summary>
        [KRPCProperty]
        public double Angle 
        {
            get
            {
                // The mod converts from radians with a float factor, so 180 degrees can
                // read just past 180
                double angle = Math.Max(-180d, Math.Min(180d, InternalNode.AngleDeg));
                return angle <= -180d ? 180d : angle;
            }
            set
            {
                double clampDeg = GeometryExtensions.ClampAngle180(value);

                // Anything beyond +/- 90 degrees (Trajectories GUI_MAX_ANGLE) becomes retrograde.
                InternalNode.Retrograde = (clampDeg > 90d);
                InternalNode.AngleDeg = clampDeg;
            }
        }

        /// <summary>
        /// Use angle of attack or angle from horizon for <see cref="Angle"/>.
        /// </summary>
        [KRPCProperty]
        public DescentProfileMode Mode 
        {
            get
            {
                return InternalNode.Horizon ? DescentProfileMode.Horizon: DescentProfileMode.AngleOfAttack;
            }
            set
            {
                InternalNode.Horizon = value == DescentProfileMode.Horizon;
            }
        }

        /// <summary>
        /// Check that nodes are the same.
        /// </summary>
        public override bool Equals(DescentProfileNode other)
        {
            return !ReferenceEquals(other, null) && InternalNode == other.InternalNode;
        }

        /// <summary>
        /// Hash the Node.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalNode.GetHashCode();
        }
    }
}
