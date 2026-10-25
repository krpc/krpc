using System;
using System.Collections.Generic;
using System.Linq;
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
        /// The bodies that orbit this body.
        /// </summary>
        [KRPCProperty]
        public IList<Body> Satellites {
            get { return InternalBody.Children.OfType<IParentBody> ().Select (body => new Body (body)).ToList (); }
        }
    }
}
