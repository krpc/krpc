using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;

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
        /// Velocity of the vessel in world space
        /// </summary>
        Vector3d WorldVelocity {
            get { return vessel.GetOrbit ().GetVel (); }
        }

        /// <summary>
        /// Position of the vessels center of mass in world space
        /// </summary>
        Vector3d WorldCoM {
            get { return vessel.findWorldCenterOfMass (); }
        }

        /// <summary>
        /// Direction the vessel is pointing in in world space
        /// </summary>
        Vector3d WorldDirection {
            get { return vessel.GetTransform ().up; }
        }

        /// <summary>
        /// Rotation of the vessel in the given reference frame
        /// E.g. in the surface reference frame, is a rotation from the vessel direction vector
        /// (in surface-space coordinates) to the surface space basis vector
        /// </summary>
        QuaternionD Rotation {
            get { return referenceFrame.RotationFromWorldSpace (vessel.GetTransform ().rotation); }
        }

        /// <summary>
        /// Orbit prograde direction in world space
        /// </summary>
        Vector3d WorldPrograde {
            get { return vessel.GetOrbit ().GetVel ().normalized; }
        }

        /// <summary>
        /// Orbit normal direction in world space
        /// </summary>
        Vector3d WorldNormal {
            get {
                // Note: y and z components of normal vector are swapped
                var normal = vessel.GetOrbit ().GetOrbitNormal ();
                var tmp = normal.y;
                normal.y = normal.z;
                normal.z = tmp;
                return normal.normalized;
            }
        }

        /// <summary>
        /// Orbit radial direction in world space
        /// </summary>
        Vector3d WorldRadial {
            get { return Vector3d.Cross (WorldNormal, WorldPrograde); }
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
        public Tuple3 Velocity {
            get { return referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity).ToTuple (); }
        }

        [KRPCProperty]
        public double Speed {
            get { return referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity).magnitude; }
        }

        [KRPCProperty]
        public double HorizontalSpeed {
            get { return Speed - Math.Abs (VerticalSpeed); }
        }

        [KRPCProperty]
        public double VerticalSpeed {
            get { return Vector3d.Dot (referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity), Vector3d.up); }
        }

        [KRPCProperty]
        public Tuple3 CenterOfMass {
            get { return referenceFrame.PositionFromWorldSpace (WorldCoM).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 Direction {
            get { return referenceFrame.DirectionFromWorldSpace (WorldDirection).ToTuple (); }
        }

        [KRPCProperty]
        public double Pitch {
            get { return Rotation.PitchHeadingRoll ().x; }
        }

        [KRPCProperty]
        public double Heading {
            get { return Rotation.PitchHeadingRoll ().y; }
        }

        [KRPCProperty]
        public double Roll {
            get { return Rotation.PitchHeadingRoll ().z; }
        }

        [KRPCProperty]
        public Tuple3 Prograde {
            get { return referenceFrame.DirectionFromWorldSpace (WorldPrograde).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 Retrograde {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldPrograde).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 Normal {
            get { return referenceFrame.DirectionFromWorldSpace (WorldNormal).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 NormalNeg {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldNormal).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 Radial {
            get { return referenceFrame.DirectionFromWorldSpace (WorldRadial).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 RadialNeg {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldRadial).ToTuple (); }
        }

        [KRPCProperty]
        public double Drag {
            get {
                var body = new CelestialBody (this.vessel.mainBody);
                if (!body.HasAtmosphere)
                    return 0d;
                var vessel = new Vessel (this.vessel);
                return 0.5d * body.AtmosphereDensityAt (MeanAltitude) * Math.Pow (Speed, 2d) * vessel.DragCoefficient * vessel.CrossSectionalArea;
            }
        }
    }
}