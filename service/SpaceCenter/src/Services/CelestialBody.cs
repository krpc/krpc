using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a celestial body (such as a planet or moon).
    /// See <see cref="SpaceCenter.Bodies"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CelestialBody : Equatable<CelestialBody>
    {
        Orbit orbit;

        /// <summary>
        /// Construct from a KSP celestial body object.
        /// </summary>
        public CelestialBody (global::CelestialBody body)
        {
            InternalBody = body;
            if (body.name != Planetarium.fetch.Sun.name)
                orbit = new Orbit (body);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CelestialBody other)
        {
            return !ReferenceEquals (other, null) && InternalBody == other.InternalBody;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return InternalBody.GetHashCode ();
        }

        /// <summary>
        /// The KSP celestial body object.
        /// </summary>
        public global::CelestialBody InternalBody { get; private set; }

        /// <summary>
        /// The name of the body.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalBody.name; }
        }

        /// <summary>
        /// A list of celestial bodies that are in orbit around this celestial body.
        /// </summary>
        [KRPCProperty]
        public IList<CelestialBody> Satellites {
            get {
                var allBodies = SpaceCenter.Bodies;
                var bodies = new List<CelestialBody> ();
                foreach (var body in InternalBody.orbitingBodies) {
                    bodies.Add (allBodies [body.name]);
                }
                return bodies;
            }
        }

        /// <summary>
        /// The mass of the body, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get { return (float)InternalBody.Mass; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Standard_gravitational_parameter">standard
        /// gravitational parameter</a> of the body in <math>m^3s^{-2}</math>.
        /// </summary>
        [KRPCProperty]
        public float GravitationalParameter {
            get { return (float)InternalBody.gravParameter; }
        }

        /// <summary>
        /// The acceleration due to gravity at sea level (mean altitude) on the body,
        /// in <math>m/s^2</math>.
        /// </summary>
        [KRPCProperty]
        public float SurfaceGravity {
            get { return (float)InternalBody.GeeASL * 9.81f; }
        }

        /// <summary>
        /// The sidereal rotational period of the body, in seconds.
        /// </summary>
        [KRPCProperty]
        public float RotationalPeriod {
            get { return (float)InternalBody.rotationPeriod; }
        }

        /// <summary>
        /// The rotational speed of the body, in radians per second.
        /// </summary>
        [KRPCProperty]
        public float RotationalSpeed {
            get { return (float)(2f * Math.PI) / RotationalPeriod; }
        }

        /// <summary>
        /// The current rotation angle of the body, in radians.
        /// A value between 0 and <math>2\pi</math>
        /// </summary>
        [KRPCProperty]
        public double RotationAngle {
            get { return GeometryExtensions.ToRadians (GeometryExtensions.ClampAngle360 (InternalBody.rotationAngle)); }
        }

        /// <summary>
        /// The initial rotation angle of the body (at UT 0), in radians.
        /// A value between 0 and <math>2\pi</math>
        /// </summary>
        [KRPCProperty]
        public double InitialRotation {
            get { return GeometryExtensions.ToRadians (GeometryExtensions.ClampAngle360 (InternalBody.initialRotation)); }
        }

        /// <summary>
        /// The equatorial radius of the body, in meters.
        /// </summary>
        [KRPCProperty]
        public float EquatorialRadius {
            get { return (float)InternalBody.Radius; }
        }

        /// <summary>
        /// The height of the surface relative to mean sea level, in meters,
        /// at the given position. When over water this is equal to 0.
        /// </summary>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        [KRPCMethod]
        public double SurfaceHeight (double latitude, double longitude)
        {
            var alt = Math.Max (0, BedrockHeight (latitude, longitude));
            // Using raycast to find real surface height.
            const double raySource = 1000;
            const double raySecondPoint = 500;
            Vector3d rayCastStart = InternalBody.GetWorldSurfacePosition(latitude, longitude, alt + raySource);
            Vector3d rayCastStop = InternalBody.GetWorldSurfacePosition(latitude, longitude, alt + raySecondPoint);
            RaycastHit hit;
            //Casting a ray on the surface (layer 15 in KSP).
            if (Physics.Raycast(rayCastStart, (rayCastStop - rayCastStart), out hit, float.MaxValue, 1 << 15))

            {
                // Ensure hit is on the topside of planet, near the rayCastStart, not on the far side.
                if (Mathf.Abs(hit.distance) < 3000)
                {
                    // Okay, a hit was found, use it instead of PQS alt:
                    alt = alt + raySource - hit.distance;
                }
            }
            return alt;
        }

        /// <summary>
        /// The height of the surface relative to mean sea level, in meters,
        /// at the given position. When over water, this is the height
        /// of the sea-bed and is therefore  negative value.
        /// </summary>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        [KRPCMethod]
        public double BedrockHeight (double latitude, double longitude)
        {
            if (InternalBody.pqsController == null)
                return 0;
            var latitudeRadians = GeometryExtensions.ToRadians (latitude);
            var longitudeRadians = GeometryExtensions.ToRadians (longitude);
            var cosLatitude = Math.Cos (latitudeRadians);
            var sinLatitude = Math.Sin (latitudeRadians);
            var cosLongitude = Math.Cos (longitudeRadians);
            var sinLongitude = Math.Sin (longitudeRadians);
            var position = new Vector3d (cosLatitude * cosLongitude, sinLatitude, cosLatitude * sinLongitude);
            return InternalBody.pqsController.GetSurfaceHeight (position) - InternalBody.pqsController.radius;
        }

        /// <summary>
        /// The position at mean sea level at the given latitude and longitude,
        /// in the given reference frame.
        /// </summary>
        /// <returns>Position as a vector.</returns>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector.</param>
        [KRPCMethod]
        public Tuple3 MSLPosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, 0, referenceFrame);
        }

        /// <summary>
        /// The position of the surface at the given latitude and longitude, in the given
        /// reference frame. When over water, this is the position of the surface of the water.
        /// </summary>
        /// <returns>Position as a vector.</returns>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector.</param>
        [KRPCMethod]
        public Tuple3 SurfacePosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, SurfaceHeight (latitude, longitude), referenceFrame);
        }

        /// <summary>
        /// The position of the surface at the given latitude and longitude, in the given
        /// reference frame. When over water, this is the position at the bottom of the sea-bed.
        /// </summary>
        /// <returns>Position as a vector.</returns>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector.</param>
        [KRPCMethod]
        public Tuple3 BedrockPosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, BedrockHeight (latitude, longitude), referenceFrame);
        }

        /// <summary>
        /// The position at the given latitude, longitude and altitude, in the given reference frame.
        /// </summary>
        /// <returns>Position as a vector.</returns>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="altitude">Altitude in meters above sea level.</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector.</param>
        [KRPCMethod]
        public Tuple3 PositionAtAltitude (double latitude, double longitude, double altitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, altitude, referenceFrame);
        }

        Tuple3 PositionAt (double latitude, double longitude, double altitude, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var position = InternalBody.GetWorldSurfacePosition (latitude, longitude, altitude);
            return referenceFrame.PositionFromWorldSpace (position).ToTuple ();
        }

        /// <summary>
        /// The latitude of the given position, in the given reference frame.
        /// </summary>
        /// <param name="position">Position as a vector.</param>
        /// <param name="referenceFrame">Reference frame for the position vector.</param>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public double LatitudeAtPosition (Tuple3 position, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals(referenceFrame, null))
                throw new ArgumentNullException(nameof(referenceFrame));
            return InternalBody.GetLatitude(referenceFrame.PositionToWorldSpace(position.ToVector()));
        }

        /// <summary>
        /// The longitude of the given position, in the given reference frame.
        /// </summary>
        /// <param name="position">Position as a vector.</param>
        /// <param name="referenceFrame">Reference frame for the position vector.</param>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public double LongitudeAtPosition (Tuple3 position, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals(referenceFrame, null))
                throw new ArgumentNullException(nameof(referenceFrame));
            return InternalBody.GetLongitude(referenceFrame.PositionToWorldSpace(position.ToVector()));
        }

        /// <summary>
        /// The altitude, in meters, of the given position in the given reference frame.
        /// </summary>
        /// <param name="position">Position as a vector.</param>
        /// <param name="referenceFrame">Reference frame for the position vector.</param>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public double AltitudeAtPosition (Tuple3 position, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals(referenceFrame, null))
                throw new ArgumentNullException(nameof(referenceFrame));
            return InternalBody.GetAltitude(referenceFrame.PositionToWorldSpace(position.ToVector()));
        }

        /// <summary>
        /// The radius of the sphere of influence of the body, in meters.
        /// </summary>
        [KRPCProperty]
        public float SphereOfInfluence {
            get { return (float)InternalBody.sphereOfInfluence; }
        }

        /// <summary>
        /// The orbit of the body.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public Orbit Orbit {
            get { return orbit; }
        }

        /// <summary>
        /// Whether or not the body is a star.
        /// </summary>
        [KRPCProperty]
        public bool IsStar => InternalBody.isStar;

        /// <summary>
        /// Whether or not the body has a solid surface.
        /// </summary>
        [KRPCProperty]
        public bool HasSolidSurface => InternalBody.hasSolidSurface;

        /// <summary>
        /// <c>true</c> if the body has an atmosphere.
        /// </summary>
        [KRPCProperty]
        public bool HasAtmosphere {
            get { return InternalBody.atmosphere; }
        }

        /// <summary>
        /// The depth of the atmosphere, in meters.
        /// </summary>
        [KRPCProperty]
        public float AtmosphereDepth {
            get { return (float)InternalBody.atmosphereDepth; }
        }

        /// <summary>
        /// The atmospheric density at the given position, in <math>kg/m^3</math>,
        /// in the given reference frame.
        /// </summary>
        /// <param name="position">The position vector at which to measure the density.</param>
        /// <param name="referenceFrame">Reference frame that the position vector is in.</param>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public double AtmosphericDensityAtPosition(Tuple3 position, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals(referenceFrame, null))
                throw new ArgumentNullException(nameof(referenceFrame));
            var worldPosition = referenceFrame.PositionToWorldSpace(position.ToVector());
            var body = InternalBody;
            var altitude = (float) body.GetAltitude(worldPosition);
            var latitude = (float) body.GetLatitude(worldPosition);
            var pressure = FlightGlobals.getStaticPressure(worldPosition);
            var temperature =
                FlightGlobals.getExternalTemperature(altitude, body)
                + body.atmosphereTemperatureSunMultCurve.Evaluate(altitude)
                * (body.latitudeTemperatureBiasCurve.Evaluate(latitude)
                   + body.latitudeTemperatureSunMultCurve.Evaluate(latitude) // fix that 0 into latitude
                   + body.axialTemperatureSunMultCurve.Evaluate(1));
            return FlightGlobals.getAtmDensity(pressure, temperature);
        }

        /// <summary>
        /// <c>true</c> if there is oxygen in the atmosphere, required for air-breathing engines.
        /// </summary>
        [KRPCProperty]
        public bool HasAtmosphericOxygen
        {
            get { return InternalBody.atmosphereContainsOxygen; }
        }

        /// <summary>
        /// The temperature on the body at the given position, in the given reference frame.
        /// </summary>
        /// <param name="position">Position as a vector.</param>
        /// <param name="referenceFrame">The reference frame that the position is in.</param>
        /// <remarks>
        /// This calculation is performed using the bodies current position, which means that
        /// the value could be wrong if you want to know the temperature in the far future.
        /// </remarks>
        [KRPCMethod]
        public double TemperatureAt (Tuple3 position, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return StockAerodynamics.GetTemperature(referenceFrame.PositionToWorldSpace(position.ToVector()), InternalBody);
        }

        /// <summary>
        /// Gets the air density, in <math>kg/m^3</math>, for the specified
        /// altitude above sea level, in meters.
        /// </summary>
        /// <remarks>
        /// This is an approximation, because actual calculations, taking sun exposure into account
        /// to compute air temperature, require us to know the exact point on the body where the
        /// density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference
        /// for trajectory prediction.
        /// </remarks>
        [KRPCMethod]
        public double DensityAt (double altitude)
        {
            return StockAerodynamics.GetDensity (altitude, InternalBody);
        }

        /// <summary>
        /// Gets the air pressure, in Pascals, for the specified
        /// altitude above sea level, in meters.
        /// </summary>
        [KRPCMethod]
        public double PressureAt (double altitude)
        {
            return StockAerodynamics.GetPressure (altitude, InternalBody);
        }

        /// <summary>
        /// The biomes present on this body.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public HashSet<string> Biomes {
            get {
                var body = InternalBody;
                if (body.BiomeMap == null)
                    return new HashSet<string>();
                return new HashSet<string> (body.BiomeMap.Attributes.Select (x => x.name));
            }
        }

        /// <summary>
        /// The biome at the given latitude and longitude, in degrees.
        /// </summary>
        [KRPCMethod]
        public string BiomeAt (double latitude, double longitude)
        {
            CheckHasBiomes ();
            return InternalBody.BiomeMap.GetAtt (latitude, longitude).name;
        }

        void CheckHasBiomes ()
        {
            var body = InternalBody;
            if (body.BiomeMap == null)
                throw new InvalidOperationException ("Body does not have any biomes");
        }

        /// <summary>
        /// The altitude, in meters, above which a vessel is considered to be
        /// flying "high" when doing science.
        /// </summary>
        [KRPCProperty]
        public float FlyingHighAltitudeThreshold {
            get { return InternalBody.scienceValues.flyingAltitudeThreshold; }
        }

        /// <summary>
        /// The altitude, in meters, above which a vessel is considered to be
        /// in "high" space when doing science.
        /// </summary>
        [KRPCProperty]
        public float SpaceHighAltitudeThreshold {
            get { return InternalBody.scienceValues.spaceAltitudeThreshold; }
        }

        /// <summary>
        /// The reference frame that is fixed relative to the celestial body.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of the body.
        /// </description></item>
        /// <item><description>The axes rotate with the body.</description></item>
        /// <item><description>The x-axis points from the center of the body
        /// towards the intersection of the prime meridian and equator (the
        /// position at 0° longitude, 0° latitude).</description></item>
        /// <item><description>The y-axis points from the center of the body
        /// towards the north pole.</description></item>
        /// <item><description>The z-axis points from the center of the body
        /// towards the equator at 90°E longitude.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalBody); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to this celestial body, and
        /// orientated in a fixed direction (it does not rotate with the body).
        /// <list type="bullet">
        /// <item><description>The origin is at the center of the body.</description></item>
        /// <item><description>The axes do not rotate.</description></item>
        /// <item><description>The x-axis points in an arbitrary direction through the
        /// equator.</description></item>
        /// <item><description>The y-axis points from the center of the body towards
        /// the north pole.</description></item>
        /// <item><description>The z-axis points in an arbitrary direction through the
        /// equator.</description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame NonRotatingReferenceFrame {
            get { return ReferenceFrame.NonRotating (InternalBody); }
        }

        /// <summary>
        /// The reference frame that is fixed relative to this celestial body, but
        /// orientated with the body's orbital prograde/normal/radial directions.
        /// <list type="bullet">
        /// <item><description>The origin is at the center of the body.
        /// </description></item>
        /// <item><description>The axes rotate with the orbital prograde/normal/radial
        /// directions.</description></item>
        /// <item><description>The x-axis points in the orbital anti-radial direction.
        /// </description></item>
        /// <item><description>The y-axis points in the orbital prograde direction.
        /// </description></item>
        /// <item><description>The z-axis points in the orbital normal direction.
        /// </description></item>
        /// </list>
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalBody); }
        }

        /// <summary>
        /// The position of the center of the body, in the specified reference frame.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (InternalBody.position).ToTuple ();
        }

        /// <summary>
        /// The linear velocity of the body, in the specified reference frame.
        /// </summary>
        /// <returns>The velocity as a vector. The vector points in the direction of travel,
        /// and its magnitude is the speed of the body in meters per second.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// velocity vector is in.</param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.VelocityFromWorldSpace (InternalBody.position, InternalBody.GetWorldVelocity ()).ToTuple ();
        }

        /// <summary>
        /// The rotation of the body, in the specified reference frame.
        /// </summary>
        /// <returns>The rotation as a quaternion of the form <math>(x, y, z, w)</math>.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// rotation is in.</param>
        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var up = Vector3.up;
            var right = InternalBody.GetRelSurfacePosition (0, 0, 1).normalized;
            var forward = Vector3.Cross (right, up);
            Vector3.OrthoNormalize (ref forward, ref up);
            var rotation = Quaternion.LookRotation (forward, up);
            return referenceFrame.RotationFromWorldSpace (rotation).ToTuple ();
        }

        /// <summary>
        /// The direction in which the north pole of the celestial body is pointing,
        /// in the specified reference frame.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (InternalBody.transform.up).ToTuple ();
        }


        /// <summary>
        /// The angular velocity of the body in the specified reference frame.
        /// </summary>
        /// <returns>The angular velocity as a vector. The magnitude of the vector is the rotational
        /// speed of the body, in radians per second. The direction of the vector indicates the axis
        /// of rotation, using the right-hand rule.</returns>
        /// <param name="referenceFrame">The reference frame the returned
        /// angular velocity is in.</param>
        [KRPCMethod]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.AngularVelocityFromWorldSpace (InternalBody.angularVelocity).ToTuple ();
        }
    }
}
