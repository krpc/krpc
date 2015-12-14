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
    /// <summary>
    /// Used to get flight telemetry for a vessel, by calling <see cref="Vessel.Flight"/>.
    /// All of the information returned by this class is given in the reference frame
    /// passed to that method.
    /// </summary>
    /// <remarks>
    /// To get orbital information, such as the apoapsis or inclination, see <see cref="Orbit"/>.
    /// </remarks>
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
        }

        /// <summary>
        /// Check that FAR is not installed
        /// </summary>
        static void CheckNoFAR ()
        {
            if (FAR.IsAvailable)
                throw new InvalidOperationException ("Not available; FAR is installed");
        }

        /// <summary>
        /// The current G force acting on the vessel in <math>m/s^2</math>.
        /// </summary>
        [KRPCProperty]
        public float GForce {
            get { return (float)vessel.geeForce; }
        }

        /// <summary>
        /// The altitude above sea level, in meters.
        /// </summary>
        [KRPCProperty]
        public double MeanAltitude {
            get { return vessel.mainBody.GetAltitude (vessel.CoM); }
        }

        /// <summary>
        /// The altitude above the surface of the body or sea level, whichever is closer, in meters.
        /// </summary>
        [KRPCProperty]
        public double SurfaceAltitude {
            get { return Math.Min (BedrockAltitude, MeanAltitude); }
        }

        /// <summary>
        /// The altitude above the surface of the body, in meters. When over water, this is the altitude above the sea floor.
        /// </summary>
        [KRPCProperty]
        public double BedrockAltitude {
            get { return MeanAltitude - Elevation; }
        }

        /// <summary>
        /// The elevation of the terrain under the vessel, in meters. This is the height of the terrain above sea level,
        /// and is negative when the vessel is over the sea.
        /// </summary>
        [KRPCProperty]
        public double Elevation {
            get { return vessel.terrainAltitude; }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Latitude">latitude</a> of the vessel for the body being orbited, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Latitude {
            get { return vessel.mainBody.GetLatitude (WorldCoM); }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Longitude">longitude</a> of the vessel for the body being orbited, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Longitude {
            get { return GeometryExtensions.ClampAngle180 (vessel.mainBody.GetLongitude (WorldCoM)); }
        }

        /// <summary>
        /// The velocity vector of the vessel. The magnitude of the vector is the speed of the vessel in meters per second.
        /// The direction of the vector is the direction of the vessels motion.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Velocity {
            get { return referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity).ToTuple (); }
        }

        /// <summary>
        /// The speed of the vessel in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Speed {
            get { return referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity).magnitude; }
        }

        /// <summary>
        /// The horizontal speed of the vessel in meters per second.
        /// </summary>
        [KRPCProperty]
        public double HorizontalSpeed {
            get {return Math.Sqrt (Math.Pow(Speed,2) - Math.Pow(VerticalSpeed,2));}
        }

        /// <summary>
        /// The vertical speed of the vessel in meters per second.
        /// </summary>s
        [KRPCProperty]
        public double VerticalSpeed {
            get {
                var velocity = referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity);
                var up = referenceFrame.DirectionFromWorldSpace ((WorldCoM - vessel.orbit.referenceBody.position).normalized);
                return Vector3d.Dot (velocity, up);
            }
        }

        /// <summary>
        /// The position of the center of mass of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple3 CenterOfMass {
            get { return referenceFrame.PositionFromWorldSpace (WorldCoM).ToTuple (); }
        }

        /// <summary>
        /// The rotation of the vessel.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return VesselRotation.ToTuple (); }
        }

        /// <summary>
        /// The direction vector that the vessel is pointing in.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Direction {
            get { return referenceFrame.DirectionFromWorldSpace (WorldDirection).normalized.ToTuple (); }
        }

        /// <summary>
        /// The pitch angle of the vessel relative to the horizon, in degrees. A value between -90° and +90°.
        /// </summary>
        [KRPCProperty]
        public float Pitch {
            get { return (float)VesselRotation.PitchHeadingRoll ().x; }
        }

        /// <summary>
        /// The heading angle of the vessel relative to north, in degrees. A value between 0° and 360°.
        /// </summary>
        [KRPCProperty]
        public float Heading {
            get { return (float)VesselRotation.PitchHeadingRoll ().y; }
        }

        /// <summary>
        /// The roll angle of the vessel relative to the horizon, in degrees. A value between -180° and +180°.
        /// </summary>
        [KRPCProperty]
        public float Roll {
            get { return (float)VesselRotation.PitchHeadingRoll ().z; }
        }

        /// <summary>
        /// The unit direction vector pointing in the prograde direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Prograde {
            get { return referenceFrame.DirectionFromWorldSpace (WorldPrograde).ToTuple (); }
        }

        /// <summary>
        /// The unit direction vector pointing in the retrograde direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Retrograde {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldPrograde).ToTuple (); }
        }

        /// <summary>
        /// The unit direction vector pointing in the normal direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Normal {
            get { return referenceFrame.DirectionFromWorldSpace (WorldNormal).ToTuple (); }
        }

        /// <summary>
        /// The unit direction vector pointing in the anti-normal direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 AntiNormal {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldNormal).ToTuple (); }
        }

        /// <summary>
        /// The unit direction vector pointing in the radial direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Radial {
            get { return referenceFrame.DirectionFromWorldSpace (WorldRadial).ToTuple (); }
        }

        /// <summary>
        /// The unit direction vector pointing in the anti-radial direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 AntiRadial {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldRadial).ToTuple (); }
        }

        /// <summary>
        /// The current density of the atmosphere around the vessel, in <math>kg/m^3</math>.
        /// </summary>
        [KRPCProperty]
        public float AtmosphereDensity {
            get {
                return (float)vessel.atmDensity;
            }
        }

        /// <summary>
        /// The dynamic pressure acting on the vessel, in Pascals. This is a measure of the strength of the
        /// aerodynamic forces. It is equal to <math>\frac{1}{2} . \mbox{air density} .  \mbox{velocity}^2</math>.
        /// It is commonly denoted as <math>Q</math>.
        /// </summary>
        /// <remarks>
        /// Calculated using <a href="http://wiki.kerbalspaceprogram.com/wiki/Atmosphere">KSPs stock aerodynamic model</a>, or
        /// <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> if it is installed.
        /// </remarks>
        [KRPCProperty]
        public float DynamicPressure {
            get {
                if (FAR.IsAvailable) {
                    return (float)FAR.VesselDynPres (vessel);
                } else {
                    return (float)(0.5f * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude);
                }
            }
        }

        /// <summary>
        /// The static atmospheric pressure acting on the vessel, in Pascals.
        /// </summary>
        /// <remarks>
        /// Calculated using <a href="http://wiki.kerbalspaceprogram.com/wiki/Atmosphere">KSPs stock aerodynamic model</a>.
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public float StaticPressure {
            get {
                CheckNoFAR ();
                return (float)vessel.staticPressurekPa * 1000f;
            }
        }

        /// <summary>
        /// The total aerodynamic forces acting on the vessel, as a vector pointing in the direction of the force, with its
        /// magnitude equal to the strength of the force in Newtons.
        /// </summary>
        /// <remarks>
        /// Calculated using <a href="http://wiki.kerbalspaceprogram.com/wiki/Atmosphere">KSPs stock aerodynamic model</a>.
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 AerodynamicForce {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldAerodynamicForce).ToTuple ();
            }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Aerodynamic_force">aerodynamic lift</a> currently acting on the vessel,
        /// as a vector pointing in the direction of the force, with its magnitude equal to the strength of the force in Newtons.
        /// </summary>
        /// <remarks>
        /// Calculated using <a href="http://wiki.kerbalspaceprogram.com/wiki/Atmosphere">KSPs stock aerodynamic model</a>.
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 Lift {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldLift).ToTuple ();
            }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Aerodynamic_force">aerodynamic drag</a> currently acting on the vessel,
        /// as a vector pointing in the direction of the force, with its magnitude equal to the strength of the force in Newtons.
        /// </summary>
        /// <remarks>
        /// Calculated using <a href="http://wiki.kerbalspaceprogram.com/wiki/Atmosphere">KSPs stock aerodynamic model</a>.
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 Drag {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldDrag).ToTuple ();
            }
        }

        /// <summary>
        /// The speed of sound, in the atmosphere around the vessel, in <math>m/s</math>.
        /// </summary>
        /// <remarks>
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public float SpeedOfSound {
            get {
                CheckNoFAR ();
                return (float)vessel.speedOfSound;
            }
        }

        /// <summary>
        /// The speed of the vessel, in multiples of the speed of sound.
        /// </summary>
        /// <remarks>
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public float Mach {
            get {
                CheckNoFAR ();
                return (float)vessel.rootPart.machNumber;
            }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Equivalent_airspeed">equivalent air speed</a> of the vessel, in <math>m/s</math>.
        /// </summary>
        /// <remarks>
        /// Not available when <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> is installed.
        /// </remarks>
        [KRPCProperty]
        public float EquivalentAirSpeed {
            get {
                CheckNoFAR ();
                return (float)Math.Sqrt (vessel.srf_velocity.sqrMagnitude * vessel.atmDensity / 1.225d);
            }
        }

        /// <summary>
        /// An estimate of the current terminal velocity of the vessel, in <math>m/s</math>.
        /// This is the speed at which the drag forces cancel out the force of gravity.
        /// </summary>
        /// <remarks>
        /// Calculated using <a href="http://wiki.kerbalspaceprogram.com/wiki/Atmosphere">KSPs stock aerodynamic model</a>, or
        /// <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a> if it is installed.
        /// </remarks>
        [KRPCProperty]
        public float TerminalVelocity {
            get {
                if (FAR.IsAvailable) {
                    return (float)FAR.VesselTermVelEst (vessel);
                } else {
                    var gravity = Math.Sqrt (vessel.GetTotalMass () * FlightGlobals.getGeeForceAtPosition (vessel.CoM).magnitude);
                    return (float)(Math.Sqrt (gravity / WorldDrag.magnitude) * vessel.speed);
                }
            }
        }

        /// <summary>
        /// Gets the pitch angle between the orientation of the vessel and its velocity vector, in degrees.
        /// </summary>
        [KRPCProperty]
        public float AngleOfAttack {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.VesselAoA (vessel);
                } else {
                    return (float)(Vector3d.Dot (vessel.transform.forward, vessel.srf_velocity.normalized) * (180d / Math.PI));
                }
            }
        }

        /// <summary>
        /// Gets the yaw angle between the orientation of the vessel and its velocity vector, in degrees.
        /// </summary>
        [KRPCProperty]
        public float SideslipAngle {
            get {
                if (FAR.IsAvailable) {
                    CheckFAR ();
                    return (float)FAR.VesselSideslip (vessel);
                } else {
                    return (float)(Vector3d.Dot (vessel.transform.up, Vector3d.Exclude (vessel.transform.forward, vessel.srf_velocity.normalized).normalized) * (180d / Math.PI));
                }
            }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Total_air_temperature">total air temperature</a> of the atmosphere
        /// around the vessel, in Kelvin. This temperature includes the <see cref="StaticAirTemperature"/> and the vessel's kinetic energy.
        /// </summary>
        [KRPCProperty]
        public float TotalAirTemperature {
            get { return (float)vessel.externalTemperature; }
        }

        /// <summary>
        /// The <a href="http://en.wikipedia.org/wiki/Total_air_temperature">static (ambient) temperature</a> of the
        /// atmosphere around the vessel, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float StaticAirTemperature {
            get { return (float)vessel.atmosphericTemperature; }
        }

        /// <summary>
        /// Gets the current amount of stall, between 0 and 1. A value greater than 0.005 indicates a minor stall
        /// and a value greater than 0.5 indicates a large-scale stall.
        /// </summary>
        /// <remarks>
        /// Requires <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float StallFraction {
            get {
                CheckFAR ();
                return (float)FAR.VesselStallFrac (vessel);
            }
        }

        /// <summary>
        /// Gets the coefficient of drag. This is the amount of drag produced by the vessel. It depends on air speed,
        /// air density and wing area.
        /// </summary>
        /// <remarks>
        /// Requires <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float DragCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.VesselDragCoeff (vessel);
            }
        }

        /// <summary>
        /// Gets the coefficient of lift. This is the amount of lift produced by the vessel, and depends on air speed, air density and wing area.
        /// </summary>
        /// <remarks>
        /// Requires <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float LiftCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.VesselLiftCoeff (vessel);
            }
        }

        /// <summary>
        /// Gets the <a href="http://en.wikipedia.org/wiki/Ballistic_coefficient">ballistic coefficient</a>.
        /// </summary>
        /// <remarks>
        /// Requires <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float BallisticCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.VesselBallisticCoeff (vessel);
            }
        }

        /// <summary>
        /// Gets the thrust specific fuel consumption for the jet engines on the vessel. This is a measure of the
        /// efficiency of the engines, with a lower value indicating a more efficient vessel. This value is the
        /// number of Newtons of fuel that are burned, per hour, to product one newton of thrust.
        /// </summary>
        /// <remarks>
        /// Requires <a href="http://forum.kerbalspaceprogram.com/threads/20451">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float ThrustSpecificFuelConsumption {
            get {
                CheckFAR ();
                return (float)FAR.VesselTSFC (vessel);
            }
        }
    }
}
