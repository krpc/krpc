using System;
using System.Runtime.CompilerServices;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a maneuver node. Can be created using <see cref="Control.AddNode"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Node : Equatable<Node>, IGameObjectState
    {
        /// Note: Maneuver node delta-v vectors use a special coordinate system.
        /// The z-component is the prograde component.
        /// The y-component is the normal component.
        /// The x-component is the radial component.

        readonly Guid vesselId;
        // The game's own maneuver node. The game offers no way to identify a node, so the
        // node is held. A node the game does not have in the vessel's flight plan, which
        // includes every node after a game is loaded, is reclaimed
        readonly ManeuverNode node;

        internal Node (global::Vessel vessel, double ut, double prograde, double normal, double radial)
        {
            vesselId = vessel.id;
            if (InternalVessel.patchedConicSolver == null)
                throw new InvalidOperationException ("Cannot add maneuver node");
            node = vessel.patchedConicSolver.AddManeuverNode (ut);
            node.DeltaV = new Vector3d (radial, normal, prograde);
            Update ();
        }

        /// <summary>
        /// Construct a node from a KSP node and the id of the vessel whose flight plan it
        /// belongs to, for a caller that has the id and must not resolve it.
        /// </summary>
        internal Node (Guid vessel, ManeuverNode maneuverNode)
        {
            vesselId = vessel;
            node = maneuverNode;
        }

        /// <summary>
        /// Construct a node from a KSP node.
        /// </summary>
        public Node (global::Vessel vessel, ManeuverNode maneuverNode)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            if (ReferenceEquals (maneuverNode, null))
                throw new ArgumentNullException (nameof (maneuverNode));
            vesselId = vessel.id;
            node = maneuverNode;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Node other)
        {
            return !ReferenceEquals (other, null) && vesselId == other.vesselId &&
            ReferenceEquals (node, other.node);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // The node's identity hash: the game holds the node itself, and nothing about
            // it may change while a client has this object
            return Hash.Of (vesselId).And (RuntimeHelpers.GetHashCode (node));
        }

        /// <summary>
        /// The KSP vessel.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (vesselId); }
        }

        /// <summary>
        /// The KSP maneuver node, checked to still be part of the vessel's flight plan.
        /// A node can be removed through this object, through another object standing for
        /// the same node (including Control.RemoveNodes), or by the player in the in-game
        /// UI, and loading a game replaces every node the vessel had; in all cases the node
        /// this stands for is gone for good. A vessel the game is not solving conics for has
        /// no flight plan to look in, which says nothing about the node either way.
        /// </summary>
        public ManeuverNode InternalNode {
            get {
                var solver = InternalVessel.patchedConicSolver;
                if (solver != null && solver.maneuverNodes.Contains (node))
                    return node;
                throw NotResolvable ();
            }
        }

        /// <summary>
        /// The error to raise when the node cannot be found in the vessel's flight plan,
        /// which <see cref="GameObjectState" /> decides between so that one rule says what
        /// the absence of the node from the plan means.
        /// </summary>
        Exception NotResolvable ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException (
                    "The maneuver node no longer exists, as it is not in the vessel's flight plan.");
            return new InvalidOperationException (
                "The maneuver node is not loaded, as the game is not computing a flight " +
                "plan for the vessel it belongs to. It can be used again once the game does.");
        }

        /// <summary>
        /// The state of the node. It is live while the vessel holds it in its flight plan,
        /// and destroyed once a live vessel stops holding it. A vessel the game solves no
        /// conics for has no flight plan, and leaves the node dormant.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var vesselState = FlightGlobalsExtensions.VesselState (vesselId);
                if (vesselState != GameObjectState.Live)
                    return vesselState;
                var solver = FlightGlobalsExtensions.FindVesselById (vesselId).patchedConicSolver;
                if (solver == null)
                    return GameObjectState.Dormant;
                return solver.maneuverNodes.Contains (node)
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        internal Vector3d WorldBurnVector {
            get {
                var prograde = InternalNode.patch.getOrbitalVelocityAtUT (InternalNode.UT).SwapYZ ().normalized;
                var normal = InternalNode.patch.GetOrbitNormal ().SwapYZ ().normalized;
                var radial = Vector3d.Cross (normal, prograde);
                return Prograde * prograde + Normal * normal + Radial * radial;
            }
        }

        /// <summary>
        /// The magnitude of the maneuver nodes delta-v in the prograde direction,
        /// in meters per second.
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
        /// The magnitude of the maneuver nodes delta-v in the normal direction,
        /// in meters per second.
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
        /// The magnitude of the maneuver nodes delta-v in the radial direction,
        /// in meters per second.
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
        /// Gets the remaining delta-v of the maneuver node, in meters per second. Changes as the
        /// node is executed. This is equivalent to the delta-v reported in-game.
        /// </summary>
        [KRPCProperty]
        public double RemainingDeltaV {
            get { return InternalNode.GetBurnVector (InternalNode.patch).magnitude; }
        }

        /// <summary>
        /// Returns the burn vector for the maneuver node.
        /// </summary>
        /// <param name="referenceFrame">The reference frame that the returned vector is in.
        /// Defaults to <see cref="Vessel.OrbitalReferenceFrame"/>.</param>
        /// <returns>A vector whose direction is the direction of the maneuver node burn, and
        /// magnitude is the delta-v of the burn in meters per second.
        /// </returns>
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
        /// Returns the remaining burn vector for the maneuver node.
        /// </summary>
        /// <param name="referenceFrame">The reference frame that the returned vector is in.
        /// Defaults to <see cref="Vessel.OrbitalReferenceFrame"/>.</param>
        /// <returns>A vector whose direction is the direction of the maneuver node burn, and
        /// magnitude is the delta-v of the burn in meters per second.
        /// </returns>
        /// <remarks>
        /// Changes as the maneuver node is executed. See <see cref="BurnVector"/>.
        /// </remarks>
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
            get { return new Orbit (this); }
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
            InternalNode.RemoveSelf ();
        }

        /// <summary>
        /// The reference frame that is fixed relative to the maneuver node's burn.
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
        /// The reference frame that is fixed relative to the maneuver node, and
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
        /// The position vector of the maneuver node in the given reference frame.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalNode.patch.getPositionAtUT (InternalNode.UT)).ToTuple ();
        }

        /// <summary>
        /// The rotation of the maneuver nodes burn.
        /// </summary>
        /// <returns>The rotation as a quaternion of the form <math>(x, y, z, w)</math>.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// rotation is in.</param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationFromWorldSpace (this.ReferenceFrame.Rotation).ToTuple ();
        }

        /// <summary>
        /// The direction of the maneuver nodes burn.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector.normalized).ToTuple ();
        }
    }
}
