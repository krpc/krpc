using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services.Parts;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

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
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    public class ReferenceFrame : Equatable<ReferenceFrame>
    {
        readonly ReferenceFrameType type;
        readonly global::CelestialBody body;
        readonly Guid vesselId;
        readonly ManeuverNode node;
        readonly uint partId;
        readonly ModuleDockingNode dockingPort;
        readonly Thruster thruster;
        readonly ReferenceFrame parent;
        readonly Vector3d relativePosition;
        readonly QuaternionD relativeRotation;
        readonly Vector3d relativeVelocity;
        readonly Vector3d relativeAngularVelocity;
        readonly ReferenceFrame hybridPosition;
        readonly ReferenceFrame hybridRotation;
        readonly ReferenceFrame hybridVelocity;
        readonly ReferenceFrame hybridAngularVelocity;

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        ReferenceFrame (
            ReferenceFrameType type, global::CelestialBody body = null, global::Vessel vessel = null,
            ManeuverNode node = null, Part part = null, ModuleDockingNode dockingPort = null,
            Thruster thruster = null, ReferenceFrame parent = null,
            ReferenceFrame hybridPosition = null, ReferenceFrame hybridRotation = null,
            ReferenceFrame hybridVelocity = null, ReferenceFrame hybridAngularVelocity = null)
        {
            this.type = type;
            this.body = body;
            vesselId = vessel != null ? vessel.id : Guid.Empty;
            this.node = node;
            // TODO: is it safe to use a part id of 0 to mean no part?
            if (part != null)
                partId = part.flightID;
            this.dockingPort = dockingPort;
            this.thruster = thruster;
            this.parent = parent;
            this.hybridPosition = hybridPosition;
            this.hybridRotation = hybridRotation;
            this.hybridVelocity = hybridVelocity;
            this.hybridAngularVelocity = hybridAngularVelocity;
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
                    return InternalVessel.transform;
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                    throw new InvalidOperationException ("Maneuver nodes do not have a transform");
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

        internal static ReferenceFrame Object (global::Vessel vessel, ManeuverNode node)
        {
            return new ReferenceFrame (ReferenceFrameType.Maneuver, vessel: vessel, node: node);
        }

        internal static ReferenceFrame Orbital (global::Vessel vessel, ManeuverNode node)
        {
            return new ReferenceFrame (ReferenceFrameType.ManeuverOrbital, vessel: vessel, node: node);
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

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        static class VectorZero
        {
            public static object Create ()
            {
                return new Tuple3 (0, 0, 0);
            }
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        static class QuaternionIdentity
        {
            public static object Create ()
            {
                return new Tuple4 (0, 0, 0, 1);
            }
        }

        /// <summary>
        /// Create a relative reference frame.
        /// </summary>
        /// <param name="referenceFrame">The parent reference frame.</param>
        /// <param name="position">The offset of the position of the origin.</param>
        /// <param name="rotation">The rotation to apply to the parent frames rotation, as a quaternion. Defaults to zero.</param>
        /// <param name="velocity">The linear velocity to offset the parent frame by. Defaults to zero.</param>
        /// <param name="angularVelocity">The angular velocity to offset the parent frame by. Defaults to zero.</param>
        [KRPCMethod]
        [KRPCDefaultValue ("position", typeof(VectorZero))]
        [KRPCDefaultValue ("rotation", typeof(QuaternionIdentity))]
        [KRPCDefaultValue ("velocity", typeof(VectorZero))]
        [KRPCDefaultValue ("angularVelocity", typeof(VectorZero))]
        public static ReferenceFrame CreateRelative (ReferenceFrame referenceFrame, Tuple3 position, Tuple4 rotation, Tuple3 velocity, Tuple3 angularVelocity)
        {
            return new ReferenceFrame (referenceFrame, position.ToVector (), rotation.ToQuaternion (), velocity.ToVector (), angularVelocity.ToVector ());
        }

        /// <summary>
        /// Create a hybrid reference frame, which is a custom reference frame
        /// whose components are inherited from other reference frames.
        /// </summary>
        /// <param name="position">The reference frame providing the position of the origin.</param>
        /// <param name="rotation">The reference frame providing the orientation of the frame.</param>
        /// <param name="velocity">The reference frame providing the linear velocity of the frame.</param>
        /// <param name="angularVelocity">The reference frame providing the angular velocity of the frame.</param>
        /// <remarks>
        /// The <paramref name="position"/> is required but all other reference frames are optional.
        /// If omitted, they are set to the <paramref name="position"/> reference frame.
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
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
                    return InternalVessel.CoM;
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                    {
                        // TODO: is there a better way to do this?
                        // node.patch.getPositionAtUT (node.UT) appears to return a position vector
                        // in a different space to vessel.GetWorldPos3D()
                        var vesselPos = FlightGlobals.ActiveVessel.CoM;
                        var vesselOrbitPos = FlightGlobals.ActiveVessel.orbit.getPositionAtUT (Planetarium.GetUniversalTime ());
                        var nodeOrbitPos = node.patch.getPositionAtUT (node.UT);
                        return vesselPos - vesselOrbitPos + nodeOrbitPos;
                    }
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
        /// Applying the rotation to a vector in reference-frame-space produces the corresponding vector in world-space.
        /// </summary>
        public QuaternionD Rotation {
            get {
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
        /// Returns the up vector for the reference frame in world coordinates.
        /// The direction in which the y-axis points.
        /// The vector is not normalized.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
                    return InternalVessel.GetOrbit ().GetVel ();
                case ReferenceFrameType.VesselSurface:
                    {
                        var right = InternalVessel.CoM - InternalVessel.mainBody.position;
                        return Vector3d.Exclude (right, ToNorthPole (InternalVessel).normalized);
                    }
                case ReferenceFrameType.VesselSurfaceVelocity:
                    return InternalVessel.srf_velocity;
                case ReferenceFrameType.Maneuver:
                    return new Node (InternalVessel, node).WorldBurnVector;
                case ReferenceFrameType.ManeuverOrbital:
                    return node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ ();
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
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
                    return InternalVessel.GetOrbit ().GetOrbitNormal ().SwapYZ ();
                case ReferenceFrameType.VesselSurface:
                    {
                        var right = InternalVessel.CoM - InternalVessel.mainBody.position;
                        var northPole = ToNorthPole (InternalVessel).normalized;
                        return Vector3d.Cross (right, northPole);
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
                        if (Vector3d.Dot (up, forward) < 0.1)
                            forward = Planetarium.up;
                        // Make the arbitrary vector orthogonal to the burn vector
                        GeometryExtensions.OrthoNormalize2 (ref up, ref forward);
                        return forward;
                    }
                case ReferenceFrameType.ManeuverOrbital:
                    return node.patch.GetOrbitNormal ().SwapYZ ();
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                    return Vector3d.zero; // TODO: check this
                case ReferenceFrameType.Part:
                case ReferenceFrameType.Thrust:
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public Vector3d AngularVelocity {
            get {
                switch (type) {
                case ReferenceFrameType.CelestialBody:
                    return body.angularVelocity;
                case ReferenceFrameType.CelestialBodyNonRotating:
                    return Vector3d.zero;
                case ReferenceFrameType.CelestialBodyOrbital:
                    return Vector3d.zero; // TODO: check this
                case ReferenceFrameType.Vessel:
                    return InternalVessel.GetComponent<Rigidbody> ().angularVelocity;
                case ReferenceFrameType.VesselOrbital:
                case ReferenceFrameType.VesselSurface:
                case ReferenceFrameType.VesselSurfaceVelocity:
                case ReferenceFrameType.Maneuver:
                case ReferenceFrameType.ManeuverOrbital:
                case ReferenceFrameType.Part:
                case ReferenceFrameType.DockingPort:
                case ReferenceFrameType.Thrust:
                    return Vector3d.zero; // TODO: check this
                case ReferenceFrameType.Relative:
                    return parent.AngularVelocityToWorldSpace (relativeAngularVelocity);
                case ReferenceFrameType.Hybrid:
                    return hybridVelocity.AngularVelocity;
                default:
                    throw new InvalidOperationException ();
                }
            }
        }

        /// <summary>
        /// Get the linear velocity at the given position that results from the reference frames angular velocity.
        /// Position is in reference frame space.
        /// </summary>
        public Vector3d AngularVelocityAt (Vector3d position)
        {
            var angularVelocity = AngularVelocity;
            var axis = angularVelocity.normalized;
            var plane_position = Vector3d.Exclude (axis, position);
            var radius = plane_position.magnitude;
            var plane_direction = plane_position.normalized;
            var direction = Vector3d.Cross (axis, plane_direction);
            return direction * angularVelocity.magnitude * radius;
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
        /// Convert the given velocity at the given position in world space, to a velocity in this reference frame.
        /// </summary>
        public Vector3d VelocityFromWorldSpace (Vector3d worldPosition, Vector3d worldVelocity)
        {
            return (Rotation.Inverse () * (worldVelocity - Velocity)) - AngularVelocityAt (PositionFromWorldSpace (worldPosition));
        }

        /// <summary>
        /// Convert the given velocity at the given position in this reference frame, to a velocity in world space.
        /// </summary>
        public Vector3d VelocityToWorldSpace (Vector3d position, Vector3d velocity)
        {
            return Velocity + (Rotation * (velocity + AngularVelocityAt (position)));
        }

        /// <summary>
        /// Convert the given angular velocity in world space, to an angular velocity in this reference frame.
        /// This only make sense when considering an object that is rotating at the origin of the reference frame.
        /// </summary>
        public Vector3d AngularVelocityFromWorldSpace (Vector3d worldAngularVelocity)
        {
            return Rotation.Inverse () * (worldAngularVelocity - AngularVelocity);
        }

        /// <summary>
        /// Convert the given angular velocity at the given position in this reference frame, to an angular velocity in world space.
        /// This only make sense when considering an object that is rotating at the origin of the reference frame.
        /// </summary>
        public Vector3d AngularVelocityToWorldSpace (Vector3d angularVelocity)
        {
            return AngularVelocity + (Rotation * angularVelocity);
        }
    }
}
