using KRPC.Service.Attributes;
using UnityEngine;
using System;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class ReferenceFrame
    {
        public enum Type
        {
            CelestialBody,
            CelestialBodySurface,
            Vessel,
            Orbital,
            Part,
            Maneuver
        }

        Type type;
        global::CelestialBody body;
        global::Vessel vessel;

        public ReferenceFrame (Type type, global::CelestialBody body)
        {
            this.type = type;
            this.body = body;
            this.vessel = null;
            if (type != Type.CelestialBody && type != Type.Orbital)
                throw new ArgumentException ("Incorrect data for reference frame type. Got " + type + " with CelestialBody data.");
        }

        public ReferenceFrame (Type type, global::Vessel vessel)
        {
            this.type = type;
            this.vessel = vessel;
            this.body = vessel.mainBody;
            if (type != Type.Vessel && type != Type.Orbital)
                throw new ArgumentException ("Incorrect data for reference frame type. Got " + type + " with Vessel data.");
        }

        public ReferenceFrame (Type type, global::CelestialBody body, global::Vessel vessel)
        {
            this.type = type;
            this.body = body;
            this.vessel = vessel;
            if (type != Type.CelestialBodySurface)
                throw new ArgumentException ("Incorrect data for reference frame type. Got " + type + " with CelestialBody and Vessel data.");
        }

        /// <summary>
        /// Returns the up vector for the reference frame in world coordinates.
        /// The vector is not normalized.
        /// </summary>
        Vector3d UpNotNormalized {
            get {
                switch (type) {
                case Type.CelestialBody:
                    // The axis of rotation of the body
                    return body.bodyTransform.up;
                case Type.CelestialBodySurface:
                    return ((Vector3d)vessel.CoM) - body.position;
                case Type.Vessel:
                    return vessel.upAxis;
                case Type.Orbital:
                    if (vessel != null)
                        return ((Vector3d)vessel.CoM) - vessel.mainBody.position;
                    else
                        throw new NotImplementedException ();
                case Type.Maneuver:
                case Type.Part:
                    throw new NotImplementedException ();
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Returns the forward vector of the reference frame in world coordinates.
        /// The vector is not normalized.
        /// </summary>
        Vector3d ForwardNotNormalized {
            get {
                switch (type) {
                case Type.CelestialBody:
                    return body.bodyTransform.up;
                case Type.Vessel:
                    return vessel.upAxis;
                case Type.CelestialBodySurface:
                    {
                        if (body != vessel.mainBody)
                            throw new ArgumentException ("Vessel is in orbit around another body");
                        var exclude = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - ((Vector3d)vessel.CoM);
                        return Vector3d.Exclude (Up, exclude);
                    }
                case Type.Orbital:
                    {
                        if (vessel != null) {
                            var exclude = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - ((Vector3d)vessel.CoM);
                            return Vector3d.Exclude (Up, exclude);
                        } else
                            throw new NotImplementedException ();
                    }
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Returns the up vector of the reference frame in world coordinates.
        /// </summary>
        public Vector3d Up {
            get { return UpNotNormalized.normalized; }
        }

        /// <summary>
        /// Returns the forward vector of the reference frame in world coordinates.
        /// </summary>
        public Vector3d Forward {
            get { return ForwardNotNormalized.normalized; }
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
                //FIXME: Vector3d.OrthoNormalize and QuaternionD.LookRotation methods are not found at run-time
                //Vector3d.OrthoNormalize (ref forward, ref up);
                //return QuaternionD.LookRotation (forward, up);
                Vector3 forward2 = forward;
                Vector3 up2 = up;
                Vector3.OrthoNormalize (ref forward2, ref up2);
                return Quaternion.LookRotation (forward2, up2);
            }
        }

        /// <summary>
        /// Returns the velocity of the reference frame in world-space.
        /// </summary>
        public Vector3d Velocity {
            get {
                switch (type) {
                case Type.CelestialBody:
                    {// TODO: better way to check for orbits?
                        if (body.name != "Sun")
                            return body.GetOrbit ().GetVel ();
                        else
                        //TODO: The sun moves in world-space. How do we get this velocity?
                        throw new NotImplementedException ();
                    }
                case Type.CelestialBodySurface:
                    {
                        if (vessel.mainBody != body)
                            throw new ArgumentException ("Vessel is in orbit around a different body");
                        return ((Vector3d)vessel.GetObtVelocity ()) - ((Vector3d)vessel.GetSrfVelocity ());
                    }
                case Type.Vessel:
                    return vessel.GetOrbit ().GetVel ();
                case Type.Orbital:
                    // TODO: is this correct? (relative to world space)
                    return Vector3d.zero;
                default:
                    throw new ArgumentException ("No such reference frame");
                }
            }
        }

        /// <summary>
        /// Returns the position of the origin of the reference frame in world-space.
        /// </summary>
        public Vector3d Position {
            get {
                switch (type) {
                case Type.CelestialBody:
                    return body.position;
                case Type.Vessel:
                    return vessel.GetWorldPos3D ();
                case Type.CelestialBodySurface:
                case Type.Orbital:
                case Type.Maneuver:
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

