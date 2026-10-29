using System;
using System.Collections.Generic;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Flight telemetry for a vehicle, relative to the surface of the body it orbits.
    /// Obtained from <see cref="Vehicle.Flight"/>.
    /// </summary>
    [KRPCClass (Service = "KSA", GameScene = GameScene.Flight)]
    public class Flight : Equatable<Flight>, IGameObjectState
    {
        readonly global::KSA.Vehicle vehicle;

        internal Flight (global::KSA.Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Flight other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Vehicles.StateOf (vehicle); }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vehicle.GetHashCode ();
        }

        global::KSA.Vehicle InternalVehicle {
            get { return new Vehicle (vehicle).InternalVehicle; }
        }

        /// <summary>
        /// The altitude above the mean radius of the body, in meters.
        /// </summary>
        [KRPCProperty]
        public double MeanAltitude {
            get { return InternalVehicle.GetBarometricAltitude (); }
        }

        /// <summary>
        /// The altitude above the terrain or the ocean surface, whichever is higher, in meters.
        /// </summary>
        [KRPCProperty]
        public double SurfaceAltitude {
            get { return InternalVehicle.GetRadarAltitude (); }
        }

        /// <summary>
        /// The speed of the vehicle relative to the surface, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Speed {
            get { return InternalVehicle.GetSurfaceSpeed (); }
        }

        /// <summary>
        /// The horizontal speed of the vehicle relative to the surface, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double HorizontalSpeed {
            get {
                var v = InternalVehicle;
                var speed = v.GetSurfaceSpeed ();
                var vertical = VerticalSpeedOf (v);
                return Math.Sqrt (Math.Max (0, speed * speed - vertical * vertical));
            }
        }

        /// <summary>
        /// The vertical speed of the vehicle relative to the surface, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double VerticalSpeed {
            get { return VerticalSpeedOf (InternalVehicle); }
        }

        static double VerticalSpeedOf (global::KSA.Vehicle v)
        {
            return double3.Dot (v.GetSurfaceVelocityCci (), double3.Normalize (v.GetPositionCci ()));
        }

        /// <summary>
        /// The pitch of the vehicle relative to the horizon, in degrees.
        /// A value between -90° and +90°.
        /// </summary>
        [KRPCProperty]
        public float Pitch {
            get { return (float)RadiansToDegrees (Attitude.Y); }
        }

        /// <summary>
        /// The heading of the vehicle (its angle relative to north), in degrees.
        /// A value between 0° and 360°.
        /// </summary>
        [KRPCProperty]
        public float Heading {
            get { return (float)YawToHeading (Attitude.Z); }
        }

        /// <summary>
        /// The roll of the vehicle relative to the horizon, in degrees.
        /// A value between -180° and +180°.
        /// </summary>
        [KRPCProperty]
        public float Roll {
            get { return (float)RadiansToDegrees (Attitude.X); }
        }

        double3 Attitude {
            get { return AttitudeOf (InternalVehicle); }
        }

        // The game's attitude frame has x east, y south and z down. Yaw turns about z from
        // east toward south, so the heading from north is the yaw plus 90 degrees
        internal static double YawToHeading (double yaw)
        {
            var heading = (RadiansToDegrees (yaw) + 90) % 360;
            return heading < 0 ? heading + 360 : heading;
        }

        internal static double HeadingToYaw (double heading)
        {
            return (heading - 90) * (Math.PI / 180d);
        }

        // The roll, pitch and yaw of the vehicle's control frame in the game's attitude
        // frame, in radians, in the convention of the flight computer's custom target
        internal static double3 AttitudeOf (global::KSA.Vehicle v)
        {
            var enu2Cci = VehicleReferenceFrameEx.GetEnuBody2Cci (v.Orbit.StateVectors.PositionCci);
            if (!enu2Cci.HasValue)
                return new double3 (double.NaN, double.NaN, double.NaN);
            var helm2Enu = doubleQuat.Concatenate (v.GetHelm2Cci (), enu2Cci.Value.Inverse ());
            return helm2Enu.ToRollPitchYawRadians ();
        }

        // The vehicle's position in the rotating frame of the body it orbits
        double3 PositionCcf {
            get {
                var v = InternalVehicle;
                return v.GetPositionCci ().Transform (v.Orbit.Parent.GetCci2Ccf ());
            }
        }

        /// <summary>
        /// The latitude of the vehicle, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Latitude {
            get { return Celestial.GetLatitudeFromCcf (PositionCcf); }
        }

        /// <summary>
        /// The longitude of the vehicle, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Longitude {
            get { return Celestial.GetLongitudeFromCcf (PositionCcf); }
        }

        /// <summary>
        /// The biome below the vehicle, or <c>null</c> if the body has no biomes.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public string Biome {
            get {
                if (InternalVehicle.Orbit.Parent is not Celestial celestial)
                    return null;
                return Body.BiomeAt (celestial, PositionCcf.NormalizeOrZero ());
            }
        }

        /// <summary>
        /// The static atmospheric pressure at the vehicle, in pascals.
        /// </summary>
        [KRPCProperty]
        public double StaticPressure {
            get { return InternalVehicle.PhysicsEnvironment.AtmosphericPressure; }
        }

        /// <summary>
        /// The density of the atmosphere at the vehicle, in kilograms per cubic meter.
        /// </summary>
        [KRPCProperty]
        public double AtmosphereDensity {
            get { return InternalVehicle.PhysicsEnvironment.AtmosphericDensity; }
        }

        /// <summary>
        /// The radiation dose rate at the vehicle, in sieverts per second. Includes any solar
        /// particle event under way.
        /// </summary>
        [KRPCProperty]
        public double DoseRate {
            get {
                // As the game's dose gauge computes it
                var v = InternalVehicle;
                var position = v.GetPositionEcl ();
                var rates = Radiation.ComputeDoseRates (v.Parent, v.GetPositionCci (), position);
                var events = ParticleEvents.ComputeDoseRate (position, v.Parent, rates.StarExposure, Universe.GetElapsedSeconds (), doseTerms);
                return (rates.Total + events).Value ();
            }
        }

        // The game's cache for its orbit dose gauge, which samples the whole orbit
        static readonly OrbitDoseAverageCache orbitDoseAverages = new OrbitDoseAverageCache ();
        // Scratch space for the particle event dose
        static readonly List<DoseTerm> doseTerms = new List<DoseTerm> ();

        /// <summary>
        /// The radiation dose rate averaged over the vehicle's orbit, in sieverts per second.
        /// Includes any solar particle event under way. Not a number if the body has no
        /// magnetosphere, or the orbit dips below the surface.
        /// </summary>
        [KRPCProperty]
        public double OrbitAverageDoseRate {
            get {
                // As the game's orbit dose gauge computes it
                var v = InternalVehicle;
                if (!Radiation.TryComputeOrbitDoseRates (v, orbitDoseAverages, out var rates))
                    return double.NaN;
                var events = ParticleEvents.ComputeDoseRate (v.GetPositionEcl (), null, rates.StarExposure, Universe.GetElapsedSeconds (), doseTerms);
                return (rates.Total + events).Value ();
            }
        }

        /// <summary>
        /// The dynamic pressure acting on the vehicle, in pascals.
        /// </summary>
        [KRPCProperty]
        public double DynamicPressure {
            get { return PhysicalAtmosphereReference.GetDynamicPressure (InternalVehicle); }
        }

        /// <summary>
        /// The acceleration of the vehicle, excluding gravity, in multiples of standard
        /// gravity.
        /// </summary>
        [KRPCProperty]
        public double GForce {
            get { return InternalVehicle.AccelerationBody.Length () / Engines.G0; }
        }

        /// <summary>
        /// The altitude above the terrain, in meters. Over the sea, this is the altitude above
        /// the sea floor.
        /// </summary>
        [KRPCProperty]
        public double BedrockAltitude {
            get { return MeanAltitude - Elevation; }
        }

        /// <summary>
        /// The height of the terrain below the vehicle, in meters above the mean radius of the
        /// body. Below the sea, this is the height of the sea floor.
        /// </summary>
        [KRPCProperty]
        public double Elevation {
            get {
                var v = InternalVehicle;
                if (v.Orbit.Parent is not Celestial celestial)
                    return 0;
                return celestial.GetTerrainHeightFromDirCcf (PositionCcf.NormalizeOrZero ());
            }
        }

        /// <summary>
        /// The time until the vehicle hits the surface on its current trajectory, in seconds.
        /// Not a number if its trajectory does not hit the surface.
        /// </summary>
        /// <remarks>
        /// The game predicts the trajectory on a worker thread, so a change to it shows here
        /// shortly after.
        /// </remarks>
        [KRPCProperty]
        public double TimeToImpact {
            get {
                var patch = InternalVehicle.FlightPlan.Patches[0];
                if (patch.EndTransition != PatchTransition.Impact)
                    return double.NaN;
                return (patch.EndTime - Universe.GetElapsedTime ()).Seconds ();
            }
        }

        /// <summary>
        /// The angle between the direction the vehicle points in and its velocity relative to
        /// the surface, in the vehicle's pitch plane, in degrees. Positive when the nose is
        /// above the velocity.
        /// </summary>
        [KRPCProperty]
        public double AngleOfAttack {
            get {
                var velocity = SurfaceVelocityHelm;
                return velocity == double3.Zero ? 0 : RadiansToDegrees (Math.Atan2 (velocity.Z, velocity.X));
            }
        }

        /// <summary>
        /// The angle between the direction the vehicle points in and its velocity relative to
        /// the surface, in the vehicle's yaw plane, in degrees. Positive when the nose is to
        /// the right of the velocity.
        /// </summary>
        [KRPCProperty]
        public double SideslipAngle {
            get {
                var velocity = SurfaceVelocityHelm;
                return velocity == double3.Zero ? 0 : RadiansToDegrees (Math.Atan2 (-velocity.Y, velocity.X));
            }
        }

        /// <summary>
        /// The unit vector of the vehicle's velocity relative to the surface.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the direction.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Prograde (ReferenceFrame referenceFrame)
        {
            return SurfaceDirection (double3.UnitX, referenceFrame);
        }

        /// <summary>
        /// The unit vector normal to the plane of the vehicle's position and its velocity
        /// relative to the surface.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the direction.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Normal (ReferenceFrame referenceFrame)
        {
            return SurfaceDirection (double3.UnitY, referenceFrame);
        }

        /// <summary>
        /// The unit vector pointing radially outward, perpendicular to <see cref="Prograde"/>
        /// and <see cref="Normal"/>.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the direction.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Radial (ReferenceFrame referenceFrame)
        {
            return SurfaceDirection (double3.UnitZ, referenceFrame);
        }

        // An axis of the prograde, normal and radial frame of the velocity relative to the
        // surface, as the orbital reference frame is for the orbital velocity
        Tuple<double, double, double> SurfaceDirection (double3 axis, ReferenceFrame referenceFrame)
        {
            var v = InternalVehicle;
            var vlf2Cci = VehicleReferenceFrameEx.GetVlf2Cci (v.GetPositionCci (), v.GetSurfaceVelocityCci ())
                ?? throw new InvalidOperationException ("The vehicle is not moving relative to the surface");
            var direction = axis.Transform (doubleQuat.Concatenate (vlf2Cci, v.Orbit.Parent.GetCci2Cce ()));
            return Vectors.ToTuple (referenceFrame.DirectionFromEcl (direction));
        }

        // The velocity relative to the surface in the vehicle's frame, with x forward, y right
        // and z down
        double3 SurfaceVelocityHelm {
            get {
                var v = InternalVehicle;
                return v.GetSurfaceVelocityCci ().Transform (v.GetHelm2Cci ().Inverse ());
            }
        }

        static double RadiansToDegrees (double radians)
        {
            return radians * (180d / Math.PI);
        }
    }
}
