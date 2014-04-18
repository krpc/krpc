using System;
using KRPC.Service.Attributes;
using KRPC.Schema.Geometry;
using UnityEngine;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Flight
    {
        public enum ReferenceFrame
        {
            Orbital,
            Surface,
            Target
        }

        global::Vessel vessel;
        ReferenceFrame referenceFrame;

        internal Flight (global::Vessel vessel, ReferenceFrame referenceFrame)
        {
            this.vessel = vessel;
            this.referenceFrame = referenceFrame;
        }

        Vector3d GetVelocity ()
        {
            switch (referenceFrame) {
            case ReferenceFrame.Orbital:
                return vessel.GetOrbit ().GetVel ();
            case ReferenceFrame.Surface:
                return vessel.GetOrbit ().GetVel () - vessel.mainBody.getRFrmVel (vessel.findWorldCenterOfMass ());
            default: //ReferenceFrame.Target:
                throw new NotImplementedException ();
            }
        }

        /// <summary>
        /// Position of the vessel's center of mass in world coordinates.
        /// </summary>
        Vector3d GetPosition ()
        {
            return vessel.findWorldCenterOfMass ();
        }

        /// <summary>
        /// Direction the vessel is pointing in, in world coordinates
        /// </summary>
        Vector3d GetDirection ()
        {
            return vessel.GetTransform ().up;
        }

        /// <summary>
        /// Direction away from the main body, in world coordinates
        /// </summary>
        Vector3d GetUpDirection ()
        {
            return (GetPosition () - vessel.mainBody.position).normalized;
        }

        /// <summary>
        /// Direction to the north of the main body (0 degrees pitch, north direction on nav ball) in world coordinates
        /// </summary>
        Vector3d GetNorthDirection ()
        {
            // Position of the north pole of the main body
            var northPole = vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius;
            // Vector from vessel to the north pole
            var toNorthPole = northPole - GetPosition ();
            // Direction from the vessel to the north pole, perpendicular to the surface of the main body
            return (toNorthPole - Vector3d.Project (toNorthPole, GetUpDirection ())).normalized;
        }

        /// <summary>
        /// Rotation of the vessel relative to the surface of the main body
        /// </summary>
        Quaternion GetRotation ()
        {
            // Rotation of the vessel w.r.t. north
            var rotation = Quaternion.LookRotation (GetNorthDirection (), GetUpDirection ());
            return Quaternion.Inverse (Quaternion.Euler (90f, 0f, 0f) * Quaternion.Inverse (vessel.GetTransform ().rotation) * rotation);
        }

        [KRPCProperty]
        public double Altitude {
            get { return vessel.mainBody.GetAltitude (vessel.CoM); }
        }

        [KRPCProperty]
        public double TrueAltitude {
            get {
                return Altitude - vessel.terrainAltitude;
            }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Velocity {
            get { return Utils.ToVector3 (GetVelocity ()); }
        }

        [KRPCProperty]
        public double Speed {
            get { return GetVelocity ().magnitude; }
        }

        [KRPCProperty]
        public double HorizontalSpeed {
            get { return Speed - VerticalSpeed; }
        }

        [KRPCProperty]
        public double VerticalSpeed {
            get { return Vector3d.Dot (GetVelocity (), GetUpDirection ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 CenterOfMass {
            get { return Utils.ToVector3 (GetPosition ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Direction {
            get { return Utils.ToVector3 (GetDirection ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 UpDirection {
            get { return Utils.ToVector3 (GetUpDirection ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NorthDirection {
            get { return Utils.ToVector3 (GetNorthDirection ()); }
        }

        [KRPCProperty]
        public double Pitch {
            get {
                var pitch = GetRotation ().eulerAngles.x;
                return (pitch > 180d) ? (360d - pitch) : -pitch;
            }
        }

        [KRPCProperty]
        public double Heading {
            get { return GetRotation ().eulerAngles.y; }
        }

        [KRPCProperty]
        public double Roll {
            get {
                var roll = GetRotation ().eulerAngles.z;
                return (roll > 180d) ? (360d - roll) : roll;
            }
        }
    }
}
