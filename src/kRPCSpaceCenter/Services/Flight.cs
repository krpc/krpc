using System;
using KRPC.Service.Attributes;
using KRPC.Schema.Geometry;
using UnityEngine;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
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

        /// <summary>
        /// Velocity of the vessel in world-space.
        /// </summary>
        Vector3d GetVelocity ()
        {
            return vessel.GetOrbit ().GetVel ();
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
            return ReferenceFrameTransform.GetUp (referenceFrame, vessel);
        }

        /// <summary>
        /// Direction to the north of the main body (0 degrees pitch, north direction on nav ball) in world coordinates
        /// </summary>
        Vector3d GetNorthDirection ()
        {
            return ReferenceFrameTransform.GetForward (referenceFrame, vessel);
        }

        /// <summary>
        /// Rotation of the vessel in the given reference frame
        /// E.g. in the surface reference frame, is a rotation from the vessel direction vector
        /// (in surface-space coordinates) to the surface space basis vector
        /// </summary>
        Quaternion GetRotation ()
        {
            return ReferenceFrameTransform.GetRotation (referenceFrame, vessel).Inverse () * vessel.GetTransform ().rotation;
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
            get {
                var rotation = ReferenceFrameTransform.GetRotation (referenceFrame, vessel);
                var velocity = ReferenceFrameTransform.GetVelocity (referenceFrame, vessel);
                return (rotation.Inverse () * (GetVelocity () - velocity)).ToMessage ();
            }
        }

        [KRPCProperty]
        public double Speed {
            get {
                var velocity = ReferenceFrameTransform.GetVelocity (referenceFrame, vessel);
                return (GetVelocity () - velocity).magnitude;
            }
        }

        [KRPCProperty]
        public double HorizontalSpeed {
            get {
                return Speed - VerticalSpeed;
            }
        }

        [KRPCProperty]
        public double VerticalSpeed {
            get {
                var velocity = ReferenceFrameTransform.GetVelocity (referenceFrame, vessel);
                return Vector3d.Dot (GetVelocity () - velocity, GetUpDirection ());
            }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 CenterOfMass {
            get { return GetPosition ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Direction {
            get { return (ReferenceFrameTransform.GetRotation (referenceFrame, vessel).Inverse () * GetDirection ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 UpDirection {
            get { return (ReferenceFrameTransform.GetRotation (referenceFrame, vessel).Inverse () * GetUpDirection ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NorthDirection {
            get { return (ReferenceFrameTransform.GetRotation (referenceFrame, vessel).Inverse () * GetNorthDirection ()).ToMessage (); }
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
