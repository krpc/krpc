using System;
using KRPC.Service.Attributes;
using KRPC.Schema.Geometry;
using UnityEngine;
using KRPCServices.ExtensionMethods;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Flight
    {
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
                return vessel.srf_velocity;
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
        /// Direction the vessel is pointing in in world coordinates
        /// </summary>
        Vector3d GetDirection ()
        {
            return vessel.GetTransform ().up;
        }

        /// <summary>
        /// Direction normal to the main body in world coordinates
        /// </summary>
        Vector3d GetUpDirection ()
        {
            return ReferenceFrameRotation.GetUp (referenceFrame, vessel);
        }

        /// <summary>
        /// Direction to the north of the main body (0 degrees pitch, north direction on nav ball) in world coordinates
        /// </summary>
        Vector3d GetNorthDirection ()
        {
            return ReferenceFrameRotation.GetForward (referenceFrame, vessel);
        }

        /// <summary>
        /// Rotation of the vessel in the given reference frame
        /// E.g. in the surface reference frame, is a rotation from the vessel direction vector
        /// (in surface-space coordinates) to the surface space basis vector
        /// </summary>
        Quaternion GetRotation ()
        {
            return ReferenceFrameRotation.GetRotation (referenceFrame, vessel).Inverse () * vessel.GetTransform ().rotation;
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
            // TODO: convert to a reference frame?
            get { return GetVelocity ().ToMessage (); }
        }

        [KRPCProperty]
        public double Speed {
            get { return GetVelocity ().magnitude; }
        }

        [KRPCProperty]
        public double HorizontalSpeed {
            get {
                if (referenceFrame == ReferenceFrame.Surface)
                    return vessel.horizontalSrfSpeed;
                else
                    return Vector3d.Exclude (GetUpDirection (), GetVelocity ()).magnitude;
            }
        }

        [KRPCProperty]
        public double VerticalSpeed {
            get {
                if (referenceFrame == ReferenceFrame.Surface)
                    return vessel.verticalSpeed;
                else
                    return Vector3d.Dot (GetVelocity (), GetUpDirection ());
            }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 CenterOfMass {
            // TODO: convert to a reference frame?
            get { return GetPosition ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Direction {
            get { return (ReferenceFrameRotation.GetRotation (referenceFrame, vessel).Inverse () * GetDirection ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 UpDirection {
            get { return (ReferenceFrameRotation.GetRotation (referenceFrame, vessel).Inverse () * GetUpDirection ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NorthDirection {
            get { return (ReferenceFrameRotation.GetRotation (referenceFrame, vessel).Inverse () * GetNorthDirection ()).ToMessage (); }
        }

        [KRPCProperty]
        public float Pitch {
            get { return GetRotation ().PitchHeadingRoll ().x; }
        }

        [KRPCProperty]
        public float Heading {
            get { return GetRotation ().PitchHeadingRoll ().y; }
        }

        [KRPCProperty]
        public float Roll {
            get { return GetRotation ().PitchHeadingRoll ().z; }
        }
    }
}
