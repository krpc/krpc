using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;
using TupleT3 = System.Tuple<System.Tuple<double, double, double>, System.Tuple<double, double, double>>;

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
                foreach (var part in InternalVessel.Parts)
                    lift += StockAeroReadout.Lift (part);
                return lift * 1000d;
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
                foreach (var part in InternalVessel.Parts)
                    drag += StockAeroReadout.Drag (part);
                return drag * 1000d;
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
        /// Net aerodynamic torque about the vessel's center of mass, in world space, in
        /// newton-meters. This is a live readout that reconstructs the exact per-part
        /// force and application-point pairs KSP applied on the current physics frame:
        /// cube drag (-dragVectorDir * dragScalar) at the part's center-of-pressure point,
        /// body lift from the bodyLiftLocalVector/bodyLiftLocalPosition values the
        /// FlightIntegrator writes back each frame, and lifting-surface lift and drag
        /// (the modules' live liftForce/dragForce) at the center-of-lift and
        /// center-of-pressure points they are applied at. Also includes the first-order
        /// equivalent of the per-part rigidbody angular drag the engine applies
        /// (-rb.angularDrag * I_part * omega), which damps rotation without ever
        /// appearing as a force.
        /// </summary>
        Vector3d WorldAerodynamicTorque {
            get {
                CheckNoFAR ();
                var com = WorldCoM;
                Vector3d torque = Vector3d.zero;
                foreach (var part in InternalVessel.Parts) {
                    if (part.Rigidbody == null)
                        continue;
                    Vector3d liftPoint = part.partTransform.TransformPoint (part.CoLOffset);
                    Vector3d dragPoint = part.partTransform.TransformPoint (part.CoPOffset);
                    // Part (cube) drag, applied by FlightIntegrator.ApplyAeroDrag at the
                    // center-of-pressure point
                    Vector3d cubeDrag = -part.dragVectorDir * part.dragScalar;
                    torque += Vector3d.Cross (dragPoint - com, cubeDrag);
                    // Body lift: FlightIntegrator.ApplyAeroLift writes the exact applied
                    // vector and point back to these part-local fields each frame. It skips
                    // parts with a lifting-surface module AND parts whose
                    // bodyLiftOnlyUnattachedLift gate is closed (pod with a heatshield on
                    // the designated node), so mirror both here (the fields would be stale
                    // for those parts).
                    var bodyLiftGated = part.bodyLiftOnlyUnattachedLiftActual
                                        && part.bodyLiftOnlyProvider != null
                                        && part.bodyLiftOnlyProvider.IsLifting;
                    if (!part.hasLiftModule && !bodyLiftGated) {
                        Vector3d bodyLift = part.partTransform.TransformDirection (part.bodyLiftLocalVector);
                        Vector3d bodyLiftPoint = part.partTransform.TransformPoint (part.bodyLiftLocalPosition);
                        torque += Vector3d.Cross (bodyLiftPoint - com, bodyLift);
                    }
                    // Lifting-surface (wing / control-surface) lift and drag, applied by
                    // the modules at the center-of-lift and center-of-pressure points
                    foreach (var module in part.Modules) {
                        var wing = module as ModuleLiftingSurface;
                        if (wing == null)
                            continue;
                        torque += Vector3d.Cross (liftPoint - com, (Vector3d)wing.liftForce);
                        torque += Vector3d.Cross (dragPoint - com, (Vector3d)wing.dragForce);
                    }
                    // Unity rigidbody angular drag: the FlightIntegrator re-applies
                    // rb.angularDrag = part.angularDrag * dynamicPressure(atm) *
                    // PhysicsGlobals.AngularDragMultiplier every frame, and the engine
                    // damps the rigidbody's rotation by it. It is a real attitude torque
                    // the game applies that never appears as a per-part force, so add its
                    // first-order equivalent (-angularDrag * I_part * omega) to keep this
                    // live sum consistent with the vessel's measured net torque and with
                    // SimulateAerodynamicTorqueAt. Gate on the part's OWN rigidbody
                    // (physicsless parts share the parent's and get no angular drag).
                    // KSP runs Unity physics in tonne/kilonewton units, so rb.inertiaTensor
                    // is in tonne*m^2 and angularDrag * I * omega is already in
                    // kilonewton-meters -- the same scale as this accumulator.
                    var rb = part.rb;
                    if (rb != null && rb.angularDrag > 0f)
                        torque -= (Vector3d)(rb.angularDrag *
                            StockAerodynamics.PartAngularMomentum (rb, rb.angularVelocity));
                }
                return torque * 1000d;
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
        /// The acceleration of the vessel, in the reference frame <see cref="ReferenceFrame"/>.
        /// This is the total acceleration, including the acceleration due to gravity, and is the
        /// time derivative of <see cref="Velocity"/>.
        /// </summary>
        /// <returns>The acceleration as a vector. The vector points in the direction of the
        /// acceleration, and its magnitude is the acceleration of the vessel in <math>m/s^2</math>.</returns>
        [KRPCProperty]
        public Tuple3 Acceleration {
            // Use acceleration_immediate (the raw per-frame change in orbital velocity) rather than
            // the acceleration field, which KSP boxcar-averages over several frames for the G-force
            // gauge. The immediate value is the true time derivative of Velocity and responds
            // without the averaging lag.
            get { return referenceFrame.DirectionFromWorldSpace (InternalVessel.acceleration_immediate).ToTuple (); }
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
        /// <remarks>
        /// An absolute Euler angle, ill-conditioned when the vessel points near vertical (pitch →
        /// ±90°), where heading and roll become ambiguous. For an always-defined attitude use
        /// <see cref="Rotation"/> or <see cref="Direction"/>.
        /// </remarks>
        [KRPCProperty]
        public float Pitch {
            get { return (float)VesselRotation.PitchHeadingRoll ().x; }
        }

        /// <summary>
        /// The heading of the vessel (its angle relative to north), in degrees.
        /// A value between 0° and 360°.
        /// </summary>
        /// <remarks>
        /// An absolute Euler angle, undefined when the vessel points near vertical (pitch → ±90°).
        /// For an always-defined attitude use <see cref="Rotation"/> or <see cref="Direction"/>.
        /// </remarks>
        [KRPCProperty]
        public float Heading {
            get { return (float)VesselRotation.PitchHeadingRoll ().y; }
        }

        /// <summary>
        /// The roll of the vessel relative to the horizon, in degrees.
        /// A value between -180° and +180°.
        /// </summary>
        /// <remarks>
        /// An absolute Euler angle, ill-conditioned when the vessel points near vertical (pitch →
        /// ±90°), where the vertical-plane reference vanishes. For an always-defined attitude use
        /// <see cref="Rotation"/>; for a well-defined roll use the auto-pilot's
        /// <c>TargetRoll</c> / <c>RollError</c> against a chosen up reference.
        /// </remarks>
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
        /// The direction of the vessels surface velocity,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// This is the prograde direction as shown on the navball when in surface mode.
        /// </summary>
        /// <remarks>
        /// Singular when surface speed is approximately zero.
        /// </remarks>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 SurfacePrograde {
            get { return referenceFrame.DirectionFromWorldSpace (InternalVessel.srf_velocity.normalized).ToTuple (); }
        }

        /// <summary>
        /// The direction opposite to the vessels surface velocity,
        /// in the reference frame <see cref="ReferenceFrame"/>.
        /// This is the retrograde direction as shown on the navball when in surface mode.
        /// </summary>
        /// <remarks>
        /// Singular when surface speed is approximately zero.
        /// </remarks>
        /// <returns>The direction as a unit vector.</returns>
        [KRPCProperty]
        public Tuple3 SurfaceRetrograde {
            get { return referenceFrame.DirectionFromWorldSpace (-InternalVessel.srf_velocity.normalized).ToTuple (); }
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
        /// ½ · air density · velocity².
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
        /// The net aerodynamic torque currently acting on the vessel about its center of
        /// mass, in reference frame <see cref="ReferenceFrame"/>. The magnitude is in
        /// newton-meters.
        /// </summary>
        /// <returns>A vector pointing along the axis of the torque, with its magnitude
        /// equal to the strength of the torque in newton-meters.</returns>
        /// <remarks>
        /// This is the live counterpart to <see cref="AerodynamicForce"/>: it reconstructs
        /// the per-part aerodynamic forces and application points that the game applied on
        /// the current physics frame and levers them about the center of mass, rather than
        /// re-simulating them for hypothetical conditions the way
        /// <see cref="SimulateAerodynamicTorqueAt"/> does. It is intended for validating the
        /// simulator against the live game state. Not available when
        /// <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>
        /// is installed, as FAR does not expose a live per-frame torque.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 AerodynamicTorque {
            get {
                CheckNoFAR ();
                return referenceFrame.DirectionFromWorldSpace (WorldAerodynamicTorque).ToTuple ();
            }
        }

        /// <summary>
        /// Simulate and return the total aerodynamic forces acting on the vessel,
        /// if it were traveling with the given velocity, at the given position and
        /// orientation, in the atmosphere of the given celestial body.
        /// </summary>
        /// <param name="body">The celestial body whose atmosphere the forces are simulated in.</param>
        /// <param name="position">The position of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>.</param>
        /// <param name="velocity">The velocity of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>.</param>
        /// <param name="rotation">The orientation of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>, in the same form as <see cref="Vessel.Rotation"/>.
        /// The angle of attack and sideslip follow from this orientation relative to the
        /// velocity; the roll component sets the direction of any aerodynamic lift. Pass
        /// the vessel's current rotation to evaluate the force at its current orientation.</param>
        /// <returns>A vector pointing in the direction that the force acts, with its
        /// magnitude equal to the strength of the force in Newtons, in reference frame
        /// <see cref="ReferenceFrame"/>.</returns>
        /// <remarks>
        /// The position, velocity and rotation arguments, and the returned force, are all
        /// expressed in reference frame <see cref="ReferenceFrame"/>. The result is the
        /// force the vessel would experience if it were placed at that position and
        /// orientation with the air flowing past it at that velocity; it is the force at
        /// the requested orientation, not the force in the vessel's current orientation.
        /// Atmospheric temperature and density are evaluated at the current universal time.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 SimulateAerodynamicForceAt(CelestialBody body, Tuple3 position, Tuple3 velocity, Tuple4 rotation)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            var vessel = InternalVessel;
            var worldVelocity = referenceFrame.VelocityToWorldSpace(position.ToVector(), velocity.ToVector());
            var worldPosition = referenceFrame.PositionToWorldSpace(position.ToVector());
            QuaternionD desiredWorld = referenceFrame.RotationToWorldSpace (rotation.ToQuaternion ());
            QuaternionD currentWorld = vessel.ReferenceTransform.rotation;
            var delta = desiredWorld * currentWorld.Inverse ();
            Vector3 force;
            if (!FAR.IsAvailable) {
                force = StockAerodynamics.SimAeroForce(
                    body.InternalBody, vessel, worldVelocity, worldPosition, delta);
            } else {
                Vector3 torque;
                var altitude = (worldPosition - body.InternalBody.position).magnitude - body.InternalBody.Radius;
                var adjustedVelocity = delta.Inverse ()
                    * (worldVelocity - body.InternalBody.getRFrmVel(worldPosition));
                FAR.CalculateVesselAeroForces(vessel, out force, out torque, adjustedVelocity, altitude);
                // CalculateVesselAeroForces returns kilonewtons; convert to newtons to
                // match the stock path and this method's documented units.
                force = force * 1000f;
                force = (Vector3)(delta * (Vector3d)force);
            }
            return referenceFrame.DirectionFromWorldSpace(force).ToTuple();
        }

        /// <summary>
        /// Simulate and return the total aerodynamic force and torque acting on the vessel,
        /// if its center of mass were traveling with the given velocity, at the given position,
        /// orientation and angular velocity, in the atmosphere of the given celestial body.
        /// </summary>
        /// <param name="body">The celestial body whose atmosphere the wrench is simulated in.</param>
        /// <param name="position">The position of the vessel's center of mass, in reference
        /// frame <see cref="ReferenceFrame"/>.</param>
        /// <param name="velocity">The velocity of the vessel's center of mass, in reference
        /// frame <see cref="ReferenceFrame"/>.</param>
        /// <param name="rotation">The orientation of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>, in the same form as <see cref="Vessel.Rotation"/>.
        /// Pass the vessel's current rotation to evaluate the wrench at its current
        /// orientation.</param>
        /// <param name="angularVelocity">The angular velocity of the vessel, in reference
        /// frame <see cref="ReferenceFrame"/>. This adds the solid-body rotation term to each
        /// part's local airflow and the per-part rigid-body angular drag the game applies,
        /// together giving the aerodynamic damping force and torque. Pass a zero vector to
        /// evaluate the static wrench relative to the reference frame.</param>
        /// <param name="ut">The universal time used for the atmospheric ephemeris. It
        /// selects the body/Sun geometry used for temperature and density, but does not
        /// change or propagate <see cref="ReferenceFrame"/> or any of the state arguments.</param>
        /// <returns>A pair containing the aerodynamic force in newtons followed by the
        /// aerodynamic torque in newton-meters about the vessel's center of mass. Both are
        /// vectors in reference frame <see cref="ReferenceFrame"/>.</returns>
        /// <remarks>
        /// The position and velocity describe the hypothetical center-of-mass state. The
        /// position, velocity, rotation and angular velocity arguments, and both returned
        /// vectors, are expressed in reference frame <see cref="ReferenceFrame"/>.
        /// For future-state prediction, <see cref="CelestialBody.NonRotatingReferenceFrame"/>
        /// is recommended so that the spatial state has unambiguous inertial semantics.
        ///
        /// This is an instantaneous rigid-body result based on the vessel's current parts,
        /// drag cubes and control-surface state. When
        /// <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>
        /// is installed the angular velocity and <paramref name="ut"/> arguments are ignored.
        /// </remarks>
        [KRPCMethod]
        public TupleT3 SimulateAerodynamicWrenchAt(CelestialBody body, Tuple3 position, Tuple3 velocity, Tuple4 rotation, Tuple3 angularVelocity, double ut)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            var vessel = InternalVessel;
            var worldVelocity = referenceFrame.VelocityToWorldSpace(position.ToVector(), velocity.ToVector());
            var worldPosition = referenceFrame.PositionToWorldSpace(position.ToVector());
            var worldAngularVelocity = referenceFrame.AngularVelocityToWorldSpace(angularVelocity.ToVector());
            QuaternionD desiredWorld = referenceFrame.RotationToWorldSpace (rotation.ToQuaternion ());
            QuaternionD currentWorld = vessel.ReferenceTransform.rotation;
            var delta = desiredWorld * currentWorld.Inverse ();
            Vector3d force;
            Vector3d torque;
            if (!FAR.IsAvailable) {
                var wrench = StockAerodynamics.SimAeroWrench(
                    body.InternalBody, vessel, worldVelocity, worldAngularVelocity,
                    worldPosition, delta, ut, true, false);
                force = wrench.Force;
                torque = wrench.Torque;
            } else {
                Vector3 farForce;
                Vector3 farTorque;
                var adjustedVelocity = delta.Inverse ()
                    * (worldVelocity - body.InternalBody.getRFrmVel(worldPosition));
                var altitude = (worldPosition - body.InternalBody.position).magnitude
                               - body.InternalBody.Radius;
                FAR.CalculateVesselAeroForces(
                    vessel, out farForce, out farTorque, adjustedVelocity, altitude);
                // FAR returns kilonewtons and kilonewton-meters. Rotate the one
                // evaluation into the hypothetical attitude and convert both to SI.
                force = delta * (Vector3d)farForce * 1000d;
                torque = delta * (Vector3d)farTorque * 1000d;
            }
            return new TupleT3(
                referenceFrame.DirectionFromWorldSpace(force).ToTuple(),
                referenceFrame.DirectionFromWorldSpace(torque).ToTuple());
        }

        /// <summary>
        /// Simulate and return the total aerodynamic torque acting on the vessel about its
        /// center of mass, if it were traveling with the given velocity, at the given
        /// position, orientation and angular velocity, in the atmosphere of the given
        /// celestial body.
        /// </summary>
        /// <param name="body">The celestial body whose atmosphere the torque is simulated in.</param>
        /// <param name="position">The position of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>.</param>
        /// <param name="velocity">The velocity of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>.</param>
        /// <param name="rotation">The orientation of the vessel, in reference frame
        /// <see cref="ReferenceFrame"/>, in the same form as <see cref="Vessel.Rotation"/>.
        /// Pass the vessel's current rotation to evaluate the torque at its current
        /// orientation.</param>
        /// <param name="angularVelocity">The angular velocity of the vessel, in reference
        /// frame <see cref="ReferenceFrame"/>. This adds the solid-body rotation term to each
        /// part's local airflow and the per-part rigid-body angular drag the game applies,
        /// together giving the aerodynamic damping torque. Pass a zero vector to
        /// evaluate the static torque.</param>
        /// <returns>A vector pointing along the axis of the torque, with its magnitude equal
        /// to the strength of the torque in newton-meters, in reference frame
        /// <see cref="ReferenceFrame"/>.</returns>
        /// <remarks>
        /// The position, velocity, rotation and angular velocity arguments, and the returned
        /// torque, are all expressed in reference frame <see cref="ReferenceFrame"/>. When
        /// <a href="https://forum.kerbalspaceprogram.com/index.php?/topic/19321-130-ferram-aerospace-research-v0159-liebe-82117/">Ferram Aerospace Research</a>
        /// is installed the angular velocity argument is ignored.
        /// Atmospheric temperature and density are evaluated at the current universal time.
        ///
        /// This is the ideal rigid-body aerodynamic torque, summed from the per-part forces
        /// about the center of mass. A vessel may not visibly rotate by the full amount when
        /// a large aerodynamic force acts on a small part far from the center of mass, because
        /// the game applies each part's force to that part and propagates it through the joints
        /// rather than to the vessel as a rigid body.
        /// </remarks>
        [KRPCMethod]
        public Tuple3 SimulateAerodynamicTorqueAt(CelestialBody body, Tuple3 position, Tuple3 velocity, Tuple4 rotation, Tuple3 angularVelocity)
        {
            return SimulateAerodynamicWrenchAt(
                body, position, velocity, rotation, angularVelocity,
                Planetarium.GetUniversalTime()).Item2;
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
        /// The acceleration of the vessel due to the total aerodynamic forces acting on it
        /// (<see cref="AerodynamicForce"/> divided by the vessel's mass),
        /// in reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>A vector pointing in the direction that the vessel is accelerated,
        /// with its magnitude equal to the acceleration in <math>m/s^2</math>.</returns>
        [KRPCProperty]
        public Tuple3 AerodynamicAcceleration {
            get {
                var mass = new Vessel (InternalVessel).Mass;
                Vector3d worldForce;
                if (FAR.IsAvailable)
                    worldForce = WorldDragDirection * DragMagnitude + WorldLiftDirection * LiftMagnitude;
                else
                    worldForce = WorldAerodynamicForce;
                return referenceFrame.DirectionFromWorldSpace (worldForce / mass).ToTuple ();
            }
        }

        /// <summary>
        /// The acceleration of the vessel due to <see cref="Lift"/>
        /// (the aerodynamic lift divided by the vessel's mass),
        /// in reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>A vector pointing in the direction that the vessel is accelerated,
        /// with its magnitude equal to the acceleration in <math>m/s^2</math>.</returns>
        [KRPCProperty]
        public Tuple3 LiftAcceleration {
            get {
                var mass = new Vessel (InternalVessel).Mass;
                return (referenceFrame.DirectionFromWorldSpace (WorldLiftDirection) * (LiftMagnitude / mass)).ToTuple ();
            }
        }

        /// <summary>
        /// The acceleration of the vessel due to <see cref="Drag"/>
        /// (the aerodynamic drag divided by the vessel's mass),
        /// in reference frame <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <returns>A vector pointing in the direction that the vessel is accelerated,
        /// with its magnitude equal to the acceleration in <math>m/s^2</math>.</returns>
        [KRPCProperty]
        public Tuple3 DragAcceleration {
            get {
                var mass = new Vessel (InternalVessel).Mass;
                return (referenceFrame.DirectionFromWorldSpace (WorldDragDirection) * (DragMagnitude / mass)).ToTuple ();
            }
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
        public float AngleOfAttack {
            get {
                var vessel = InternalVessel;
                if (FAR.IsAvailable) {
                    return (float)FAR.VesselAoA (vessel);
                } else {
                    return (float)GeometryExtensions.ToDegrees (Math.Asin (GeometryExtensions.Clamp (Vector3d.Dot (vessel.transform.forward, vessel.srf_velocity.normalized), -1d, 1d)));
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
                    return (float)GeometryExtensions.ToDegrees (Math.Asin (GeometryExtensions.Clamp (Vector3d.Dot (vessel.transform.right, vessel.srf_velocity.normalized), -1d, 1d)));
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
