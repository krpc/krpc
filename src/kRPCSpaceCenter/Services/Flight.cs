using System;
using KRPC.Service.Attributes;
using KRPC.Schema.Geometry;
using KRPC.Utils;
using UnityEngine;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Flight : Equatable<Flight>
    {
        global::Vessel vessel;
        ReferenceFrame referenceFrame;

        internal Flight (global::Vessel vessel, ReferenceFrame referenceFrame)
        {
            this.vessel = vessel;
            this.referenceFrame = referenceFrame;
        }

        public override bool Equals (Flight other)
        {
            return vessel == other.vessel && referenceFrame == other.referenceFrame;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode () ^ referenceFrame.GetHashCode ();
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
        public double GForce {
            get { return vessel.geeForce; }
        }

        [KRPCProperty]
        public double MeanAltitude {
            get { return vessel.mainBody.GetAltitude (vessel.CoM); }
        }

        [KRPCProperty]
        public double SurfaceAltitude {
            get { return Math.Min (BedrockAltitude, MeanAltitude); }
        }

        [KRPCProperty]
        public double BedrockAltitude {
            get { return MeanAltitude - vessel.terrainAltitude; }
        }

        [KRPCProperty]
        public double Elevation {
            get { return -vessel.terrainAltitude; }
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

        Vector3d GetPrograde ()
        {
            var rotation = ReferenceFrameTransform.GetRotation (referenceFrame, vessel);
            var velocity = ReferenceFrameTransform.GetVelocity (referenceFrame, vessel);
            return (rotation.Inverse () * (GetVelocity () - velocity)).normalized;
        }

        Vector3d GetNormal ()
        {
            var rotation = ReferenceFrameTransform.GetRotation (referenceFrame, vessel);
            var velocity = ReferenceFrameTransform.GetVelocity (referenceFrame, vessel);
            var normal = vessel.GetOrbit ().GetOrbitNormal ();
            var tmp = normal.y;
            normal.y = normal.z;
            normal.z = tmp;
            return (rotation.Inverse () * (normal - velocity)).normalized;
        }

        Vector3d GetRadial ()
        {
            return Vector3d.Cross (GetNormal (), GetPrograde ()).normalized;
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Prograde {
            get { return GetPrograde ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Retrograde {
            get { return (-GetPrograde ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Normal {
            get { return GetNormal ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NormalNeg {
            get { return (-GetNormal ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Radial {
            get { return GetRadial ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 RadialNeg {
            get { return (-GetRadial ()).ToMessage (); }
        }
    }
}
