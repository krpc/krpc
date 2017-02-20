using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a maneuver node. Can be created using <see cref="Control.AddNode"/>.
    /// </summary>
    // FIXME: need to perform memory management for node objects
    [KRPCClass (Service = "SpaceCenter")]
    public class Node : Equatable<Node>
    {
        /// Note: Maneuver node delta-v vectors use a special coordinate system.
        /// The z-component is the prograde component.
        /// The y-component is the normal component.
        /// The x-component is the radial component.

        readonly Guid vesselId;

        internal Node (global::Vessel vessel, double ut, double prograde, double normal, double radial)
        {
            vesselId = vessel.id;
            if (InternalVessel.patchedConicSolver == null)
                throw new InvalidOperationException ("Cannot add maneuver node");
            var node = vessel.patchedConicSolver.AddManeuverNode (ut);
            node.DeltaV = new Vector3d (radial, normal, prograde);
            Update ();
            InternalNode = node;
        }

        /// <summary>
        /// Construct a node from a KSP node.
        /// </summary>
        public Node (global::Vessel vessel, ManeuverNode node)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            if (ReferenceEquals (node, null))
                throw new ArgumentNullException (nameof (node));
            vesselId = vessel.id;
            InternalNode = node;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Node other)
        {
            return !ReferenceEquals (other, null) && vesselId == other.vesselId && InternalNode == other.InternalNode;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = vesselId.GetHashCode ();
            // Note: InternalNode could be set to null by Remove
            if (InternalNode != null)
                hash ^= InternalNode.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The KSP vessel.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (vesselId); }
        }

        /// <summary>
        /// The KSP node.
        /// </summary>
        public ManeuverNode InternalNode { get; private set; }

        internal Vector3d WorldBurnVector {
            get {
                var prograde = InternalNode.patch.getOrbitalVelocityAtUT (InternalNode.UT).SwapYZ ().normalized;
                var normal = InternalNode.patch.GetOrbitNormal ().SwapYZ ().normalized;
                var radial = Vector3d.Cross (normal, prograde);
                return Prograde * prograde + Normal * normal + Radial * radial;
            }
        }

        /// <summary>
        /// The magnitude of the maneuver nodes delta-v in the prograde direction, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Prograde {
            get { return InternalNode.DeltaV.z; }
            set {
                InternalNode.DeltaV.z = value;
                Update ();
            }
        }

        /// <summary>
        /// The magnitude of the maneuver nodes delta-v in the normal direction, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Normal {
            get { return InternalNode.DeltaV.y; }
            set {
                InternalNode.DeltaV.y = value;
                Update ();
            }
        }

        /// <summary>
        /// The magnitude of the maneuver nodes delta-v in the radial direction, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Radial {
            get { return InternalNode.DeltaV.x; }
            set {
                InternalNode.DeltaV.x = value;
                Update ();
            }
        }

        /// <summary>
        /// The delta-v of the maneuver node, in meters per second.
        /// </summary>
        /// <remarks>
        /// Does not change when executing the maneuver node. See <see cref="RemainingDeltaV"/>.
        /// </remarks>
        [KRPCProperty]
        public double DeltaV {
            get { return InternalNode.DeltaV.magnitude; }
            set {
                var direction = InternalNode.DeltaV.normalized;
                InternalNode.DeltaV = new Vector3d (direction.x * value, direction.y * value, direction.z * value);
                Update ();
            }
        }

        /// <summary>
        /// Gets the remaining delta-v of the maneuver node, in meters per second. Changes as the node
        /// is executed. This is equivalent to the delta-v reported in-game.
        /// </summary>
        [KRPCProperty]
        public double RemainingDeltaV {
            get { return InternalNode.GetBurnVector (InternalNode.patch).magnitude; }
        }

        /// <summary>
        /// Returns a vector whose direction the direction of the maneuver node burn, and whose magnitude
        /// is the delta-v of the burn in m/s.
        /// </summary>
        /// <param name="referenceFrame"></param>
        /// <remarks>
        /// Does not change when executing the maneuver node. See <see cref="RemainingBurnVector"/>.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 BurnVector (ReferenceFrame referenceFrame = null)
        {
            if (ReferenceEquals (referenceFrame, null))
                referenceFrame = ReferenceFrame.Orbital (InternalVessel);
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector).ToTuple ();
        }

        /// <summary>
        /// Returns a vector whose direction the direction of the maneuver node burn, and whose magnitude
        /// is the delta-v of the burn in m/s. The direction and magnitude change as the burn is executed.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 RemainingBurnVector (ReferenceFrame referenceFrame = null)
        {
            if (ReferenceEquals (referenceFrame, null))
                referenceFrame = ReferenceFrame.Orbital (InternalVessel);
            return referenceFrame.DirectionFromWorldSpace (InternalNode.GetBurnVector (InternalNode.patch)).ToTuple ();
        }

        /// <summary>
        /// The universal time at which the maneuver will occur, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UT {
            get { return InternalNode.UT; }
            set {
                InternalNode.UT = value;
                Update ();
            }
        }

        /// <summary>
        /// The time until the maneuver node will be encountered, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeTo {
            get { return UT - SpaceCenter.UT; }
        }

        /// <summary>
        /// The orbit that results from executing the maneuver node.
        /// </summary>
        [KRPCProperty]
        public Orbit Orbit {
            get { return new Orbit (InternalNode.nextPatch); }
        }

        void Update () {
            var vessel = InternalVessel;
            if (vessel.patchedConicSolver == null)
                throw new InvalidOperationException ("Cannot update maneuver node");
            vessel.patchedConicSolver.UpdateFlightPlan ();
        }

        /// <summary>
        /// Removes the maneuver node.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            if (InternalNode == null)
                throw new InvalidOperationException ("Node does not exist");
            if (InternalVessel.patchedConicSolver == null)
                throw new InvalidOperationException ("Cannot remove maneuver node");
            InternalNode.RemoveSelf ();
            InternalNode = null;
            // TODO: delete this Node object
        }

        /// <summary>
        /// Gets the reference frame that is fixed relative to the maneuver node's burn.
        /// <list type="bullet">
        /// <item><description>The origin is at the position of the maneuver node.</description></item>
        /// <item><description>The y-axis points in the direction of the burn.</description></item>
        /// <item><description>The x-axis and z-axis point in arbitrary but fixed directions.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalVessel, InternalNode); }
        }

        /// <summary>
        /// Gets the reference frame that is fixed relative to the maneuver node, and
        /// orientated with the orbital prograde/normal/radial directions of the
        /// original orbit at the maneuver node's position.
        /// <list type="bullet">
        /// <item><description>The origin is at the position of the maneuver node.</description></item>
        /// <item><description>The x-axis points in the orbital anti-radial direction of the original
        /// orbit, at the position of the maneuver node.</description></item>
        /// <item><description>The y-axis points in the orbital prograde direction of the original
        /// orbit, at the position of the maneuver node.</description></item>
        /// <item><description>The z-axis points in the orbital normal direction of the original orbit,
        /// at the position of the maneuver node.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalVessel, InternalNode); }
        }

        /// <summary>
        /// Returns the position vector of the maneuver node in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (InternalNode.patch.getPositionAtUT (InternalNode.UT)).ToTuple ();
        }

        /// <summary>
        /// Returns the unit direction vector of the maneuver nodes burn in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector.normalized).ToTuple ();
        }
    }
}
