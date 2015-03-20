using System;
using UnityEngine;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class ReferenceFrame : Equatable<ReferenceFrame>
    {
        enum Type
        {
            CelestialBody,
            CelestialBodyNonRotating,
            CelestialBodyOrbital,
            Vessel,
            VesselOrbital,
            VesselSurface,
            Maneuver,
            ManeuverOrbital
        }

        readonly Type type;
        readonly global::CelestialBody body;
        readonly global::Vessel vessel;
        readonly ManeuverNode node;

        ReferenceFrame (Type type, global::CelestialBody body, global::Vessel vessel, ManeuverNode node)
        {
            this.type = type;
            this.body = body;
            this.vessel = vessel;
            this.node = node;
        }

        public override bool Equals (ReferenceFrame obj)
        {
            return type == obj.type && body == obj.body && vessel == obj.vessel && node == obj.node;
        }

        public override int GetHashCode ()
        {
            var hash = type.GetHashCode ();
            if (body != null)
                hash ^= body.GetHashCode ();
            if (vessel != null)
                hash ^= vessel.GetHashCode ();
            if (node != null)
                hash ^= node.GetHashCode ();
            return hash;
        }

        internal static ReferenceFrame Object (global::CelestialBody body)
        {
            return new ReferenceFrame (Type.CelestialBody, body, null, null);
        }

        internal static ReferenceFrame NonRotating (global::CelestialBody body)
        {
            return new ReferenceFrame (Type.CelestialBodyNonRotating, body, null, null);
        }

        internal static ReferenceFrame Orbital (global::CelestialBody body)
        {
            if (body == body.referenceBody)
                throw new ArgumentException ("CelestialBody '" + body.name + "' does not orbit anything");
            return new ReferenceFrame (Type.CelestialBodyOrbital, body, null, null);
        }

        internal static ReferenceFrame Object (global::Vessel vessel)
        {
            return new ReferenceFrame (Type.Vessel, null, vessel, null);
        }

        internal static ReferenceFrame Orbital (global::Vessel vessel)
        {
            return new ReferenceFrame (Type.VesselOrbital, null, vessel, null);
        }

        internal static ReferenceFrame Surface (global::Vessel vessel)
        {
            return new ReferenceFrame (Type.VesselSurface, null, vessel, null);
        }

        internal static ReferenceFrame Object (ManeuverNode node)
        {
            return new ReferenceFrame (Type.Maneuver, null, null, node);
        }

        internal static ReferenceFrame Orbital (ManeuverNode node)
        {
            return new ReferenceFrame (Type.ManeuverOrbital, null, null, node);
        }

        /// <summary>
        /// Returns the position of the origin of the reference frame in world-space.
        /// </summary>
        public Vector3d Position {
            get {
                switch (type) {
                case Type.CelestialBody:
                case Type.CelestialBodyNonRotating:
                case Type.CelestialBodyOrbital:
                    return body.position;
                case Type.Vessel:
                case Type.VesselOrbital:
                case Type.VesselSurface:
                    return vessel.GetWorldPos3D ();
                case Type.Maneuver:
                case Type.ManeuverOrbital:
                    {
                        //TODO: is there a better way to do this?
                        // node.patch.getPositionAtUT (node.UT) appears to return a position vector
                        // in a different space to vessel.GetWorldPos3D()
                        var vesselPos = FlightGlobals.ActiveVessel.GetWorldPos3D ();
                        var vesselOrbitPos = FlightGlobals.ActiveVessel.orbit.getPositionAtUT (Planetarium.GetUniversalTime ());
                        var nodeOrbitPos = node.patch.getPositionAtUT (node.UT);
                        return vesselPos - vesselOrbitPos + nodeOrbitPos;
                    }
                default:
                    throw new ArgumentException ("No such reference frame");
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
                    throw new ArithmeticException ("forward and up directions are not close to perpendicular");
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
            return parent.position + ((Vector3d)parent.transform.up) * parent.Radius - (vessel.GetWorldPos3D ());
        }

        /// <summary>
        /// Vector from center of given body to north pole of body being orbited, in world space.
        /// </summary>
        static Vector3d ToNorthPole (global::CelestialBody body)
        {
            var parent = body.referenceBody;
            return parent.position + (((Vector3d)parent.transform.up) * parent.Radius) - body.position;
        }

        /// <summary>
        /// Returns the up vector for the reference frame in world coordinates.
        /// The direction in which the y-axis points.
        /// The vector is not normalized.
        /// </summary>
        Vector3d UpNotNormalized {
            get {
                switch (type) {
                case Type.CelestialBody:
                    return body.bodyTransform.up;
                case Type.CelestialBodyNonRotating:
                    return Planetarium.up;
                case Type.CelestialBodyOrbital:
                    {
                        return Vector3d.Exclude (Forward, ToNorthPole (body).normalized);
                    }
                case Type.Vessel:
                    return vessel.transform.up;
                case Type.VesselOrbital:
                    return vessel.GetOrbit ().GetVel ();
                case Type.VesselSurface:
                    {
                        var right = vessel.GetWorldPos3D () - vessel.mainBody.position;
                        return Vector3d.Exclude (right, ToNorthPole (vessel).normalized);
                    }
                case Type.Maneuver:
                    throw new NotImplementedException ();
                case Type.ManeuverOrbital:
                    return node.patch.GetOrbitNormal ().SwapYZ ();
                default:
                    throw new ArgumentException ("No such reference frame");
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
                case Type.CelestialBody:
                    return body.bodyTransform.forward;
                case Type.CelestialBodyNonRotating:
                    return Planetarium.forward;
                case Type.CelestialBodyOrbital:
                    {
                        var right = (body.position - body.referenceBody.position).normalized;
                        var northPole = ToNorthPole (body).normalized;
                        return Vector3d.Cross (right, northPole);
                    }
                case Type.Vessel:
                    return vessel.transform.forward;
                case Type.VesselOrbital:
                    return vessel.GetOrbit ().GetOrbitNormal ().SwapYZ ();
                case Type.VesselSurface:
                    {
                        var right = vessel.GetWorldPos3D () - vessel.mainBody.position;
                        var northPole = ToNorthPole (vessel).normalized;
                        return Vector3d.Cross (right, northPole);
                    }
                case Type.Maneuver:
                    throw new NotImplementedException ();
                case Type.ManeuverOrbital:
                    return node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ ();
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Returns the velocity of the reference frame in world-space.
        /// </summary>
        public Vector3d Velocity {
            get {
                switch (type) {
                case Type.CelestialBody:
                case Type.CelestialBodyNonRotating:
                case Type.CelestialBodyOrbital:
                    return body.GetWorldVelocity ();
                case Type.Vessel:
                case Type.VesselOrbital:
                case Type.VesselSurface:
                    return vessel.GetOrbit ().GetVel ();
                case Type.Maneuver:
                case Type.ManeuverOrbital:
                    throw new NotImplementedException ();
                default:
                    throw new ArgumentException ("No such reference frame");
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
                case Type.CelestialBody:
                    return body.angularVelocity;
                case Type.CelestialBodyNonRotating:
                    return Vector3d.zero;
                case Type.CelestialBodyOrbital:
                    throw new NotImplementedException ();
                case Type.Vessel:
                    return vessel.angularVelocity;
                case Type.VesselOrbital:
                    throw new NotImplementedException ();
                case Type.VesselSurface:
                    throw new NotImplementedException ();
                case Type.Maneuver:
                    throw new NotImplementedException ();
                case Type.ManeuverOrbital:
                    throw new NotImplementedException ();
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Get the linear velocity at the given position that results from the reference frames angular velocity.
        /// Position is in reference frame space.
        /// </summary>
        Vector3d AngularVelocityAt (Vector3d position)
        {
            var axis = AngularVelocity.normalized;
            var plane_position = Vector3d.Exclude (axis, position);
            var radius = plane_position.magnitude;
            var plane_direction = plane_position.normalized;
            var direction = Vector3d.Cross (axis, plane_direction);
            return direction * AngularVelocity.magnitude * radius;
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
        /// Convert the given position in this reference frame, to a position in world space.
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

