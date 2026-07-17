using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A close approach between an orbit and a target orbit. Obtained by calling
    /// <see cref="Orbit.NextClosestApproach"/> or <see cref="Orbit.ClosestApproaches"/>.
    /// </summary>
    /// <remarks>
    /// A close approach is a snapshot: the time of closest approach is estimated once
    /// when the object is created, and every member describes the state at that time.
    /// Relative quantities are the target relative to the orbiting object (target minus
    /// self).
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class ClosestApproach : Equatable<ClosestApproach>
    {
        readonly Orbit orbit;
        readonly Orbit target;
        readonly double ut;
        readonly double distance;

        internal ClosestApproach (Orbit orbit, Orbit target, double beginTime)
        {
            if (ReferenceEquals (orbit, null))
                throw new ArgumentNullException (nameof (orbit));
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
            this.orbit = orbit;
            this.target = target;
            ut = Orbit.CalcClosestAproach (orbit, target, beginTime, out distance);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ClosestApproach other)
        {
            return !ReferenceEquals (other, null) &&
                   orbit.Equals (other.orbit) &&
                   target.Equals (other.target) &&
                   ut == other.ut;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return orbit.GetHashCode () ^ target.GetHashCode () ^ ut.GetHashCode ();
        }

        // The world-space position of the given orbit at the closest approach.
        Vector3d WorldPosition (Orbit o)
        {
            return o.InternalOrbit.getPositionAtUT (ut);
        }

        // The true world-space velocity of the given orbit at the closest approach,
        // including the motion of its reference body, so relative velocity is correct
        // even when the two orbits are around different bodies.
        Vector3d WorldVelocity (Orbit o)
        {
            return o.InternalOrbit.GetFrameVelAtUT (ut).SwapYZ ();
        }

        ReferenceFrame DefaultedFrame (ReferenceFrame referenceFrame)
        {
            return ReferenceEquals (referenceFrame, null) ? orbit.DefaultReferenceFrame : referenceFrame;
        }

        /// <summary>
        /// The universal time of the closest approach, in seconds.
        /// </summary>
        [KRPCProperty]
        public double UT {
            get { return ut; }
        }

        /// <summary>
        /// The time until the closest approach, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeTo {
            get { return ut - SpaceCenter.UT; }
        }

        /// <summary>
        /// The distance between the objects at the closest approach, in meters.
        /// </summary>
        [KRPCProperty]
        public double Distance {
            get { return distance; }
        }

        /// <summary>
        /// The relative speed of the objects at the closest approach, in meters per
        /// second. This is the magnitude of <see cref="RelativeVelocity"/>, and does
        /// not depend on the choice of reference frame.
        /// </summary>
        [KRPCProperty]
        public double RelativeSpeed {
            get { return (WorldVelocity (target) - WorldVelocity (orbit)).magnitude; }
        }

        /// <summary>
        /// The vessel doing the approaching, or <c>null</c> if it is not a vessel.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Vessel Vessel {
            get { return orbit.OwnerVessel; }
        }

        /// <summary>
        /// The celestial body doing the approaching, or <c>null</c> if it is not a
        /// celestial body.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public CelestialBody Body {
            get { return orbit.OwnerBody; }
        }

        /// <summary>
        /// The vessel being approached, or <c>null</c> if the target is not a vessel.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Vessel TargetVessel {
            get { return target.OwnerVessel; }
        }

        /// <summary>
        /// The celestial body being approached, or <c>null</c> if the target is not a
        /// celestial body.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public CelestialBody TargetBody {
            get { return target.OwnerBody; }
        }

        /// <summary>
        /// The position of the orbiting object at the closest approach.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned position
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 Position ([KRPCNullable] ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.PositionFromWorldSpace (WorldPosition (orbit)).ToTuple ();
        }

        /// <summary>
        /// The position of the target object at the closest approach.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned position
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 TargetPosition ([KRPCNullable] ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.PositionFromWorldSpace (WorldPosition (target)).ToTuple ();
        }

        /// <summary>
        /// The velocity of the orbiting object at the closest approach.
        /// </summary>
        /// <returns>The velocity as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned velocity
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 Velocity ([KRPCNullable] ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.VelocityFromWorldSpace (WorldPosition (orbit), WorldVelocity (orbit)).ToTuple ();
        }

        /// <summary>
        /// The velocity of the target object at the closest approach.
        /// </summary>
        /// <returns>The velocity as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned velocity
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 TargetVelocity ([KRPCNullable] ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.VelocityFromWorldSpace (WorldPosition (target), WorldVelocity (target)).ToTuple ();
        }

        /// <summary>
        /// The position of the target relative to the orbiting object at the closest
        /// approach.
        /// </summary>
        /// <returns>The relative position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame whose axes the returned
        /// vector is expressed in. Defaults to the orbital reference frame of the object
        /// the orbit belongs to.</param>
        [KRPCMethod]
        public Tuple3 RelativePosition ([KRPCNullable] ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.DirectionFromWorldSpace (WorldPosition (target) - WorldPosition (orbit)).ToTuple ();
        }

        /// <summary>
        /// The velocity of the target relative to the orbiting object at the closest
        /// approach.
        /// </summary>
        /// <returns>The relative velocity as a vector.</returns>
        /// <param name="referenceFrame">The reference frame whose axes the returned
        /// vector is expressed in. Defaults to the orbital reference frame of the object
        /// the orbit belongs to.</param>
        [KRPCMethod]
        public Tuple3 RelativeVelocity ([KRPCNullable] ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.DirectionFromWorldSpace (WorldVelocity (target) - WorldVelocity (orbit)).ToTuple ();
        }
    }
}
