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
    public class Body : Equatable<Body>
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
        /// The acceleration due to gravity at the body's mean radius, in meters per second
        /// squared.
        /// </summary>
        [KRPCProperty]
        public double SurfaceGravity {
            get { return InternalBody.Mu / (InternalBody.MeanRadius * InternalBody.MeanRadius); }
        }

        /// <summary>
        /// The rate at which the body rotates about its axis, in radians per second.
        /// </summary>
        [KRPCProperty]
        public double RotationalSpeed {
            get { return InternalBody.GetAngularVelocity (); }
        }

        /// <summary>
        /// The time the body takes to rotate once about its axis, relative to the stars, in
        /// seconds. Infinite for a body that does not rotate.
        /// </summary>
        [KRPCProperty]
        public double RotationalPeriod {
            get {
                var speed = Math.Abs (InternalBody.GetAngularVelocity ());
                return speed > 0 ? 2 * Math.PI / speed : double.PositiveInfinity;
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
