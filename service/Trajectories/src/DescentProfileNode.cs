using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using System;

namespace KRPC.Trajectories
{
    /// <summary>
    /// A node of the descent profile.
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
        /// The angle in degrees, from -180 to 180.
        /// </summary>
        /// <remarks>
        /// <see cref="Mode"/> sets what the angle is measured from. The default, 180, is
        /// retrograde.
        /// </remarks>
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
        /// Whether <see cref="Angle"/> is the angle of attack or the angle from the horizon.
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
        /// Whether the nodes are the same.
        /// </summary>
        public override bool Equals(DescentProfileNode other)
        {
            return !ReferenceEquals(other, null) && InternalNode == other.InternalNode;
        }

        /// <summary>
        /// Hash the node.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalNode.GetHashCode();
        }
    }
}
