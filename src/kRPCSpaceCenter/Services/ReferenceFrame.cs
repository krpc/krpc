using System;
using System.Linq;
using UnityEngine;
using KRPC.Service.Attributes;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class ReferenceFrame
    {
        enum Type
        {
            CelestialBody,
            CelestialBodyOrbital,
            CelestialBodySurface,
            Vessel,
            VesselOrbital,
            VesselSurface,
            Part,
            PartOrbital,
            PartSurface,
            //SpaceObject,
            //SpaceObjectOrbital,
            //SpaceObjectSurface,
            Maneuver
        }

        Type type;
        global::CelestialBody body;
        global::Vessel vessel;
        global::Part part;
        global::ManeuverNode node;

        ReferenceFrame (Type type)
        {
            this.type = type;
        }

        internal static ReferenceFrame Object (global::CelestialBody body)
        {
            var r = new ReferenceFrame (Type.CelestialBody);
            r.body = body;
            return r;
        }

        internal static ReferenceFrame Orbital (global::CelestialBody body)
        {
            if (body == body.referenceBody)
                throw new ArgumentException ("CelestialBody '" + body.name + "' does not orbit anything");
            var r = new ReferenceFrame (Type.CelestialBodyOrbital);
            r.body = body;
            return r;
        }

        internal static ReferenceFrame Surface (global::CelestialBody body)
        {
            if (body == body.referenceBody)
                throw new ArgumentException ("CelestialBody '" + body.name + "' does not orbit anything");
            var r = new ReferenceFrame (Type.CelestialBodySurface);
            r.body = body;
            return r;
        }

        internal static ReferenceFrame Object (global::Vessel vessel)
        {
            var r = new ReferenceFrame (Type.Vessel);
            r.vessel = vessel;
            return r;
        }

        internal static ReferenceFrame Orbital (global::Vessel vessel)
        {
            var r = new ReferenceFrame (Type.VesselOrbital);
            r.vessel = vessel;
            return r;
        }

        internal static ReferenceFrame Surface (global::Vessel vessel)
        {
            var r = new ReferenceFrame (Type.VesselSurface);
            r.vessel = vessel;
            return r;
        }

        internal static ReferenceFrame Object (global::Part part)
        {
            var r = new ReferenceFrame (Type.Part);
            r.part = part;
            return r;
        }

        internal static ReferenceFrame Orbital (global::Part part)
        {
            var r = new ReferenceFrame (Type.PartOrbital);
            r.part = part;
            return r;
        }

        internal static ReferenceFrame Surface (global::Part part)
        {
            var r = new ReferenceFrame (Type.PartSurface);
            r.part = part;
            return r;
        }

        internal static ReferenceFrame Object (global::ManeuverNode node)
        {
            var r = new ReferenceFrame (Type.Maneuver);
            r.node = node;
            return r;
        }

        /// <summary>
        /// Returns the position of the origin of the reference frame in world-space.
        /// </summary>
        public Vector3d Position {
            get {
                switch (type) {
                case Type.CelestialBody:
                case Type.CelestialBodyOrbital:
                case Type.CelestialBodySurface:
                    return body.position;
                case Type.Vessel:
                case Type.VesselOrbital:
                case Type.VesselSurface:
                    return vessel.GetWorldPos3D ();
                case Type.Part:
                case Type.PartOrbital:
                case Type.PartSurface:
                    return part.vessel.GetWorldPos3D () + part.CoMOffset;
                case Type.Maneuver:
                    return node.patch.getPositionAtUT (node.UT);
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
                case Type.CelestialBodySurface:
                    {
                        if (body != body.referenceBody) {
                            // Body orbits something
                            return body.GetOrbit ().GetVel ();
                        } else {
                            // Body does not orbit anything
                            // Get a body that orbits the sun
                            var orbitingBody = FlightGlobals.Bodies.Find (b => b.name != "Sun" && b.GetOrbit ().referenceBody == body);
                            var orbit = orbitingBody.GetOrbit ();
                            // Compute the velocity of the sun in world space from this body
                            // Can't be done for from the sun object as it has no orbit object
                            return orbit.GetVel () - orbit.GetRelativeVel ();
                        }
                    }
                case Type.Vessel:
                    return vessel.GetOrbit ().GetVel ();
                case Type.VesselSurface:
                    return vessel.GetOrbit ().GetVel () - ((Vector3d)vessel.GetSrfVelocity ());
                case Type.OrbitalBody:
                    return body.GetOrbit ().GetVel ();
                case Type.OrbitalVessel:
                    return vessel.GetOrbit ().GetVel ();
                case Type.Maneuver:
                    return node.patch.GetVel ();
                case Type.Part:
                    throw new NotImplementedException ();
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
                Vector3d forward = ForwardNotNormalized;
                // Note: forward is along the z-axis, up is along the negative y-axis
                Vector3d up = -UpNotNormalized;
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
        /// Returns the up vector for the reference frame in world coordinates.
        /// The direction in which the y-axis points.
        /// The vector is not normalized.
        /// </summary>
        Vector3d UpNotNormalized {
            get {
                switch (type) {
                case Type.CelestialBody:
                case Type.CelestialBodySurface:
                    // The axis of rotation of the body
                    return body.bodyTransform.up;
                case Type.Vessel:
                    return vessel.upAxis;
                case Type.VesselSurface:
                    return ((Vector3d)vessel.CoM) - vessel.mainBody.position;
                case Type.OrbitalBody:
                    if (body.name == "Sun")
                        throw new NotImplementedException ();
                    return body.position - body.referenceBody.position;
                case Type.OrbitalVessel:
                    return ((Vector3d)vessel.CoM) - vessel.mainBody.position;
                case Type.Maneuver:
                    return node.patch.GetOrbitNormal ();
                case Type.Part:
                    throw new NotImplementedException ();
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Returns the forward vector of the reference frame in world coordinates.
        /// The direction in which the z axis points.
        /// The vector is not normalized.
        /// </summary>
        Vector3d ForwardNotNormalized {
            get {
                switch (type) {
                case Type.CelestialBody:
                    return body.bodyTransform.up;
                case Type.CelestialBodySurface:
                    {
                        throw new NotImplementedException ();
                    }
                case Type.Vessel:
                    return vessel.upAxis;
                case Type.VesselSurface:
                    {
                        var exclude = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - ((Vector3d)vessel.CoM);
                        return Vector3d.Exclude (Up, exclude);
                    }
                case Type.OrbitalBody:
                    {
                        // TODO: is this correct?
                        var exclude = body.referenceBody.position + ((Vector3d)body.referenceBody.transform.up) * body.referenceBody.Radius - body.position;
                        return Vector3d.Exclude (Up, exclude);
                    }
                case Type.OrbitalVessel:
                    {
                        var exclude = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - ((Vector3d)vessel.CoM);
                        return Vector3d.Exclude (Up, exclude);
                    }
                case Type.Maneuver:
                    return node.patch.getOrbitalVelocityAtUT (node.UT);
                case Type.Part:
                    throw new NotImplementedException ();
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Convert the given position in world space, to a position in this reference frame.
        /// </summary>
        public Vector3d PositionFromWorldSpace (Vector3d worldPosition)
        {
            return worldPosition - Position;
        }

        /// <summary>
        /// Convert the given position in this reference frame, to a position in world space.
        /// </summary>
        public Vector3d PositionToWorldSpace (Vector3d position)
        {
            return Position + position;
        }

        /// <summary>
        /// Convert the given velocity in world space, to a velocity in this reference frame.
        /// </summary>
        public Vector3d VelocityFromWorldSpace (Vector3d worldVelocity)
        {
            return worldVelocity + Velocity;
        }

        /// <summary>
        /// Convert the given velocity in this reference frame, to a velocity in world space.
        /// </summary>
        public Vector3d VelocityToWorldSpace (Vector3d velocity)
        {
            return velocity - Velocity;
        }
    }
}

