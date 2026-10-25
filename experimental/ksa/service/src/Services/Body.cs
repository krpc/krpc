using System;
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
    }
}
