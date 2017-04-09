using System;
using System.Collections.Generic;
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
        /// The acceleration due to gravity at sea level (mean altitude) on the body, in <math>m/s^2</math>.
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
        /// The height of the surface relative to mean sea level at the given position,
        /// in meters. When over water this is equal to 0.
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        [KRPCMethod]
        public double SurfaceHeight (double latitude, double longitude)
        {
            return Math.Max (0, BedrockHeight (latitude, longitude));
        }

        /// <summary>
        /// The height of the surface relative to mean sea level at the given position,
        /// in meters. When over water, this is the height of the sea-bed and is therefore a
        /// negative value.
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
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
        /// The position at mean sea level at the given latitude and longitude, in the given reference frame.
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector</param>
        [KRPCMethod]
        public Tuple3 MSLPosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, 0, referenceFrame);
        }

        /// <summary>
        /// The position of the surface at the given latitude and longitude, in the given
        /// reference frame. When over water, this is the position of the surface of the water.
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector</param>
        [KRPCMethod]
        public Tuple3 SurfacePosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, SurfaceHeight (latitude, longitude), referenceFrame);
        }

        /// <summary>
        /// The position of the surface at the given latitude and longitude, in the given
        /// reference frame. When over water, this is the position at the bottom of the sea-bed.
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        /// <param name="referenceFrame">Reference frame for the returned position vector</param>
        [KRPCMethod]
        public Tuple3 BedrockPosition (double latitude, double longitude, ReferenceFrame referenceFrame)
        {
            return PositionAt (latitude, longitude, BedrockHeight (latitude, longitude), referenceFrame);
        }

        Tuple3 PositionAt (double latitude, double longitude, double altitude, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            var position = InternalBody.GetWorldSurfacePosition (latitude, longitude, altitude);
            return referenceFrame.PositionFromWorldSpace (position).ToTuple ();
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
        [KRPCProperty]
        public Orbit Orbit {
            get { return orbit; }
        }

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
        /// <c>true</c> if there is oxygen in the atmosphere, required for air-breathing engines.
        /// </summary>
        [KRPCProperty]
        public bool HasAtmosphericOxygen {
            get { return InternalBody.atmosphereContainsOxygen; }
        }

        /// <summary>
        /// The biomes present on this body.
        /// </summary>
        [KRPCProperty]
        public HashSet<string> Biomes {
            get {
                CheckHasBiomes ();
                return new HashSet<string> (InternalBody.BiomeMap.Attributes.Select (x => x.name));
            }
        }

        /// <summary>
        /// The biomes at the given latitude and longitude, in degrees.
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
        /// The altitude, in meters, above which a vessel is considered to be flying "high" when doing science.
        /// </summary>
        [KRPCProperty]
        public float FlyingHighAltitudeThreshold {
            get { return InternalBody.scienceValues.flyingAltitudeThreshold; }
        }

        /// <summary>
        /// The altitude, in meters, above which a vessel is considered to be in "high" space when doing science.
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
        /// Gets the reference frame that is fixed relative to this celestial body, but
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
        /// Returns the position vector of the center of the body in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace (InternalBody.position).ToTuple ();
        }

        /// <summary>
        /// Returns the velocity vector of the body in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.VelocityFromWorldSpace (InternalBody.position, InternalBody.GetWorldVelocity ()).ToTuple ();
        }

        /// <summary>
        /// Returns the rotation of the body in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
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
        /// Returns the direction in which the north pole of the celestial body is
        /// pointing, as a unit vector, in the specified reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (InternalBody.transform.up).ToTuple ();
        }


        /// <summary>
        /// Returns the angular velocity of the body in the specified reference
        /// frame. The magnitude of the vector is the rotational speed of the body, in
        /// radians per second, and the direction of the vector indicates the axis of
        /// rotation, using the right-hand rule.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.AngularVelocityFromWorldSpace (InternalBody.angularVelocity).ToTuple ();
        }
    }
}
