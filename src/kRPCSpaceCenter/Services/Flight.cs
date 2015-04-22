using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.Service;
using KRPCSpaceCenter.ExternalAPI;
using UnityEngine;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;
using Tuple4 = KRPC.Utils.Tuple<double,double,double,double>;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Flight : Equatable<Flight>
    {
        readonly global::Vessel vessel;
        readonly ReferenceFrame referenceFrame;

        internal Flight (global::Vessel vessel, ReferenceFrame referenceFrame)
        {
            this.vessel = vessel;
            this.referenceFrame = referenceFrame;
        }

        public override bool Equals (Flight obj)
        {
            return vessel == obj.vessel && referenceFrame == obj.referenceFrame;
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
            get { return vessel.ReferenceTransform.up; }
        }

        /// <summary>
        /// Rotation of the vessel in the given reference frame.
        /// Rotation * Vector3d.up gives the direction vector in which the vessel points, in reference frame space.
        /// </summary>
        QuaternionD VesselRotation {
            get { return referenceFrame.RotationFromWorldSpace (vessel.ReferenceTransform.rotation); }
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
                return vessel.GetOrbit ().GetOrbitNormal ().SwapYZ ().normalized;
            }
        }

        /// <summary>
        /// Orbit radial direction in world space
        /// </summary>
        Vector3d WorldRadial {
            get { return Vector3d.Cross (WorldNormal, WorldPrograde); }
        }

        /// <summary>
        /// Check that FAR is installed and that it is active for the vessel
        /// </summary>
        void CheckFAR ()
        {
            if (!FAR.IsAvailable)
                throw new InvalidOperationException ("FAR is not available");
            if (!FAR.ActiveControlSysIsOnVessel (this.vessel))
                throw new InvalidOperationException ("FAR is not active on this vessel");
        }

        [KRPCProperty]
        public float GForce {
            get { return (float) vessel.geeForce; }
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
            get { return MeanAltitude - Elevation; }
        }

        [KRPCProperty]
        public double Elevation {
            get { return vessel.terrainAltitude; }
        }

        [KRPCProperty]
        public double Latitude {
            get { return vessel.mainBody.GetLatitude (WorldCoM); }
        }

        [KRPCProperty]
        public double Longitude {
            get { return GeometryExtensions.ClampAngle180 (vessel.mainBody.GetLongitude (WorldCoM)); }
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
            get {
                var velocity = referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity);
                var up = referenceFrame.DirectionFromWorldSpace ((WorldCoM - vessel.orbit.referenceBody.position).normalized);
                return Vector3d.Dot (velocity, up);
            }
        }

        [KRPCProperty]
        public Tuple3 CenterOfMass {
            get { return referenceFrame.PositionFromWorldSpace (WorldCoM).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple4 Rotation {
            get { return VesselRotation.ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 Direction {
            get { return referenceFrame.DirectionFromWorldSpace (WorldDirection).normalized.ToTuple (); }
        }

        [KRPCProperty]
        public float Pitch {
            get { return (float) VesselRotation.PitchHeadingRoll ().x; }
        }

        [KRPCProperty]
        public float Heading {
            get { return (float) VesselRotation.PitchHeadingRoll ().y; }
        }

        [KRPCProperty]
        public float Roll {
            get { return (float) VesselRotation.PitchHeadingRoll ().z; }
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
        public Tuple3 AntiNormal {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldNormal).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 Radial {
            get { return referenceFrame.DirectionFromWorldSpace (WorldRadial).ToTuple (); }
        }

        [KRPCProperty]
        public Tuple3 AntiRadial {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldRadial).ToTuple (); }
        }

        [KRPCProperty]
        public float AtmosphereDensity {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float) FAR.GetActiveControlSys_AirDensity ();
                } else {
                    return new CelestialBody (vessel.mainBody).AtmosphereDensityAt ((float) MeanAltitude);
                }
            }
        }

        [KRPCProperty]
        public float Drag {
            get {
                if (FAR.IsAvailable)
                    CheckFAR ();
                var body = new CelestialBody (this.vessel.mainBody);
                if (!body.HasAtmosphere)
                    return 0f;
                var vessel = new Vessel (this.vessel);
                return 0.5f * AtmosphereDensity * (float) Math.Pow (Speed, 2f) * DragCoefficient * vessel.CrossSectionalArea;
            }
        }

        [KRPCProperty]
        public float DynamicPressure {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_Q ();
            }
        }

        [KRPCProperty]
        public float AngleOfAttack {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_AoA ();
            }
        }

        [KRPCProperty]
        public float SideslipAngle {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_Sideslip ();
            }
        }

        [KRPCProperty]
        public float StallFraction {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_StallFrac ();
            }
        }

        [KRPCProperty]
        public float MachNumber {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_MachNumber ();
            }
        }

        [KRPCProperty]
        public float TerminalVelocity {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_TermVel ();
            }
        }

        [KRPCProperty]
        public float DragCoefficient {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float) FAR.GetActiveControlSys_Cd ();
                } else {
                    // Mass-weighted average of max_drag for each part
                    // Note: Uses Part.mass, so does not include the mass of resources
                    return vessel.Parts.Sum (p => p.maximum_drag * p.mass) / vessel.Parts.Sum (p => p.mass);
                }
            }
        }

        [KRPCProperty]
        public float LiftCoefficient {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_Cl ();
            }
        }

        [KRPCProperty]
        public float PitchingMomentCoefficient {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_Cm ();
            }
        }

        [KRPCProperty]
        public float BallisticCoefficient {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_BallisticCoeff ();
            }
        }

        [KRPCProperty]
        public float ThrustSpecificFuelConsumption {
            get {
                CheckFAR ();
                return (float) FAR.GetActiveControlSys_TSFC ();
            }
        }

        [KRPCProperty]
        public string FARStatus {
            get {
                CheckFAR ();
                return FAR.GetActiveControlSys_StatusMessage ();
            }
        }
    }
}
