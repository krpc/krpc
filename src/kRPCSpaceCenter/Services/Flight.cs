using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using KRPCSpaceCenter.ExternalAPI;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

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
        /// Sum of the lift force acting on each part.
        /// Note this is NOT the force in the vessel's lift direction.
        /// </summary>
        Vector3d WorldPartsLift {
            get {
                Vector3d lift = Vector3d.zero;
                foreach (var part in vessel.Parts) {
                    if (!part.hasLiftModule) {
                        Vector3 bodyLift = part.transform.rotation * (part.bodyLiftScalar * part.DragCubes.LiftForce);
                        bodyLift = Vector3.ProjectOnPlane (bodyLift, -part.dragVectorDir);
                        lift += bodyLift;
                    }
                    foreach (var module in part.Modules) {
                        var wing = module as ModuleLiftingSurface;
                        if (wing != null)
                            lift += wing.liftForce;
                    }
                }
                return lift;
            }
        }

        /// <summary>
        /// Sum of the drag force acting on each part.
        /// Note this is NOT the force in the vessel's drag direction.
        /// </summary>
        Vector3d WorldPartsDrag {
            get {
                Vector3d drag = Vector3d.zero;
                foreach (var part in vessel.Parts) {
                    // Part drag
                    drag += -part.dragVectorDir * part.dragScalar;
                    // Lifting surface drag
                    foreach (var module in part.Modules) {
                        var wing = module as ModuleLiftingSurface;
                        if (wing != null)
                            drag += wing.dragForce;
                    }
                }
                return drag;
            }
        }

        /// <summary>
        /// Total aerodynamic forces acting on vessel in world space.
        /// </summary>
        Vector3d WorldAerodynamicForce {
            get { return WorldPartsLift + WorldPartsDrag; }
        }

        /// <summary>
        /// Total lift force acting on vessel (perpendicular to air stream and up wrt roll angle) in world space.
        /// </summary>
        Vector3d WorldLift {
            get {
                Vector3d direction = -Vector3d.Cross (vessel.transform.right, vessel.srf_velocity.normalized);
                return Vector3d.Dot (WorldAerodynamicForce, direction) * direction;
            }
        }

        /// <summary>
        /// Total drag force acting on vessel (in opposite direction to air stream) in world space.
        /// </summary>
        Vector3d WorldDrag {
            get {
                Vector3d direction = -vessel.srf_velocity.normalized;
                return Vector3d.Dot (WorldAerodynamicForce, direction) * direction;
            }
        }

        /// <summary>
        /// Check that FAR is installed and that it is active for the vessel
        /// </summary>
        void CheckFAR ()
        {
            if (!FAR.IsAvailable)
                throw new InvalidOperationException ("FAR is not available");
            if (!FAR.ActiveControlSysIsOnVessel (vessel))
                throw new InvalidOperationException ("FAR is not active on this vessel");
        }

        /// <summary>
        /// Check that FAR is not installed
        /// </summary>
        static void CheckNoFAR ()
        {
            if (FAR.IsAvailable)
                throw new InvalidOperationException ("Not available; FAR is installed");
        }

        [KRPCProperty]
        public float GForce {
            get { return (float)vessel.geeForce; }
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
            get { return (float)VesselRotation.PitchHeadingRoll ().x; }
        }

        [KRPCProperty]
        public float Heading {
            get { return (float)VesselRotation.PitchHeadingRoll ().y; }
        }

        [KRPCProperty]
        public float Roll {
            get { return (float)VesselRotation.PitchHeadingRoll ().z; }
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
                    return (float)FAR.GetActiveControlSys_AirDensity ();
                } else {
                    return (float)vessel.atmDensity;
                }
            }
        }

        [KRPCProperty]
        public float DynamicPressure {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.GetActiveControlSys_Q ();
                } else {
                    return (float)(0.5f * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude);
                }
            }
        }

        [KRPCProperty]
        public float StaticPressure {
            get {
                CheckNoFAR ();
                return (float)vessel.staticPressurekPa * 1000f;
            }
        }

        [KRPCProperty]
        public Tuple3 AerodynamicForce {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldAerodynamicForce).ToTuple ();
            }
        }

        [KRPCProperty]
        public Tuple3 Lift {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldLift).ToTuple ();
            }
        }

        [KRPCProperty]
        public Tuple3 Drag {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldDrag).ToTuple ();
            }
        }

        [KRPCProperty]
        public float SpeedOfSound {
            get {
                CheckNoFAR ();
                return (float)vessel.speedOfSound;
            }
        }

        [KRPCProperty]
        public float Mach {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.GetActiveControlSys_MachNumber ();
                } else {
                    return (float)vessel.rootPart.machNumber;
                }
            }
        }

        [KRPCProperty]
        public float EquivalentAirSpeed {
            get {
                CheckNoFAR ();
                return (float)Math.Sqrt (vessel.srf_velocity.sqrMagnitude * vessel.atmDensity / 1.225d);
            }
        }

        [KRPCProperty]
        public float TerminalVelocity {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.GetActiveControlSys_TermVel ();
                } else {
                    //FIXME: this is an estimate
                    var gravity = Math.Sqrt (vessel.GetTotalMass () * FlightGlobals.getGeeForceAtPosition (vessel.CoM).magnitude);
                    return (float)(Math.Sqrt (gravity / WorldDrag.magnitude) * vessel.speed);
                }
            }
        }

        [KRPCProperty]
        public float AngleOfAttack {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.GetActiveControlSys_AoA ();
                } else {
                    return (float)(Vector3d.Dot (vessel.transform.forward, vessel.srf_velocity.normalized) * (180d / Math.PI));
                }
            }
        }

        [KRPCProperty]
        public float SideslipAngle {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.GetActiveControlSys_Sideslip ();
                } else {
                    return (float)(Vector3d.Dot (vessel.transform.up, Vector3d.Exclude (vessel.transform.forward, vessel.srf_velocity.normalized).normalized) * (180d / Math.PI));
                }
            }
        }

        [KRPCProperty]
        public float TotalAirTemperature {
            get { return (float)vessel.externalTemperature; }
        }

        [KRPCProperty]
        public float StaticAirTemperature {
            get { return (float)vessel.atmosphericTemperature; }
        }

        [KRPCProperty]
        public float StallFraction {
            get {
                CheckFAR ();
                return (float)FAR.GetActiveControlSys_StallFrac ();
            }
        }

        [KRPCProperty]
        public float DragCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.GetActiveControlSys_Cd ();
            }
        }

        [KRPCProperty]
        public float LiftCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.GetActiveControlSys_Cl ();
            }
        }

        [KRPCProperty]
        public float PitchingMomentCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.GetActiveControlSys_Cm ();
            }
        }

        [KRPCProperty]
        public float BallisticCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.GetActiveControlSys_BallisticCoeff ();
            }
        }

        [KRPCProperty]
        public float ThrustSpecificFuelConsumption {
            get {
                CheckFAR ();
                return (float)FAR.GetActiveControlSys_TSFC ();
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
