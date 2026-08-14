using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services.Parts;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a reference frame for positions, rotations and
    /// velocities. Contains:
    /// <list type="bullet">
    /// <item><description>The position of the origin.</description></item>
    /// <item><description>The directions of the x, y and z axes.</description></item>
    /// <item><description>The linear velocity of the frame.</description></item>
    /// <item><description>The angular velocity of the frame.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// This class does not contain any properties or methods. It is only
    /// used as a parameter to other functions.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class ReferenceFrame : Equatable<ReferenceFrame>
    {
        readonly ReferenceFrameType type;
        readonly global::CelestialBody body;
        readonly Guid vesselId;
        readonly ManeuverNode node;
        readonly uint partId;
        readonly ModuleDockingNode dockingPort;
        readonly Thruster thruster;
        readonly Orbit orbit;
        readonly ReferenceFrame parent;
        readonly Vector3d relativePosition;
        readonly QuaternionD relativeRotation;
        readonly Vector3d relativeVelocity;
        readonly Vector3d relativeAngularVelocity;
        readonly ReferenceFrame hybridPosition;
        readonly ReferenceFrame hybridRotation;
        readonly ReferenceFrame hybridVelocity;
        readonly ReferenceFrame hybridAngularVelocity;

        ReferenceFrame (
            ReferenceFrameType type, global::CelestialBody body = null, global::Vessel vessel = null,
            ManeuverNode node = null, Part part = null, ModuleDockingNode dockingPort = null,
            Thruster thruster = null, Orbit orbit = null, ReferenceFrame parent = null,
            ReferenceFrame hybridPosition = null, ReferenceFrame hybridRotation = null,
            ReferenceFrame hybridVelocity = null, ReferenceFrame hybridAngularVelocity = null)
        {
            this.type = type;
            this.body = body;
            vesselId = vessel != null ? vessel.id : Guid.Empty;
            this.node = node;
            if (part != null)
                partId = part.flightID;
            this.dockingPort = dockingPort;
            this.thruster = thruster;
            this.orbit = orbit;
            this.parent = parent;
            this.hybridPosition = hybridPosition;
            this.hybridRotation = hybridRotation;
            this.hybridVelocity = hybridVelocity;
            this.hybridAngularVelocity = hybridAngularVelocity;
        }

        ReferenceFrame (ReferenceFrame parent, Vector3d relativePosition, QuaternionD relativeRotation, Vector3d relativeVelocity, Vector3d relativeAngularVelocity)
        {
            type = ReferenceFrameType.Relative;
            this.parent = parent;
            this.relativePosition = relativePosition;
            this.relativeRotation = relativeRotation;
            this.relativeVelocity = relativeVelocity;
            this.relativeAngularVelocity = relativeAngularVelocity;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ReferenceFrame other)
        {
            return
            !ReferenceEquals (other, null) &&
            type == other.type &&
            body == other.body &&
            vesselId == other.vesselId &&
            node == other.node &&
            partId == other.partId &&
            dockingPort == other.dockingPort &&
            thruster == other.thruster &&
            orbit == other.orbit &&
            parent == other.parent &&
            (type != ReferenceFrameType.Relative ||
            (relativePosition == other.relativePosition &&
            relativeRotation == other.relativeRotation &&
            relativeVelocity == other.relativeVelocity &&
            relativeAngularVelocity == other.relativeAngularVelocity)) &&
            (type != ReferenceFrameType.Hybrid ||
            (hybridPosition == other.hybridPosition &&
            hybridRotation == other.hybridRotation &&
            hybridVelocity == other.hybridVelocity &&
            hybridAngularVelocity == other.hybridAngularVelocity));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = type.GetHashCode ();
            if (body != null)
                hash ^= body.name.GetHashCode ();
            hash ^= vesselId.GetHashCode ();
            if (node != null)
                hash ^= node.GetHashCode ();
            hash ^= partId.GetHashCode ();
            if (dockingPort != null)
                hash ^= dockingPort.GetHashCode ();
            if (thruster != null)
                hash ^= thruster.GetHashCode ();
            if (orbit != null)
                hash ^= orbit.GetHashCode ();
            if (parent != null)
                hash ^= parent.GetHashCode ();
            if (type == ReferenceFrameType.Relative) {
                hash ^= relativePosition.GetHashCode ();
                hash ^= relativeRotation.GetHashCode ();
                hash ^= relativeVelocity.GetHashCode ();
                hash ^= relativeAngularVelocity.GetHashCode ();
            }
            if (type == ReferenceFrameType.Hybrid) {
                hash ^= hybridPosition.GetHashCode ();
                hash ^= hybridRotation.GetHashCode ();
                hash ^= hybridVelocity.GetHashCode ();
                hash ^= hybridAngularVelocity.GetHashCode ();
            }
            return hash;
        }

        /// <summary>
        /// The type of the reference frame.
        /// </summary>
        public ReferenceFrameType Type {
            get { return type; }
        }

        /// <summary>
        /// The celestial body.
        /// </summary>
        public CelestialBody Body {
            get {
                if (body == null)
                    throw new InvalidOperationException ("Reference frame has no celestial body");
                return new CelestialBody (body);
            }
        }

        /// <summary>
        /// The vessel.
        /// </summary>
        public Vessel Vessel {
            get { return new Vessel (InternalVessel); }
        }

        /// <summary>
        /// The node.
        /// </summary>
        public Node Node {
            get {
                if (node == null)
                    throw new InvalidOperationException ("Reference frame has no maneuver node");
                return new Node (InternalVessel, node);
            }
        }

        /// <summary>
        /// The part.
        /// </summary>
        public Parts.Part Part {
            get { return new Parts.Part (InternalPart); }
        }

        /// <summary>
        /// The docking port.
        /// </summary>
        public DockingPort DockingPort {
            get {
                if (dockingPort == null)
                    throw new InvalidOperationException ("Reference frame has no docking port");
                return new DockingPort (new Parts.Part (dockingPort.part));
            }
        }

        /// <summary>
        /// The thruster.
        /// </summary>
        public Thruster Thruster {
            get {
                if (thruster == null)
                    throw new InvalidOperationException ("Reference frame has no thruster");
                return thruster;
            }
        }

        /// <summary>
        /// The transform for the object that this reference frame is attached to.
        /// </summary>
        public Transform Transform {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                case ReferenceFrameType.CelestialBodyNonRotating:
                case ReferenceFrameType.CelestialBodyOrbital:
                    return body.transform;
                case ReferenceFrameType.Vessel:
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselSurface:
                case ReferenceFrameType.VesselSurfaceVelocity:
                case ReferenceFrameType.VesselOrbitSpeed:
                case ReferenceFrameType.VesselSurfaceSpeed:
                case ReferenceFrameType.VesselTargetSpeed:
                    return InternalVessel.transform;
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                    throw new InvalidOperationException ("Maneuver nodes do not have a transform");
                case ReferenceFrameType.OrbitNonRotating:
                case ReferenceFrameType.OrbitOrbital:
                    throw new InvalidOperationException ("Orbits do not have a transform");
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                    return InternalPart.transform;
                case ReferenceFrameType.DockingPort:
                    return dockingPort.nodeTransform;
                case ReferenceFrameType.Thrust:
                    return thruster.WorldTransform;
                case ReferenceFrameType.Relative:
                case ReferenceFrameType.Hybrid:
                    throw new InvalidOperationException ("Transform not available for relative or hybrid frames");
                default:
                    throw new InvalidOperationException ("No such reference frame");
                }
            }
        }

        global::Vessel InternalVessel {
            get {
                if (vesselId == Guid.Empty)
                    throw new InvalidOperationException ("Reference frame has no vessel");
                return FlightGlobalsExtensions.GetVesselById (vesselId);
            }
        }

        Part InternalPart {
            get {
                if (partId == 0)
                    throw new InvalidOperationException ("Reference frame has no part");
                return FlightGlobals.FindPartByID (partId);
            }
        }

        // The target of the vessel this frame is attached to (another vessel, a
        // celestial body or a docking port), as set in-game. There is a single target,
        // shared by the active vessel, so this is only meaningful for that vessel.
        static ITargetable InternalTarget {
            get {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target == null)
                    throw new InvalidOperationException ("Reference frame has no target; the vessel's target is not set");
                return target;
            }
        }

        // The velocity of the vessel this frame is attached to, relative to its target, in
        // world space. This is the velocity the navball shows in 'target' mode. It is
        // measured against the same target velocity that the frame moves with, so the
        // vessel's velocity in the frame points exactly along it.
        Vector3d TargetRelativeVelocity {
            get { return InternalVessel.GetOrbit ().GetVel () - InternalTarget.GetWorldVelocity (); }
        }

        // The navball speed mode frames take their orientation from the velocity they
        // measure, so they are singular when it is zero -- as is the navball's prograde
        // marker, which the game stops drawing at zero speed.
        static void CheckSpeedNotSingular (Vector3d velocity, string name, string description)
        {
            if (velocity.sqrMagnitude < 0.01d)
                throw new InvalidOperationException (
                    name + " reference frame is singular when " + description + " is near zero");
        }

        internal static ReferenceFrame Object (global::CelestialBody body)
        {
            return new ReferenceFrame (ReferenceFrameType.CelestialBody, body);
        }

        internal static ReferenceFrame NonRotating (global::CelestialBody body)
        {
            return new ReferenceFrame (ReferenceFrameType.CelestialBodyNonRotating, body, null, null);
        }

        internal static ReferenceFrame Orbital (global::CelestialBody body)
        {
            if (body == body.referenceBody || body.orbit == null)
                throw new ArgumentException ("CelestialBody '" + body.name + "' does not orbit anything");
            return new ReferenceFrame (ReferenceFrameType.CelestialBodyOrbital, body);
        }

        internal static ReferenceFrame Object (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.Vessel, vessel: vessel);
        }

        internal static ReferenceFrame Orbital (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.VesselOrbital, vessel: vessel);
        }

        internal static ReferenceFrame Surface (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.VesselSurface, vessel: vessel);
        }

        internal static ReferenceFrame SurfaceVelocity (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.VesselSurfaceVelocity, vessel: vessel);
        }

        internal static ReferenceFrame OrbitSpeed (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.VesselOrbitSpeed, vessel: vessel);
        }

        internal static ReferenceFrame SurfaceSpeed (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.VesselSurfaceSpeed, vessel: vessel);
        }

        internal static ReferenceFrame TargetSpeed (global::Vessel vessel)
        {
            return new ReferenceFrame (ReferenceFrameType.VesselTargetSpeed, vessel: vessel);
        }

        internal static ReferenceFrame Object (global::Vessel vessel, ManeuverNode node)
        {
            return new ReferenceFrame (ReferenceFrameType.Maneuver, vessel: vessel, node: node);
        }

        internal static ReferenceFrame Orbital (global::Vessel vessel, ManeuverNode node)
        {
            return new ReferenceFrame (ReferenceFrameType.ManeuverOrbital, vessel: vessel, node: node);
        }

        internal static ReferenceFrame NonRotating (Orbit orbit)
        {
            return new ReferenceFrame (ReferenceFrameType.OrbitNonRotating, orbit: orbit);
        }

        internal static ReferenceFrame Orbital (Orbit orbit)
        {
            return new ReferenceFrame (ReferenceFrameType.OrbitOrbital, orbit: orbit);
        }

        internal static ReferenceFrame Object (Part part)
        {
            return new ReferenceFrame (ReferenceFrameType.Part, part: part);
        }

        internal static ReferenceFrame ObjectCenterOfMass (Part part)
        {
            return new ReferenceFrame (ReferenceFrameType.PartCenterOfMass, part: part);
        }

        internal static ReferenceFrame Object (ModuleDockingNode dockingPort)
        {
            return new ReferenceFrame (ReferenceFrameType.DockingPort, dockingPort: dockingPort);
        }

        internal static ReferenceFrame Thrust (Thruster thruster)
        {
            return new ReferenceFrame (ReferenceFrameType.Thrust, part: thruster.Part.InternalPart, thruster: thruster);
        }

        static class VectorZero
        {
            public static object Create ()
            {
                return new Tuple3 (0, 0, 0);
            }
        }

        static class QuaternionIdentity
        {
            public static object Create ()
            {
                return new Tuple4 (0, 0, 0, 1);
            }
        }

        /// <summary>
        /// Create a relative reference frame. This is a custom reference frame
        /// whose components offset the components of a parent reference frame.
        /// </summary>
        /// <param name="referenceFrame">The parent reference frame on which to
        /// base this reference frame.</param>
        /// <param name="position">The offset of the position of the origin,
        /// as a position vector. Defaults to <math>(0, 0, 0)</math></param>
        /// <param name="rotation">The rotation to apply to the parent frames rotation,
        /// as a quaternion of the form <math>(x, y, z, w)</math>.
        /// Defaults to <math>(0, 0, 0, 1)</math> (i.e. no rotation)</param>
        /// <param name="velocity">The linear velocity to offset the parent frame by,
        /// as a vector pointing in the direction of travel, whose magnitude is the speed in
        /// meters per second. Defaults to <math>(0, 0, 0)</math>.</param>
        /// <param name="angularVelocity">The angular velocity to offset the parent frame by,
        /// as a vector. This vector points in the direction of the axis of rotation,
        /// and its magnitude is the speed of the rotation in radians per second.
        /// Defaults to <math>(0, 0, 0)</math>.</param>
        [KRPCMethod]
        public static ReferenceFrame CreateRelative (
            ReferenceFrame referenceFrame,
            [KRPCDefaultValue(typeof(VectorZero))] Tuple3 position,
            [KRPCDefaultValue(typeof(QuaternionIdentity))] Tuple4 rotation,
            [KRPCDefaultValue(typeof(VectorZero))] Tuple3 velocity,
            [KRPCDefaultValue(typeof(VectorZero))] Tuple3 angularVelocity)
        {
            return new ReferenceFrame (referenceFrame, position.ToVector (), rotation.ToQuaternion (), velocity.ToVector (), angularVelocity.ToVector ());
        }

        /// <summary>
        /// Create a hybrid reference frame. This is a custom reference frame
        /// whose components inherited from other reference frames.
        /// </summary>
        /// <param name="position">The reference frame providing the position of the origin.</param>
        /// <param name="rotation">The reference frame providing the rotation of the frame.</param>
        /// <param name="velocity">The reference frame providing the linear velocity of the frame.
        /// </param>
        /// <param name="angularVelocity">The reference frame providing the angular velocity
        /// of the frame.</param>
        /// <remarks>
        /// The <paramref name="position"/> reference frame is required but all other
        /// reference frames are optional. If omitted, they are set to the
        /// <paramref name="position"/> reference frame.
        /// </remarks>
        [KRPCMethod]
        public static ReferenceFrame CreateHybrid (ReferenceFrame position, ReferenceFrame rotation = null, ReferenceFrame velocity = null, ReferenceFrame angularVelocity = null)
        {
            if (rotation == null)
                rotation = position;
            if (velocity == null)
                velocity = position;
            if (angularVelocity == null)
                angularVelocity = position;
            return new ReferenceFrame (ReferenceFrameType.Hybrid, hybridPosition: position, hybridRotation: rotation, hybridVelocity: velocity, hybridAngularVelocity: angularVelocity);
        }

        /// <summary>
        /// Returns the position of the origin of the reference frame in world-space.
        /// </summary>
        public Vector3d Position {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                case ReferenceFrameType.CelestialBodyNonRotating:
                case ReferenceFrameType.CelestialBodyOrbital:
                    return body.position;
                case ReferenceFrameType.Vessel:
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselSurface:
                case ReferenceFrameType.VesselSurfaceVelocity:
                case ReferenceFrameType.VesselOrbitSpeed:
                case ReferenceFrameType.VesselSurfaceSpeed:
                case ReferenceFrameType.VesselTargetSpeed:
                    return InternalVessel.CoM;
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                    {
                        // Orbit positions don't exactly coincide with transform world
                        // positions, so the node's orbit position is corrected by the
                        // offset between the two spaces, measured on the node's vessel
                        // whose position is known in both.
                        var vesselPos = InternalVessel.CoM;
                        var vesselOrbitPos = InternalVessel.orbit.getPositionAtUT (Planetarium.GetUniversalTime ());
                        var nodeOrbitPos = node.patch.getPositionAtUT (node.UT);
                        return vesselPos - vesselOrbitPos + nodeOrbitPos;
                    }
                case ReferenceFrameType.OrbitNonRotating:
                case ReferenceFrameType.OrbitOrbital:
                    // An orbit is not attached to anything in the scene, so it has no
                    // transform position to correct this towards the way a maneuver node,
                    // which belongs to a vessel, does. The orbit is the whole of it.
                    return orbit.InternalOrbit.getPositionAtUT (Planetarium.GetUniversalTime ());
                case ReferenceFrameType.Part:
                    return InternalPart.transform.position;
                case ReferenceFrameType.PartCenterOfMass:
                    return InternalPart.CenterOfMass ();
                case ReferenceFrameType.DockingPort:
                    return dockingPort.nodeTransform.position;
                case ReferenceFrameType.Thrust:
                    return thruster.WorldTransform.position;
                case ReferenceFrameType.Relative:
                    return parent.PositionToWorldSpace (relativePosition);
                case ReferenceFrameType.Hybrid:
                    return hybridPosition.Position;
                default:
                    throw new InvalidOperationException ();
                }
            }
        }

        /// <summary>
        /// Returns the rotation of the given frame of reference, relative to world space.
        /// Applying the rotation to a vector in reference-frame-space produces the corresponding
        /// vector in world-space.
        /// </summary>
        public QuaternionD Rotation {
            get {
                // For transform-backed frames use the Unity quaternion directly.
                // Unity derives transform.up/forward from the quaternion, so
                // q.Inverse * up == (0,1,0) to near double precision, avoiding
                // the ~1e-7 error that LookRotation2 introduces when reconstructing
                // from float Vector3 up/forward values.
                switch (type) {
                case ReferenceFrameType.Vessel:
                    return ((QuaternionD)InternalVessel.ReferenceTransform.rotation).Normalize ();
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                    return ((QuaternionD)InternalPart.transform.rotation).Normalize ();
                case ReferenceFrameType.Hybrid:
                    return hybridRotation.Rotation;
                case ReferenceFrameType.VesselSurfaceVelocity:
                    if (InternalVessel.srf_velocity.sqrMagnitude < 0.01d)
                        throw new InvalidOperationException (
                            "VesselSurfaceVelocity reference frame is singular when surface velocity is near zero");
                    break;
                case ReferenceFrameType.VesselSurfaceSpeed:
                    CheckSpeedNotSingular (
                        InternalVessel.srf_velocity, "VesselSurfaceSpeed", "surface velocity");
                    break;
                case ReferenceFrameType.VesselTargetSpeed:
                    CheckSpeedNotSingular (
                        TargetRelativeVelocity, "VesselTargetSpeed", "velocity relative to the target");
                    break;
                default:
                    break;
                }
                // Note: up is along the y-axis, forward is along the z-axis
                Vector3d up = UpNotNormalized;
                Vector3d forward = ForwardNotNormalized;
                // Check that the up and forward directions are roughly perpendicular
                if (Math.Abs (Vector3d.Dot (up.normalized, forward.normalized)) > 0.1)
                    throw new InvalidOperationException ("Forward and up directions are not close to perpendicular, got " + up + " and " + forward);
                GeometryExtensions.OrthoNormalize2 (ref forward, ref up);
                return GeometryExtensions.LookRotation2 (forward, up);
            }
        }

        /// <summary>
        /// Returns the up vector of the reference frame in world coordinates.
        /// The direction in which the y-axis points.
        /// </summary>
        public Vector3d Up {
            get { return UpNotNormalized.normalized; }
        }

        /// <summary>
        /// Returns the forward vector of the reference frame in world coordinates.
        /// The direction in which the z axis points.
        /// </summary>
        public Vector3d Forward {
            get { return ForwardNotNormalized.normalized; }
        }

        /// <summary>
        /// Vector from given vessel to north pole of body being orbited, in world space.
        /// </summary>
        static Vector3d ToNorthPole (global::Vessel vessel)
        {
            var parent = vessel.mainBody;
            return parent.position + ((Vector3d)parent.transform.up) * parent.Radius - (vessel.CoM);
        }

        /// <summary>
        /// Vector from the center of the body being orbited to the given vessel, in world
        /// space. Points in the zenith direction.
        /// </summary>
        static Vector3d ToZenith (global::Vessel vessel)
        {
            return vessel.CoM - vessel.mainBody.position;
        }

        // The velocity the orbit this frame is built on has reached at the current time,
        // relative to the body being orbited, in world space.
        Vector3d OrbitVelocityNow {
            get {
                return orbit.InternalOrbit.getOrbitalVelocityAtUT (
                    Planetarium.GetUniversalTime ()).SwapYZ ();
            }
        }

        /// <summary>
        /// Returns the up vector for the reference frame in world coordinates.
        /// The direction in which the y-axis points.
        /// The vector is not normalized.
        /// </summary>
        Vector3d UpNotNormalized {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                    return body.bodyTransform.up;
                case ReferenceFrameType.CelestialBodyNonRotating:
                    return Planetarium.up;
                case ReferenceFrameType.CelestialBodyOrbital:
                    return body.orbit.GetVel () - body.orbit.referenceBody.GetWorldVelocity ();
                case ReferenceFrameType.Vessel:
                    return InternalVessel.ReferenceTransform.up;
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselOrbitSpeed:
                    return InternalVessel.GetOrbit ().GetVel ();
                case ReferenceFrameType.VesselSurface:
                    {
                        var right = InternalVessel.CoM - InternalVessel.mainBody.position;
                        return Vector3d.Exclude (right, ToNorthPole (InternalVessel).normalized);
                    }
                case ReferenceFrameType.VesselSurfaceVelocity:
                case ReferenceFrameType.VesselSurfaceSpeed:
                    return InternalVessel.srf_velocity;
                case ReferenceFrameType.VesselTargetSpeed:
                    return TargetRelativeVelocity;
                case ReferenceFrameType.Maneuver:
                    return node.GetBurnVector(node.patch);
                case ReferenceFrameType.ManeuverOrbital:
                    return node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ ();
                case ReferenceFrameType.OrbitNonRotating:
                    return Planetarium.up;
                case ReferenceFrameType.OrbitOrbital:
                    return OrbitVelocityNow;
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                    return InternalPart.transform.up;
                case ReferenceFrameType.DockingPort:
                    return dockingPort.nodeTransform.forward;
                case ReferenceFrameType.Thrust:
                    return thruster.WorldThrustDirection;
                case ReferenceFrameType.Relative:
                    return parent.DirectionToWorldSpace (relativeRotation * Vector3d.up);
                case ReferenceFrameType.Hybrid:
                    return hybridRotation.UpNotNormalized;
                default:
                    throw new InvalidOperationException ();
                }
            }
        }

        /// <summary>
        /// Returns the forward vector of the reference frame in world coordinates.
        /// The direction in which the z-axis points.
        /// The vector is not normalized.
        /// </summary>
        Vector3d ForwardNotNormalized {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                    return body.bodyTransform.forward;
                case ReferenceFrameType.CelestialBodyNonRotating:
                    return Planetarium.forward;
                case ReferenceFrameType.CelestialBodyOrbital:
                    {
                        var up = UpNotNormalized;
                        var radial = body.referenceBody.position - body.position;
                        return Vector3d.Cross (radial, up);
                    }
                case ReferenceFrameType.Vessel:
                    return InternalVessel.ReferenceTransform.forward;
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselOrbitSpeed:
                    return InternalVessel.GetOrbit ().GetOrbitNormal ().SwapYZ ();
                case ReferenceFrameType.VesselSurface:
                    {
                        var right = InternalVessel.CoM - InternalVessel.mainBody.position;
                        var northPole = ToNorthPole (InternalVessel).normalized;
                        return Vector3d.Cross (right, northPole);
                    }
                case ReferenceFrameType.VesselSurfaceSpeed:
                case ReferenceFrameType.VesselTargetSpeed:
                    {
                        // Normal to the plane of the motion, in the same sense as the
                        // orbital frame's normal: the velocity crossed with the zenith.
                        // The remaining axis then points towards the body, opposite the
                        // navball's radial out marker.
                        return Vector3d.Cross (UpNotNormalized, ToZenith (InternalVessel));
                    }
                case ReferenceFrameType.VesselSurfaceVelocity:
                    {
                        // Compute orthogonal vector to vessels velocity, in the horizon plane
                        var up = (InternalVessel.CoM - InternalVessel.mainBody.position).normalized;
                        var velocity = InternalVessel.srf_velocity;
                        var proj = GeometryExtensions.ProjectVectorOntoPlane (up, velocity);
                        return Vector3d.Cross (up, proj);
                    }
                case ReferenceFrameType.Maneuver:
                    {
                        var up = UpNotNormalized;
                        // Pick an arbitrary vector that is not close to the burn vector
                        var forward = Planetarium.forward;
                        if (Math.Abs (Vector3d.Dot (up.normalized, forward)) > 0.9)
                            forward = Planetarium.up;
                        // Make the arbitrary vector orthogonal to the burn vector
                        GeometryExtensions.OrthoNormalize2 (ref up, ref forward);
                        return forward;
                    }
                case ReferenceFrameType.ManeuverOrbital:
                    return node.patch.GetOrbitNormal ().SwapYZ ();
                case ReferenceFrameType.OrbitNonRotating:
                    return Planetarium.forward;
                case ReferenceFrameType.OrbitOrbital:
                    return orbit.InternalOrbit.GetOrbitNormal ().SwapYZ ();
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                    return InternalPart.transform.forward;
                case ReferenceFrameType.DockingPort:
                    return -dockingPort.nodeTransform.up;
                case ReferenceFrameType.Thrust:
                    return thruster.WorldThrustPerpendicularDirection;
                case ReferenceFrameType.Relative:
                    return parent.DirectionToWorldSpace (relativeRotation * Vector3d.forward);
                case ReferenceFrameType.Hybrid:
                    return hybridRotation.ForwardNotNormalized;
                default:
                    throw new InvalidOperationException ();
                }
            }
        }

        /// <summary>
        /// Returns the velocity of the reference frame in world-space.
        /// </summary>
        public Vector3d Velocity {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                case ReferenceFrameType.CelestialBodyNonRotating:
                case ReferenceFrameType.CelestialBodyOrbital:
                    return body.GetWorldVelocity ();
                case ReferenceFrameType.Vessel:
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselSurface:
                case ReferenceFrameType.VesselSurfaceVelocity:
                    return InternalVessel.GetOrbit ().GetVel ();
                case ReferenceFrameType.VesselOrbitSpeed:
                    // The body being orbited, not rotating with it. The vessel's velocity
                    // relative to this is the navball's orbital velocity.
                    return InternalVessel.mainBody.GetWorldVelocity ();
                case ReferenceFrameType.VesselSurfaceSpeed: {
                    // The point on the rotating body that the vessel is currently above,
                    // so that the vessel's velocity relative to this is the navball's
                    // surface velocity.
                    var vessel = InternalVessel;
                    var mainBody = vessel.mainBody;
                    return mainBody.GetWorldVelocity () + mainBody.getRFrmVel (vessel.CoM);
                }
                case ReferenceFrameType.VesselTargetSpeed:
                    return InternalTarget.GetWorldVelocity ();
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                    return node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ () + node.patch.referenceBody.GetWorldVelocity ();
                case ReferenceFrameType.OrbitNonRotating:
                case ReferenceFrameType.OrbitOrbital:
                    return OrbitVelocityNow + orbit.InternalOrbit.referenceBody.GetWorldVelocity ();
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                case ReferenceFrameType.Thrust:
                    // The vessel's velocity, taken at its center of mass. A part offset from the
                    // center of mass on a rotating vessel also moves at omega x (part - CoM), and
                    // that term is not included here, so this understates the part's true velocity
                    // by the tangential contribution of the vessel's spin.
                    return InternalPart.vessel.GetOrbit ().GetVel ();
                case ReferenceFrameType.DockingPort:
                    return dockingPort.vessel.GetOrbit ().GetVel ();
                case ReferenceFrameType.Relative:
                    return parent.VelocityToWorldSpace (relativePosition, relativeVelocity);
                case ReferenceFrameType.Hybrid:
                    return hybridVelocity.Velocity;
                default:
                    throw new InvalidOperationException ();
                }
            }
        }

        /// <summary>
        /// Returns the rotational velocity of the reference frame in world-space.
        /// Vector points in direction of axis of rotation
        /// Vector's magnitude is the speed of rotation in radians per second
        /// </summary>
        public Vector3d AngularVelocity {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                    return body.angularVelocity;
                case ReferenceFrameType.CelestialBodyNonRotating:
                    return Vector3d.zero;
                case ReferenceFrameType.CelestialBodyOrbital: {
                    var refBody = body.referenceBody;
                    var r = body.position - refBody.position;
                    var v = body.GetWorldVelocity () - refBody.GetWorldVelocity ();
                    return Vector3d.Cross (r, v) / r.sqrMagnitude;
                }
                case ReferenceFrameType.Vessel:
                    return InternalVessel.WorldAngularVelocity ();
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselOrbitSpeed: {
                    var r = InternalVessel.CoM - InternalVessel.mainBody.position;
                    var v = InternalVessel.GetOrbit ().GetVel ();
                    return Vector3d.Cross (r, v) / r.sqrMagnitude;
                }
                case ReferenceFrameType.VesselSurface: {
                    // The surface frame's orientation tracks the vessel's inertial
                    // position (zenith points from the body centre to the vessel) and
                    // the body's spin axis (north), so its angular velocity is the rate
                    // at which that basis rotates -- not the body's rotation rate. It is
                    // the orbital sweep of the zenith axis plus the twist of the north
                    // direction about the zenith as the vessel's latitude changes.
                    var vessel = InternalVessel;
                    var mainBody = vessel.mainBody;
                    var r = vessel.CoM - mainBody.position;
                    var v = vessel.GetOrbit ().GetVel ();
                    var rSqr = r.sqrMagnitude;
                    var rxv = Vector3d.Cross (r, v);
                    // Zenith swing (perpendicular to the vertical).
                    var angularVelocity = rxv / rSqr;
                    // Twist of the horizon-projected north about the vertical. Vanishes
                    // on equatorial orbits; singular at the poles (as is the frame).
                    var spinAxis = (Vector3d)mainBody.transform.up;
                    var rDotS = Vector3d.Dot (r, spinAxis);
                    var rPerpSqr = rSqr - rDotS * rDotS;
                    if (rPerpSqr > rSqr * 1e-6)
                        angularVelocity += (Vector3d.Dot (spinAxis, rxv) * rDotS / (rPerpSqr * rSqr)) * r;
                    return angularVelocity;
                }
                case ReferenceFrameType.VesselSurfaceVelocity: {
                    var vessel = InternalVessel;
                    var srf_vel = vessel.srf_velocity;
                    if (srf_vel.sqrMagnitude < 0.01d)
                        return vessel.mainBody.angularVelocity;
                    // d(srf_vel)/dt ≈ grav − body.ω × v_orbital
                    var grav = (Vector3d)FlightGlobals.getGeeForceAtPosition (vessel.CoM);
                    var v_orb = vessel.GetOrbit ().GetVel ();
                    var a_srf = grav - Vector3d.Cross (vessel.mainBody.angularVelocity, v_orb);
                    return vessel.mainBody.angularVelocity + Vector3d.Cross (srf_vel, a_srf) / srf_vel.sqrMagnitude;
                }
                case ReferenceFrameType.VesselSurfaceSpeed: {
                    var vessel = InternalVessel;
                    var srfVelocity = vessel.srf_velocity;
                    CheckSpeedNotSingular (srfVelocity, "VesselSurfaceSpeed", "surface velocity");
                    // d(srf_vel)/dt ≈ grav − body.ω × v_orbital
                    var grav = (Vector3d)FlightGlobals.getGeeForceAtPosition (vessel.CoM);
                    var acceleration = grav - Vector3d.Cross (
                        vessel.mainBody.angularVelocity, vessel.GetOrbit ().GetVel ());
                    return SpeedModeAngularVelocity (vessel, srfVelocity, acceleration);
                }
                case ReferenceFrameType.VesselTargetSpeed: {
                    var vessel = InternalVessel;
                    var relativeVelocity = TargetRelativeVelocity;
                    CheckSpeedNotSingular (
                        relativeVelocity, "VesselTargetSpeed", "velocity relative to the target");
                    // Vessel and target are both in free fall, so their relative velocity
                    // turns with the difference in gravity between the two positions. Each
                    // is taken against the body that object orbits, as the gravity of a
                    // body at its own center -- where a targeted body sits -- is undefined.
                    // Whatever pulls on both of them cancels in the difference. Thrust and
                    // drag on either of them are not accounted for.
                    var target = InternalTarget;
                    var targetOrbit = target.GetOrbit ();
                    var acceleration =
                        (Vector3d)FlightGlobals.getGeeForceAtPosition (vessel.CoM, vessel.mainBody);
                    if (targetOrbit != null)
                        acceleration -= (Vector3d)FlightGlobals.getGeeForceAtPosition (
                            target.GetTransform ().position, targetOrbit.referenceBody);
                    return SpeedModeAngularVelocity (vessel, relativeVelocity, acceleration);
                }
                case ReferenceFrameType.Maneuver: {
                    // The burn vector is stored in prograde/normal/radial components, so it rotates
                    // with the orbital frame. The z-axis (arbitrary inertial projection) introduces
                    // a small spin correction about ŷ that is neglected here.
                    var r = node.patch.getRelativePositionAtUT (node.UT).SwapYZ ();
                    var v = node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ ();
                    return Vector3d.Cross (r, v) / r.sqrMagnitude;
                }
                case ReferenceFrameType.ManeuverOrbital: {
                    var r = node.patch.getRelativePositionAtUT (node.UT).SwapYZ ();
                    var v = node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ ();
                    return Vector3d.Cross (r, v) / r.sqrMagnitude;
                }
                case ReferenceFrameType.OrbitNonRotating:
                    return Vector3d.zero;
                case ReferenceFrameType.OrbitOrbital: {
                    var ut = Planetarium.GetUniversalTime ();
                    var r = orbit.InternalOrbit.getRelativePositionAtUT (ut).SwapYZ ();
                    var v = orbit.InternalOrbit.getOrbitalVelocityAtUT (ut).SwapYZ ();
                    return Vector3d.Cross (r, v) / r.sqrMagnitude;
                }
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                    return InternalPart.vessel.WorldAngularVelocity ();
                case ReferenceFrameType.DockingPort:
                    return dockingPort.vessel.WorldAngularVelocity ();
                case ReferenceFrameType.Thrust:
                    return InternalPart.vessel.WorldAngularVelocity ();
                case ReferenceFrameType.Relative:
                    return parent.AngularVelocityToWorldSpace (relativeAngularVelocity);
                case ReferenceFrameType.Hybrid:
                    return hybridAngularVelocity.AngularVelocity;
                default:
                    throw new InvalidOperationException ();
                }
            }
        }

        /// <summary>
        /// The angular velocity, in world space, of a navball speed mode frame oriented by
        /// the given velocity, which is changing at the given acceleration.
        /// </summary>
        /// <remarks>
        /// The basis is ŷ along the velocity, ẑ along the velocity crossed with the zenith
        /// and x̂ the remaining axis. For a basis whose axes obey ė = ω × e, the angular
        /// velocity is ω = (ŷ × dŷ/dt) + ŷ (x̂ · dẑ/dt): the swing of the velocity direction,
        /// plus the roll about it as the plane through the velocity and the zenith turns.
        /// </remarks>
        static Vector3d SpeedModeAngularVelocity (
            global::Vessel vessel, Vector3d velocity, Vector3d acceleration)
        {
            var swing = Vector3d.Cross (velocity, acceleration) / velocity.sqrMagnitude;
            // The zenith runs from the center of the body to the vessel, so it changes at
            // the vessel's orbital velocity.
            var zenith = ToZenith (vessel);
            var zenithRate = vessel.GetOrbit ().GetVel ();
            var normal = Vector3d.Cross (velocity, zenith);
            var normalRate = Vector3d.Cross (acceleration, zenith) + Vector3d.Cross (velocity, zenithRate);
            var y = velocity.normalized;
            var x = Vector3d.Cross (y, normal.normalized);
            var roll = Vector3d.Dot (x, normalRate) / normal.magnitude;
            return swing + roll * y;
        }

        /// <summary>
        /// Get the linear velocity at the given position that results from the reference frames angular velocity.
        /// Position is in reference frame space.
        /// </summary>
        public Vector3d AngularVelocityAt (Vector3d position)
        {
            var angularVelocity = Rotation.Inverse () * AngularVelocity;
            return Vector3d.Cross (angularVelocity, position);
        }

        /// <summary>
        /// Convert the given position in world space, to a position in this reference frame.
        /// </summary>
        public Vector3d PositionFromWorldSpace (Vector3d worldPosition)
        {
            return Rotation.Inverse () * (worldPosition - Position);
        }

        /// <summary>
        /// Convert the given position in this reference frame, to a position in world space.
        /// </summary>
        public Vector3d PositionToWorldSpace (Vector3d position)
        {
            return Position + (Rotation * position);
        }

        /// <summary>
        /// Convert the given direction in world space, to a direction in this reference frame.
        /// </summary>
        public Vector3d DirectionFromWorldSpace (Vector3d worldDirection)
        {
            return Rotation.Inverse () * worldDirection;
        }

        /// <summary>
        /// Convert the given direction in this reference frame, to a direction in world space.
        /// </summary>
        public Vector3d DirectionToWorldSpace (Vector3d direction)
        {
            return Rotation * direction;
        }

        /// <summary>
        /// Convert the given rotation in world space, to a rotation in this reference frame.
        /// </summary>
        public QuaternionD RotationFromWorldSpace (QuaternionD worldRotation)
        {
            return Rotation.Inverse () * worldRotation;
        }

        /// <summary>
        /// Convert the given rotation in this reference frame, to a rotation in world space.
        /// </summary>
        public QuaternionD RotationToWorldSpace (QuaternionD rotation)
        {
            return Rotation * rotation;
        }

        /// <summary>
        /// Convert the given velocity at the given position in world space,
        /// to a velocity in this reference frame.
        /// </summary>
        public Vector3d VelocityFromWorldSpace (Vector3d worldPosition, Vector3d worldVelocity)
        {
            return (Rotation.Inverse () * (worldVelocity - Velocity)) - AngularVelocityAt (PositionFromWorldSpace (worldPosition));
        }

        /// <summary>
        /// Convert the given velocity at the given position in this reference frame,
        /// to a velocity in world space.
        /// </summary>
        public Vector3d VelocityToWorldSpace (Vector3d position, Vector3d velocity)
        {
            return Velocity + (Rotation * (velocity + AngularVelocityAt (position)));
        }

        /// <summary>
        /// Convert the given angular velocity in world space,
        /// to an angular velocity in this reference frame.
        /// This only make sense when considering an object that
        /// is rotating at the origin of the reference frame.
        /// </summary>
        public Vector3d AngularVelocityFromWorldSpace (Vector3d worldAngularVelocity)
        {
            return Rotation.Inverse () * (worldAngularVelocity - AngularVelocity);
        }

        /// <summary>
        /// Convert the given angular velocity at the given position in this reference frame,
        /// to an angular velocity in world space.
        /// This only make sense when considering an object that is rotating at the origin
        /// of the reference frame.
        /// </summary>
        public Vector3d AngularVelocityToWorldSpace (Vector3d angularVelocity)
        {
            return AngularVelocity + (Rotation * angularVelocity);
        }
    }
}
