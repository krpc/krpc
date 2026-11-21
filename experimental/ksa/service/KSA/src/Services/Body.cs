using System;
using System.Collections.Generic;
using System.Linq;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A planet, moon or star. See <see cref="KSA.Bodies"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Body : Equatable<Body>, IGameObjectState
    {
        internal Body (IParentBody body)
        {
            InternalBody = body ?? throw new ArgumentNullException (nameof (body));
        }

        /// <summary>
        /// The KSA body.
        /// </summary>
        public IParentBody InternalBody { get; }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Body other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (InternalBody, other.InternalBody);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Vehicles.StateOf (InternalBody); }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return InternalBody.GetHashCode ();
        }

        /// <summary>
        /// The name of the body.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalBody.Id; }
        }

        /// <summary>
        /// The mass of the body, in kilograms.
        /// </summary>
        [KRPCProperty]
        public double Mass {
            get { return InternalBody.Mass; }
        }

        /// <summary>
        /// The standard gravitational parameter of the body, in cubic meters per second squared.
        /// </summary>
        [KRPCProperty]
        public double GravitationalParameter {
            get { return InternalBody.Mu; }
        }

        /// <summary>
        /// The radius of the body, in meters.
        /// </summary>
        [KRPCProperty]
        public double Radius {
            get { return InternalBody.MeanRadius; }
        }

        /// <summary>
        /// The radius of the top of the atmosphere, in meters. Zero for a body with no
        /// atmosphere.
        /// </summary>
        [KRPCProperty]
        public double AtmosphereRadius {
            get { return InternalBody.GetAtmosphereRadius (); }
        }

        /// <summary>
        /// Whether the body is a star.
        /// </summary>
        [KRPCProperty]
        public bool IsStar {
            get { return InternalBody is Astronomical astronomical && astronomical.IsStar (); }
        }

        PhysicalAtmosphere Atmosphere {
            get { return InternalBody.GetPhysicalAtmosphere (); }
        }

        /// <summary>
        /// Whether the body has an atmosphere.
        /// </summary>
        [KRPCProperty]
        public bool HasAtmosphere {
            get { return Atmosphere != null; }
        }

        /// <summary>
        /// The atmospheric pressure at the body's mean radius, in pascals. Zero for a body
        /// with no atmosphere.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelPressure {
            get { return PressureAt (0); }
        }

        /// <summary>
        /// The density of the atmosphere at the body's mean radius, in kilograms per cubic
        /// meter. Zero for a body with no atmosphere.
        /// </summary>
        [KRPCProperty]
        public float SeaLevelDensity {
            get { return DensityAt (0); }
        }

        /// <summary>
        /// The height over which the atmospheric pressure falls by a factor of <c>e</c> at
        /// sea level, in meters. Zero for a body with no atmosphere.
        /// </summary>
        [KRPCProperty]
        public float ScaleHeight {
            get {
                // Hydrostatic scale height, p / (rho g)
                if (Atmosphere == null)
                    return 0;
                return SeaLevelPressure / (SeaLevelDensity * SurfaceGravity);
            }
        }

        /// <summary>
        /// The atmospheric pressure at an altitude, in pascals. Zero for a body with no
        /// atmosphere.
        /// </summary>
        /// <param name="altitude">Altitude above the body's mean radius, in meters.</param>
        [KRPCMethod]
        public float PressureAt (double altitude)
        {
            return (float)(Atmosphere?.ComputePressureAtAltitude (altitude) ?? 0);
        }

        /// <summary>
        /// The density of the atmosphere at an altitude, in kilograms per cubic meter. Zero
        /// for a body with no atmosphere.
        /// </summary>
        /// <param name="altitude">Altitude above the body's mean radius, in meters.</param>
        [KRPCMethod]
        public float DensityAt (double altitude)
        {
            return (float)(Atmosphere?.ComputeDensityAtAltitude (altitude) ?? 0);
        }

        /// <summary>
        /// The temperature of the atmosphere at an altitude, in kelvin. Zero for a body with
        /// no atmosphere, or above the atmosphere.
        /// </summary>
        /// <param name="altitude">Altitude above the body's mean radius, in meters.</param>
        [KRPCMethod]
        public float TemperatureAt (double altitude)
        {
            var atmosphere = AtmosphereAt (altitude);
            return atmosphere == null ? 0 : (float)atmosphere.ComputeTemperatureAtAltitude (altitude);
        }

        /// <summary>
        /// The speed of sound in the atmosphere at an altitude, in meters per second. Zero for
        /// a body with no atmosphere, or above the atmosphere.
        /// </summary>
        /// <param name="altitude">Altitude above the body's mean radius, in meters.</param>
        [KRPCMethod]
        public float SpeedOfSoundAt (double altitude)
        {
            var atmosphere = AtmosphereAt (altitude);
            if (atmosphere == null)
                return 0;
            return (float)atmosphere.Mixture.ComputeSpeedOfSound (atmosphere.ComputeTemperatureAtAltitude (altitude));
        }

        // The atmosphere at an altitude, or null at or above its top, as in Flight
        PhysicalAtmosphere AtmosphereAt (double altitude)
        {
            var atmosphere = Atmosphere;
            return atmosphere != null && altitude < atmosphere.Height ? atmosphere : null;
        }

        /// <summary>
        /// The fraction of the atmosphere that is oxygen, by volume. Zero for a body with no
        /// atmosphere.
        /// </summary>
        [KRPCProperty]
        public float AtmosphereOxygenFraction {
            get { return (float)(Atmosphere?.OxygenVolumeFraction ?? 0); }
        }

        /// <summary>
        /// The mean molar mass of the atmosphere, in kilograms per mole. Zero for a body with
        /// no atmosphere.
        /// </summary>
        [KRPCProperty]
        public float AtmosphereMolarMass {
            get { return (float)(Atmosphere?.Mixture.MolarMass ?? 0); }
        }

        /// <summary>
        /// Whether the body has an ocean.
        /// </summary>
        [KRPCProperty]
        public bool HasOcean {
            get { return InternalBody.GetOceanReference () != null; }
        }

        /// <summary>
        /// The density of the ocean, in kilograms per cubic meter. Zero for a body with no
        /// ocean.
        /// </summary>
        [KRPCProperty]
        public float OceanDensity {
            get { return (float)(InternalBody.GetOceanReference ()?.Density.KgPerM3 ?? 0); }
        }

        // The geomagnetic storm of a body with a magnetosphere driven by the solar wind, or null
        Storm? GeomagneticStorm {
            get {
                if (!(InternalBody is Celestial celestial))
                    return null;
                var magnetosphere = celestial.BodyTemplate?.MagnetosphereReference;
                // The game's space weather window skips magnetospheres driven by corotating flow,
                // such as a gas giant's
                if (magnetosphere == null || magnetosphere.CorotatingFlowPressure != null)
                    return null;
                return StormState.Get (celestial, Universe.GetElapsedSeconds ());
            }
        }

        /// <summary>
        /// The level of the geomagnetic storm at the body, from 0 for none to 5 for the most
        /// severe. The G level of the game's space weather window. Zero for a body with no
        /// magnetosphere.
        /// </summary>
        [KRPCProperty]
        public int GeomagneticStormLevel {
            get {
                var storm = GeomagneticStorm;
                return storm.HasValue ? SpaceWeatherAlerts.GetGLevel (storm.Value.RingCurrent) : 0;
            }
        }

        /// <summary>
        /// The disturbance storm time index of the body, in teslas. It falls below zero in a
        /// geomagnetic storm. Zero for a body with no magnetosphere.
        /// </summary>
        [KRPCProperty]
        public float DstIndex {
            get { return (float)((GeomagneticStorm?.RingCurrent ?? 0) * 1e-9); }
        }

        global::KSA.SpaceWeather SpaceWeather {
            get { return (InternalBody as StellarBody)?.BodyTemplate.SpaceWeather; }
        }

        /// <summary>
        /// The activity of a star, from 0 at the minimum of its cycle to 1 at the maximum.
        /// Zero for a body that is not a star.
        /// </summary>
        [KRPCProperty]
        public float SolarActivity {
            get { return (float)(SpaceWeather?.GetActivity (Universe.GetElapsedSeconds ()) ?? 0); }
        }

        /// <summary>
        /// Whether the activity of a star is rising toward the maximum of its cycle.
        /// False for a body that is not a star, or a star with no cycle.
        /// </summary>
        [KRPCProperty]
        public bool SolarActivityRising {
            get {
                var cycle = SpaceWeather?.Cycle;
                if (cycle == null)
                    return false;
                cycle.GetTimeToTurn (Universe.GetElapsedSeconds (), out var rising);
                return rising;
            }
        }

        /// <summary>
        /// The time until the activity of a star reaches the maximum or minimum of its cycle,
        /// in seconds. Not a number for a body that is not a star, or a star with no cycle.
        /// </summary>
        [KRPCProperty]
        public float TimeToSolarActivityTurn {
            get {
                var cycle = SpaceWeather?.Cycle;
                if (cycle == null)
                    return float.NaN;
                return (float)cycle.GetTimeToTurn (Universe.GetElapsedSeconds (), out _);
            }
        }

        /// <summary>
        /// The mean number of coronal mass ejections a star throws out per day, at its
        /// current activity. Zero for a body that is not a star.
        /// </summary>
        [KRPCProperty]
        public float CoronalMassEjectionsPerDay {
            get { return (float)(SpaceWeather?.GetCmesPerDay (Universe.GetElapsedSeconds ()) ?? 0); }
        }

        static readonly List<ActiveRegion> activeRegions = new List<ActiveRegion> ();

        /// <summary>
        /// The number of active regions on a star, the sources of its eruptions. Zero for a
        /// body that is not a star.
        /// </summary>
        [KRPCProperty]
        public int ActiveRegionCount {
            get {
                var weather = SpaceWeather;
                if (weather == null)
                    return 0;
                activeRegions.Clear ();
                EruptionHistory.GetRegions (weather, Universe.GetElapsedSeconds (), activeRegions);
                return activeRegions.Count;
            }
        }

        /// <summary>
        /// The fast coronal mass ejections a star threw out in the last three days, as the
        /// game's space weather window lists them. Empty for a body that is not a star.
        /// </summary>
        [KRPCProperty]
        public IList<CoronalMassEjection> RecentCoronalMassEjections {
            get {
                if (!(InternalBody is StellarBody star))
                    return new List<CoronalMassEjection> ();
                return CoronalMassEjection.Recent (star);
            }
        }

        /// <summary>
        /// The acceleration due to gravity at the body's mean radius, in meters per second
        /// squared.
        /// </summary>
        [KRPCProperty]
        public float SurfaceGravity {
            get { return (float)(InternalBody.Mu / (InternalBody.MeanRadius * InternalBody.MeanRadius)); }
        }

        /// <summary>
        /// The rate at which the body rotates about its axis, in radians per second.
        /// </summary>
        [KRPCProperty]
        public float RotationalSpeed {
            get { return (float)InternalBody.GetAngularVelocity (); }
        }

        /// <summary>
        /// The time the body takes to rotate once about its axis, relative to the stars, in
        /// seconds. Infinite for a body that does not rotate.
        /// </summary>
        [KRPCProperty]
        public float RotationalPeriod {
            get {
                var speed = Math.Abs (InternalBody.GetAngularVelocity ());
                return (float)(speed > 0 ? 2 * Math.PI / speed : double.PositiveInfinity);
            }
        }

        /// <summary>
        /// The radius of the body's sphere of influence, in meters. Not a number for a body
        /// that orbits nothing.
        /// </summary>
        [KRPCProperty]
        public double SphereOfInfluence {
            get { return InternalBody.SphereOfInfluence; }
        }

        /// <summary>
        /// The body this body orbits, or <c>null</c> if it orbits nothing.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Body Parent {
            get {
                var parent = (InternalBody as IOrbiter)?.Orbit?.Parent;
                return parent == null ? null : new Body (parent);
            }
        }

        /// <summary>
        /// The orbit of the body, or <c>null</c> if it orbits nothing.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Orbit Orbit {
            get {
                var orbit = (InternalBody as IOrbiter)?.Orbit;
                return orbit == null ? null : new Orbit (orbit);
            }
        }

        /// <summary>
        /// The bodies that orbit this body.
        /// </summary>
        [KRPCProperty]
        public IList<Body> Satellites {
            get { return InternalBody.Children.OfType<IParentBody> ().Select (body => new Body (body)).ToList (); }
        }

        /// <summary>
        /// The names of the launch sites on the body. See <see cref="KSA.LaunchVehicle"/>.
        /// </summary>
        [KRPCProperty]
        public IList<string> LaunchSites {
            get {
                if (InternalBody is not Celestial celestial)
                    return new List<string> ();
                return celestial.BodyTemplate.Locations.OfType<LandmarkReference> ()
                    .Where (landmark => landmark.IsLaunchPad).Select (landmark => landmark.Key).ToList ();
            }
        }

        /// <summary>
        /// The reference frame fixed to the body, with its origin at the body's center. The
        /// z axis points to the north pole, the x axis to latitude and longitude zero, and the
        /// y axis to latitude zero and longitude 90 degrees east. The frame rotates with the
        /// body.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return new ReferenceFrame (ReferenceFrame.Kind.Body, InternalBody); }
        }

        /// <summary>
        /// The reference frame with its origin at the body's center that does not rotate. The
        /// z axis points to the north pole, and the x and y axes are fixed relative to the
        /// stars.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame NonRotatingReferenceFrame {
            get { return new ReferenceFrame (ReferenceFrame.Kind.BodyNonRotating, InternalBody); }
        }

        /// <summary>
        /// The position of the center of the body, in meters.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Position (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.PositionFromEcl (InternalBody.GetPositionEcl ()));
        }

        /// <summary>
        /// The velocity of the body, in meters per second.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> Velocity (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.VelocityFromEcl (InternalBody.GetPositionEcl (), InternalBody.GetVelocityEcl ()));
        }

        /// <summary>
        /// The rotation of <see cref="ReferenceFrame"/> relative to the given frame.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the rotation.</param>
        [KRPCMethod]
        public Tuple<double, double, double, double> Rotation (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.RotationFromEcl (InternalBody.GetCcf2Cce ()));
        }

        /// <summary>
        /// The angular velocity of the body, in radians per second. The vector points along
        /// the axis of rotation, and its length is the rate of rotation.
        /// </summary>
        /// <param name="referenceFrame">The reference frame of the angular velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> AngularVelocity (ReferenceFrame referenceFrame)
        {
            return Vectors.ToTuple (referenceFrame.AngularVelocityFromEcl (InternalBody.GetAngularVelocityCce ()));
        }

        /// <summary>
        /// The height of the terrain or the sea surface, whichever is higher, at a point on
        /// the body, in meters above the body's mean radius.
        /// </summary>
        /// <param name="latitude">Latitude, in degrees.</param>
        /// <param name="longitude">Longitude, in degrees.</param>
        [KRPCMethod]
        public double SurfaceHeight (double latitude, double longitude)
        {
            var height = BedrockHeight (latitude, longitude);
            var ocean = (InternalBody as Celestial)?.BodyTemplate.OceanReference;
            return ocean == null ? height : Math.Max (height, ocean.Level);
        }

        /// <summary>
        /// The height of the terrain at a point on the body, in meters above the body's mean
        /// radius. Below the sea, this is the height of the sea floor.
        /// </summary>
        /// <param name="latitude">Latitude, in degrees.</param>
        /// <param name="longitude">Longitude, in degrees.</param>
        /// <remarks>
        /// Zero for a body with no terrain, such as a gas giant.
        /// </remarks>
        [KRPCMethod]
        public double BedrockHeight (double latitude, double longitude)
        {
            if (InternalBody is not Celestial celestial)
                return 0;
            return celestial.GetTerrainHeightFromDirCcf (celestial.GetDirCcfFromLatLon (latitude, longitude));
        }

        /// <summary>
        /// The names of the body's biomes. Empty for a body with no biomes.
        /// </summary>
        [KRPCProperty]
        public IList<string> Biomes {
            get {
                var biomes = (InternalBody as Celestial)?.BodyTemplate.BiomesReference;
                if (biomes == null)
                    return new List<string> ();
                return biomes.Biomes.Select (biome => biome.Key).ToList ();
            }
        }

        /// <summary>
        /// The biome at a point on the body, or <c>null</c> if the body has no biomes.
        /// </summary>
        /// <param name="latitude">Latitude, in degrees.</param>
        /// <param name="longitude">Longitude, in degrees.</param>
        [KRPCMethod (Nullable = true)]
        public string BiomeAt (double latitude, double longitude)
        {
            if (InternalBody is not Celestial celestial)
                return null;
            return BiomeAt (celestial, celestial.GetDirCcfFromLatLon (latitude, longitude));
        }

        // The biome at a direction in the body's rotating frame. Up to four biomes blend at
        // each point, and we take the one with the highest weight
        internal static string BiomeAt (Celestial celestial, double3 directionCcf)
        {
            var biomes = celestial.BodyTemplate.BiomesReference;
            if (biomes == null)
                return null;
            var ids = celestial.SampleBiomeIdMap (directionCcf);
            var weights = celestial.SampleBiomeControlMap (directionCcf);
            var id = ids.R;
            var weight = weights.R;
            if (weights.G > weight) {
                id = ids.G;
                weight = weights.G;
            }
            if (weights.B > weight) {
                id = ids.B;
                weight = weights.B;
            }
            if (weights.A > weight)
                id = ids.A;
            return biomes.TryGetBiomeAlias (id);
        }

        // A position in the body's rotating frame
        double3 PositionCcf (Tuple<double, double, double> position, ReferenceFrame referenceFrame)
        {
            var positionCce = referenceFrame.PositionToEcl (Vectors.ToVector (position)) - InternalBody.GetPositionEcl ();
            return positionCce.Transform (InternalBody.GetCcf2Cce ().Inverse ());
        }

        /// <summary>
        /// The latitude of a position, in degrees.
        /// </summary>
        /// <param name="position">Position, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public double LatitudeAtPosition (Tuple<double, double, double> position, ReferenceFrame referenceFrame)
        {
            return Celestial.GetLatitudeFromCcf (PositionCcf (position, referenceFrame));
        }

        /// <summary>
        /// The longitude of a position, in degrees.
        /// </summary>
        /// <param name="position">Position, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public double LongitudeAtPosition (Tuple<double, double, double> position, ReferenceFrame referenceFrame)
        {
            return Celestial.GetLongitudeFromCcf (PositionCcf (position, referenceFrame));
        }

        /// <summary>
        /// The altitude of a position above the body's mean radius, in meters.
        /// </summary>
        /// <param name="position">Position, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public double AltitudeAtPosition (Tuple<double, double, double> position, ReferenceFrame referenceFrame)
        {
            return PositionCcf (position, referenceFrame).Length () - InternalBody.MeanRadius;
        }

        /// <summary>
        /// The position of a point on the surface, at the body's mean radius, in meters.
        /// </summary>
        /// <param name="latitude">Latitude, in degrees.</param>
        /// <param name="longitude">Longitude, in degrees.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public Tuple<double, double, double> SurfacePosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            var lat = latitude * (Math.PI / 180d);
            var lon = longitude * (Math.PI / 180d);
            var positionCcf = new double3 (Math.Cos (lat) * Math.Cos (lon), Math.Cos (lat) * Math.Sin (lon), Math.Sin (lat)) * InternalBody.MeanRadius;
            var positionEcl = InternalBody.GetPositionEcl () + positionCcf.Transform (InternalBody.GetCcf2Cce ());
            return Vectors.ToTuple (referenceFrame.PositionFromEcl (positionEcl));
        }
    }
}
