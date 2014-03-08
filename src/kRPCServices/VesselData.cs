using UnityEngine;

namespace KRPCServices
{
    /// <summary>
    /// Tracks data for a given vessel
    /// </summary>
    class VesselData
    {
        public Vessel Vessel { get; set; }

        public VesselData (Vessel vessel)
        {
            Vessel = vessel;
        }

        public void Update ()
        {
        }

        /// <summary>
        /// Position of the vessels centre of mass in world coordinates
        /// </summary>
        public Vector3 Position {
            get { return Vessel.findWorldCenterOfMass (); }
        }

        /// <summary>
        /// Direction the vessel is pointing in, in world coordinates
        /// </summary>
        public Vector3 Direction {
            get { return Vessel.GetTransform ().up; }
        }

        /// <summary>
        /// Direction away from the main body, in world coordinates
        /// </summary>
        public Vector3 UpDirection {
            get { return (Position - Vessel.mainBody.position).normalized; }
        }

        /// <summary>
        /// Altitude above sea level in meters
        /// </summary>
        public double Altitude {
            get { return Vessel.GetOrbit ().altitude; }
        }

        /// <summary>
        /// Altitude above the surface in meters
        /// </summary>
        public double TrueAltitude {
            get { return Vessel.mainBody.GetAltitude (Position); }
        }

        /// <summary>
        /// Orbital velocity in m/s and world coordinates
        /// </summary>
        public Vector3 OrbitalVelocity {
            get { return Vessel.GetOrbit ().GetVel (); }
        }

        /// <summary>
        /// Orbital speed in m/s
        /// </summary>
        public double OrbitalSpeed {
            get { return OrbitalVelocity.magnitude; }
        }

        /// <summary>
        /// Velocity of the vessel over the surface, in m/s and world coordinates
        /// </summary>
        Vector3 SurfaceVelocity {
            get {
                var vesselPosition = Vessel.findWorldCenterOfMass ();
                return Vessel.GetOrbit ().GetVel () - Vessel.mainBody.getRFrmVel (vesselPosition);
            }
        }

        /// <summary>
        /// Speed over the surface in m/s. The magnitude of the SurfaceVelocity.
        /// </summary>
        public double SurfaceSpeed {
            get { return SurfaceVelocity.magnitude; }
        }

        /// <summary>
        /// Vertical speed relative to the main body in m/s
        /// </summary>
        public double VerticalSurfaceSpeed {
            get { return Vector3d.Dot (UpDirection, SurfaceVelocity); }
        }

        /// <summary>
        /// Rotation of the vessel relative to the surface of the main body
        /// </summary>
        public Quaternion Rotation {
            get {
                // Position of the north pole of the main body
                var northPole = Vessel.mainBody.position + Vessel.mainBody.transform.up * (float)Vessel.mainBody.Radius;
                // Vector from vessel to the north pole
                var toNorthPole = northPole - Position;
                // Direction from the vessel to the north pole, perpendicular to the surface of the main body
                var north = (toNorthPole - Vector3d.Project (toNorthPole, UpDirection)).normalized;
                // Rotation of the vessel w.r.t. north
                var rotation = Quaternion.LookRotation (north, UpDirection);
                return Quaternion.Inverse (Quaternion.Euler (90f, 0f, 0f) * Quaternion.Inverse (Vessel.GetTransform ().rotation) * rotation);
            }
        }

        /// <summary>
        /// Pitch of the vessel, between -180 and 180 degrees
        /// </summary>
        public double Pitch {
            get {
                var pitch = Rotation.eulerAngles.x;
                return (pitch > 180d) ? (360d - pitch) : -pitch;
            }
        }

        /// <summary>
        /// Heading of the vessel, between 0 and 360 degrees
        /// </summary>
        public double Heading {
            get { return Rotation.eulerAngles.y; }
        }

        /// <summary>
        /// Roll of the vessel, between -180 and 180 degrees
        /// </summary>
        public double Roll {
            get {
                var roll = Rotation.eulerAngles.z;
                return (roll > 180d) ? (roll - 360d) : roll;
            }
        }

        /// <summary>
        /// Prograde direction in world coordinates
        /// </summary>
        public Vector3 Prograde {
            get { return Vessel.GetOrbit ().GetVel ().normalized; }
        }

        /// <summary>
        /// Retrograde direction in world coordinates
        /// </summary>
        public Vector3 Retrograde {
            get { return -Prograde; }
        }

        /// <summary>
        /// Normal+ direction in world coordinates
        /// </summary>
        public Vector3 Normal {
            get { return Vector3.Cross (Prograde, Radial); }
        }

        /// <summary>
        /// Normal- direction in world coordinates
        /// </summary>
        public Vector3 NormalNeg {
            get { return -Normal; }
        }

        /// <summary>
        /// Radial+ direction in world coordinates
        /// </summary>
        public Vector3 Radial {
            get { return Vector3d.Exclude (OrbitalVelocity, UpDirection); }
        }

        /// <summary>
        /// Radial- direction in world coordinates
        /// </summary>
        public Vector3 RadialNeg {
            get { return -Radial; }
        }
    }
}
