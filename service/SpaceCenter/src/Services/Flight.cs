using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to get flight telemetry for a vessel, by calling <see cref="Vessel.Flight"/>.
    /// All of the information returned by this class is given in the reference frame
    /// passed to that method.
    /// Obtained by calling <see cref="Vessel.Flight"/>.
    /// </summary>
    /// <remarks>
    /// To get orbital information, such as the apoapsis or inclination, see <see cref="Orbit"/>.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Flight : Equatable<Flight>
    {
        readonly Guid vesselId;
        readonly ReferenceFrame referenceFrame;

        [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
        internal Flight (global::Vessel vessel, ReferenceFrame referenceFrame)
        {
            vesselId = vessel.id;
            this.referenceFrame = referenceFrame;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Flight other)
        {
            return !ReferenceEquals (other, null) && vesselId == other.vesselId && referenceFrame == other.referenceFrame;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (vesselId); }
        }

        /// <summary>
        /// Velocity of the vessel in world space
        /// </summary>
        Vector3d WorldVelocity {
            get { return InternalVessel.GetOrbit ().GetVel (); }
        }

        /// <summary>
        /// Position of the vessels center of mass in world space
        /// </summary>
        Vector3d WorldCoM {
            get { return InternalVessel.CoM; }
        }

        /// <summary>
        /// Direction the vessel is pointing in in world space
        /// </summary>
        Vector3d WorldDirection {
            get { return InternalVessel.ReferenceTransform.up; }
        }

        /// <summary>
        /// Rotation of the vessel in the given reference frame.
        /// Rotation * Vector3d.up gives the direction vector in which the vessel points, in reference frame space.
        /// </summary>
        QuaternionD VesselRotation {
            get { return referenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation); }
        }

        /// <summary>
        /// Orbit prograde direction in world space
        /// </summary>
        Vector3d WorldPrograde {
            get { return InternalVessel.GetOrbit ().GetVel ().normalized; }
        }

        /// <summary>
        /// Orbit normal direction in world space
        /// </summary>
        Vector3d WorldNormal {
            get {
                // Note: y and z components of normal vector are swapped
                return InternalVessel.GetOrbit ().GetOrbitNormal ().SwapYZ ().normalized;
            }
        }

        /// <summary>
        /// Orbit radial direction in world space
        /// </summary>
        Vector3d WorldRadial {
            get { return Vector3d.Cross (WorldNormal, WorldPrograde); }
        }

        /// <summary>
        /// Sum of the lift forces acting every part, in Newtons.
        /// Note this is NOT the force in the vessel's lift direction.
        /// </summary>
        Vector3d WorldPartsLift {
            get {
                CheckNoFAR ();
                Vector3d lift = Vector3d.zero;
                foreach (var part in InternalVessel.Parts) {
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
                return lift * 1000f;
            }
        }

        /// <summary>
        /// Sum of the drag forces acting on every part, in Newtons.
        /// Note this is NOT the force in the vessel's drag direction.
        /// </summary>
        Vector3d WorldPartsDrag {
            get {
                CheckNoFAR ();
                Vector3d drag = Vector3d.zero;
                foreach (var part in InternalVessel.Parts) {
                    // Part drag
                    drag += -part.dragVectorDir * part.dragScalar;
                    // Lifting surface drag
                    foreach (var module in part.Modules) {
                        var wing = module as ModuleLiftingSurface;
                        if (wing != null)
                            drag += wing.dragForce;
                    }
                }
                return drag * 1000f;
            }
        }

        /// <summary>
        /// Total aerodynamic forces acting on vessel in world space.
        /// </summary>
        Vector3d WorldAerodynamicForce {
            get {
                CheckNoFAR ();
                return WorldPartsLift + WorldPartsDrag;
            }
        }

        /// <summary>
        /// Reference area used for lift and drag calculations
        /// </summary>
        double ReferenceArea {
            // TODO: avoid creating vessel object
            get { return new Vessel (InternalVessel).Mass / (BallisticCoefficient * DragCoefficient); }
        }

        /// <summary>
        /// Direction of the lift force acting on the vessel (perpendicular to air stream and up wrt roll angle) in world space.
        /// </summary>
        Vector3d WorldLiftDirection {
            get {
                var vessel = InternalVessel;
                return -Vector3d.Cross (vessel.transform.right, vessel.srf_velocity.normalized);
            }
        }

        /// <summary>
        /// Magnitude of the lift force acting on the vessel, in Newtons.
        /// </summary>
        double LiftMagnitude {
            get {
                if (FAR.IsAvailable)
                    return LiftCoefficient * ReferenceArea * DynamicPressure;
                else
                    return Vector3d.Dot (WorldAerodynamicForce, WorldLiftDirection);
            }
        }

        /// <summary>
        /// Direction of the drag force acting on the vessel (opposite direction to air stream) in world space.
        /// </summary>
        Vector3d WorldDragDirection {
            get { return -InternalVessel.srf_velocity.normalized; }
        }

        /// <summary>
        /// Magnitude of the drag force acting on the vessel.
        /// </summary>
        double DragMagnitude {
            get {
                if (FAR.IsAvailable)
                    return DragCoefficient * ReferenceArea * DynamicPressure;
                else
                    return Vector3d.Dot (WorldAerodynamicForce, WorldDragDirection);
            }
        }

        /// <summary>
        /// Check that FAR is installed and that it is active for the vessel
        /// </summary>
        static void CheckFAR ()
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
        /// The current G force acting on the vessel in <math>g</math>.
        /// </summary>
        [KRPCProperty]
        public float GForce {
            get { return (float)InternalVessel.geeForce; }
        }

        /// <summary>
        /// The altitude above sea level, in meters.
        /// Measured from the center of mass of the vessel.
        /// </summary>
        [KRPCProperty]
        public double MeanAltitude {
            get { return InternalVessel.mainBody.GetAltitude (WorldCoM); }
        }

        /// <summary>
        /// The altitude above the surface of the body or sea level, whichever is closer, in meters.
        /// Measured from the center of mass of the vessel.
        /// </summary>
        [KRPCProperty]
        public double SurfaceAltitude {
            get { return Math.Min (BedrockAltitude, MeanAltitude); }
        }

        /// <summary>
        /// The altitude above the surface of the body, in meters. When over water, this is the altitude above the sea floor.
        /// Measured from the center of mass of the vessel.
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
            get { return InternalVessel.terrainAltitude; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Latitude">latitude</a> of the vessel for the body being orbited, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Latitude {
            get { return InternalVessel.mainBody.GetLatitude (WorldCoM); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Longitude">longitude</a> of the vessel for the body being orbited, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Longitude {
            get { return GeometryExtensions.ClampAngle180 (InternalVessel.mainBody.GetLongitude (WorldCoM)); }
        }

        /// <summary>
        /// The velocity of the vessel, in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The velocity as a vector. The vector points in the direction of travel,
        /// and its magnitude is the speed of the vessel in meters per second.</returns>
        [KRPCProperty]
        public Tuple3 Velocity {
            get { return referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity).ToTuple (); }
        }

        /// <summary>
        /// The speed of the vessel in meters per second,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public double Speed {
            get { return referenceFrame.VelocityFromWorldSpace (WorldCoM, WorldVelocity).magnitude; }
        }

        /// <summary>
        /// The horizontal speed of the vessel in meters per second,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public double HorizontalSpeed {
            get {
                var speed = Speed;
                var verticalSpeed = VerticalSpeed;
                return Math.Sqrt (speed * speed - verticalSpeed * verticalSpeed);
            }
        }

        /// <summary>
        /// The vertical speed of the vessel in meters per second,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public double VerticalSpeed {
            get {
                var worldCoM = WorldCoM;
                var velocity = referenceFrame.VelocityFromWorldSpace (worldCoM, WorldVelocity);
                var up = referenceFrame.DirectionFromWorldSpace ((worldCoM - InternalVessel.orbit.referenceBody.position).normalized);
                return Vector3d.Dot (velocity, up);
            }
        }

        /// <summary>
        /// The position of the center of mass of the vessel,
        /// in the reference frame <see cref="ReferenceFrame"/>
        /// </summary>
        /// <returns>The position as a vector.</returns>
        [KRPCProperty]
        public Tuple3 CenterOfMass {
            get { return referenceFrame.PositionFromWorldSpace (WorldCoM).ToTuple (); }
        }

        /// <summary>
        /// The rotation of the vessel, in the reference frame <see cref="ReferenceFrame"/>
        /// </summary>
        /// <returns>The rotation as a quaternion of the form <math>(x, y, z, w)</math>.</returns>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return VesselRotation.ToTuple (); }
        }

        /// <summary>
        /// The direction that the vessel is pointing in,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 Direction {
            get { return referenceFrame.DirectionFromWorldSpace (WorldDirection).normalized.ToTuple (); }
        }

        /// <summary>
        /// The pitch of the vessel relative to the horizon, in degrees.
        /// A value between -90° and +90°.
        /// </summary>
        [KRPCProperty]
        public float Pitch {
            get { return (float)VesselRotation.PitchHeadingRoll ().x; }
        }

        /// <summary>
        /// The heading of the vessel (its angle relative to north), in degrees.
        /// A value between 0° and 360°.
        /// </summary>
        [KRPCProperty]
        public float Heading {
            get { return (float)VesselRotation.PitchHeadingRoll ().y; }
        }

        /// <summary>
        /// The roll of the vessel relative to the horizon, in degrees.
        /// A value between -180° and +180°.
        /// </summary>
        [KRPCProperty]
        public float Roll {
            get { return (float)VesselRotation.PitchHeadingRoll ().z; }
        }

        /// <summary>
        /// The prograde direction of the vessels orbit,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 Prograde {
            get { return referenceFrame.DirectionFromWorldSpace (WorldPrograde).ToTuple (); }
        }

        /// <summary>
        /// The retrograde direction of the vessels orbit,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 Retrograde {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldPrograde).ToTuple (); }
        }

        /// <summary>
        /// The direction normal to the vessels orbit,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 Normal {
            get { return referenceFrame.DirectionFromWorldSpace (WorldNormal).ToTuple (); }
        }

        /// <summary>
        /// The direction opposite to the normal of the vessels orbit,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 AntiNormal {
            get { return referenceFrame.DirectionFromWorldSpace (-WorldNormal).ToTuple (); }
        }

        /// <summary>
        /// The radial direction of the vessels orbit,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 Radial {
            get { return referenceFrame.DirectionFromWorldSpace (WorldRadial).ToTuple (); }
        }

        /// <summary>
        /// The direction opposite to the radial direction of the vessels orbit,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
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
                return (float)InternalVessel.atmDensity;
            }
        }

        /// <summary>
        /// The dynamic pressure acting on the vessel, in Pascals. This is a measure of the
        /// strength of the aerodynamic forces. It is equal to
        /// <math>\frac{1}{2} . \mbox{air density} . \mbox{velocity}^2</math>.
        /// It is commonly denoted <math>Q</math>.
        /// </summary>
        [KRPCProperty]
        public float DynamicPressure {
            get {
                var vessel = InternalVessel;
                if (FAR.IsAvailable)
                    return (float)FAR.VesselDynPres (vessel) * 1000f;
                else
                    return (float)(0.5f * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude);
            }
        }

        /// <summary>
        /// The static atmospheric pressure at mean sea level, in Pascals.
        /// </summary>
        [KRPCProperty]
        public float StaticPressureAtMSL {
            get {
                return (float)InternalVessel.mainBody.atmospherePressureSeaLevel * 1000f;
            }
        }

        /// <summary>
        /// The static atmospheric pressure acting on the vessel, in Pascals.
        /// </summary>
        [KRPCProperty]
        public float StaticPressure {
            get {
                return (float)InternalVessel.staticPressurekPa * 1000f;
            }
        }

        /// <summary>
        /// The total aerodynamic forces acting on the vessel,
        /// in reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>A vector pointing in the direction that the force acts,
        /// with its magnitude equal to the strength of the force in Newtons.</returns>
        [KRPCProperty]
        public Tuple3 AerodynamicForce {
            get {
                if (FAR.IsAvailable)
                    return referenceFrame.DirectionFromWorldSpace (WorldDragDirection * DragMagnitude + WorldLiftDirection * LiftMagnitude).ToTuple ();
                else
                    return referenceFrame.DirectionFromWorldSpace (WorldAerodynamicForce).ToTuple ();
            }
        }

        /// <summary>
        /// Simulate and return the total aerodynamic forces acting on the vessel,
        /// if it where to be traveling with the given velocity at the given position in the
        /// atmosphere of the given celestial body.
        /// </summary>
        /// <returns>A vector pointing in the direction that the force acts,
        /// with its magnitude equal to the strength of the force in Newtons.</returns>
        [KRPCMethod]
        public Tuple3 SimulateAerodynamicForceAt(CelestialBody body, Tuple3 position, Tuple3 velocity)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            var vessel = InternalVessel;
            var worldVelocity = referenceFrame.VelocityToWorldSpace(position.ToVector(), velocity.ToVector());
            var worldPosition = referenceFrame.PositionToWorldSpace(position.ToVector());
            Vector3 worldForce;
            if (!FAR.IsAvailable) {
                var relativeWorldVelocity = worldVelocity - body.InternalBody.getRFrmVel(worldPosition);
                worldForce = StockAerodynamics.SimAeroForce(body.InternalBody, vessel, relativeWorldVelocity, worldPosition);
            } else {
                Vector3 torque;
                var altitude = (worldPosition - body.InternalBody.position).magnitude - body.InternalBody.Radius;
                FAR.CalculateVesselAeroForces(vessel, out worldForce, out torque, worldVelocity - body.InternalBody.getRFrmVel(worldPosition), altitude);
            }
            return referenceFrame.DirectionFromWorldSpace(worldForce).ToTuple();
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Aerodynamic_force">aerodynamic lift</a>
        /// currently acting on the vessel.
        /// </summary>
        /// <returns>A vector pointing in the direction that the force acts,
        /// with its magnitude equal to the strength of the force in Newtons.</returns>
        [KRPCProperty]
        public Tuple3 Lift {
            get { return (referenceFrame.DirectionFromWorldSpace (WorldLiftDirection) * LiftMagnitude).ToTuple (); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Aerodynamic_force">aerodynamic drag</a> currently acting on the vessel.
        /// </summary>
        /// <returns>A vector pointing in the direction of the force, with its magnitude
        /// equal to the strength of the force in Newtons.</returns>
        [KRPCProperty]
        public Tuple3 Drag {
            get { return (referenceFrame.DirectionFromWorldSpace (WorldDragDirection) * DragMagnitude).ToTuple (); }
        }

        /// <summary>
        /// The speed of sound, in the atmosphere around the vessel, in <math>m/s</math>.
        /// </summary>
        [KRPCProperty]
        public float SpeedOfSound {
            get {
                return (float)InternalVessel.speedOfSound;
            }
        }

        /// <summary>
        /// The speed of the vessel, in multiples of the speed of sound.
        /// </summary>
        [KRPCProperty]
        public float Mach {
            get {
                var vessel = InternalVessel;
                return (float)(FAR.IsAvailable ? FAR.VesselMachNumber (vessel) : vessel.rootPart.machNumber);
            }
        }

        /// <summary>
        /// The vessels Reynolds number.
        /// </summary>
        /// <remarks>
        /// Requires <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float ReynoldsNumber {
            get {
                CheckFAR ();
                return (float)FAR.VesselReynoldsNumber (InternalVessel);
            }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/True_airspeed">true air speed</a>
        /// of the vessel, in meters per second.
        /// </summary>
        [KRPCProperty]
        public float TrueAirSpeed {
            get { return (float)InternalVessel.srfSpeed; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Equivalent_airspeed">equivalent air speed</a>
        /// of the vessel, in meters per second.
        /// </summary>
        [KRPCProperty]
        public float EquivalentAirSpeed {
            get {
                var vessel = InternalVessel;
                return (float)Math.Sqrt (vessel.srf_velocity.sqrMagnitude * vessel.atmDensity / 1.225d);
            }
        }

        /// <summary>
        /// An estimate of the current terminal velocity of the vessel, in meters per second.
        /// This is the speed at which the drag forces cancel out the force of gravity.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float TerminalVelocity {
            get {
                var vessel = InternalVessel;
                if (FAR.IsAvailable) {
                    return (float)FAR.VesselTermVelEst (vessel);
                } else {
                    var mass = vessel.parts.Sum(part => part.WetMass());
                    var gForce = FlightGlobals.getGeeForceAtPosition(WorldCoM).magnitude;
                    var drag = FlightGlobals.ActiveVessel.parts.Sum(part => part.DragCubes.AreaDrag) * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;
                    var atmDensity = FlightGlobals.ActiveVessel.atmDensity;
                    return (float)Math.Sqrt((2.0 * mass * gForce) / (atmDensity * drag));
                }
            }
        }

        /// <summary>
        /// The pitch angle between the orientation of the vessel and its velocity vector,
        /// in degrees.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float AngleOfAttack {
            get {
                var vessel = InternalVessel;
                if (FAR.IsAvailable) {
                    return (float)FAR.VesselAoA (vessel);
                } else {
                    return (float)GeometryExtensions.ToDegrees (Vector3d.Dot (vessel.transform.forward, vessel.srf_velocity.normalized));
                }
            }
        }

        /// <summary>
        /// The yaw angle between the orientation of the vessel and its velocity vector, in degrees.
        /// </summary>
        [KRPCProperty]
        public float SideslipAngle {
            get {
                var vessel = InternalVessel;
                if (FAR.IsAvailable) {
                    return (float)FAR.VesselSideslip (vessel);
                } else {
                    return (float)GeometryExtensions.ToDegrees (Vector3d.Dot (vessel.transform.right, vessel.srf_velocity.normalized));
                }
            }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Total_air_temperature">total air temperature</a>
        /// of the atmosphere around the vessel, in Kelvin.
        /// This includes the <see cref="StaticAirTemperature"/> and the vessel's kinetic energy.
        /// </summary>
        [KRPCProperty]
        public float TotalAirTemperature {
            get { return (float)InternalVessel.externalTemperature; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Total_air_temperature">static (ambient)
        /// temperature</a> of the atmosphere around the vessel, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float StaticAirTemperature {
            get { return (float)InternalVessel.atmosphericTemperature; }
        }

        /// <summary>
        /// The current amount of stall, between 0 and 1. A value greater than 0.005 indicates
        /// a minor stall and a value greater than 0.5 indicates a large-scale stall.
        /// </summary>
        /// <remarks>
        /// Requires <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float StallFraction {
            get {
                CheckFAR ();
                return (float)FAR.VesselStallFrac (InternalVessel);
            }
        }

        /// <summary>
        /// The coefficient of drag. This is the amount of drag produced by the vessel.
        /// It depends on air speed, air density and wing area.
        /// </summary>
        /// <remarks>
        /// Requires <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float DragCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.VesselDragCoeff (InternalVessel);
            }
        }

        /// <summary>
        /// The coefficient of lift. This is the amount of lift produced by the vessel, and
        /// depends on air speed, air density and wing area.
        /// </summary>
        /// <remarks>
        /// Requires <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float LiftCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.VesselLiftCoeff (InternalVessel);
            }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Ballistic_coefficient">ballistic coefficient</a>.
        /// </summary>
        /// <remarks>
        /// Requires <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float BallisticCoefficient {
            get {
                CheckFAR ();
                return (float)FAR.VesselBallisticCoeff (InternalVessel);
            }
        }

        /// <summary>
        /// The thrust specific fuel consumption for the jet engines on the vessel. This is a
        /// measure of the efficiency of the engines, with a lower value indicating a more
        /// efficient vessel. This value is the number of Newtons of fuel that are burned,
        /// per hour, to produce one newton of thrust.
        /// </summary>
        /// <remarks>
        /// Requires <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>.
        /// </remarks>
        [KRPCProperty]
        public float ThrustSpecificFuelConsumption {
            get {
                CheckFAR ();
                return (float)FAR.VesselTSFC (InternalVessel);
            }
        }
    }
}
