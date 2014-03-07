using UnityEngine;

namespace KRPCServices
{
    /// <summary>
    /// Tracks data for a given vessel
    /// </summary>
    class VesselData
    {
        Vessel vessel;

        public VesselData (Vessel vessel)
        {
            this.vessel = vessel;
        }

        public void Update ()
        {
        }

        public double Altitude {
            get { return vessel.GetOrbit ().altitude; }
        }

        /// <summary>
        /// Rotation of the vessel relative to the main body
        /// </summary>
        public Quaternion Rotation {
            get {
                // Position of the vessel
                var vesselPosition = vessel.findWorldCenterOfMass ();
                // Direction from the center of the main body to the vessel
                var upDirection = (vesselPosition - vessel.mainBody.position).normalized;
                // Position of the north pole of the main body
                var northPole = vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius;
                // Vector from vessel to the north pole
                var toNorthPole = northPole - vesselPosition;
                // Direction from the vessel to the north pole, perpendicular to the surface of the main body
                var north = (toNorthPole - Vector3d.Project (toNorthPole, upDirection)).normalized;
                // Rotation of the vessel w.r.t. north
                var rotation = Quaternion.LookRotation (north, upDirection);
                return Quaternion.Inverse (Quaternion.Euler (90f, 0f, 0f) * Quaternion.Inverse (vessel.GetTransform ().rotation) * rotation);
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
    }
}
