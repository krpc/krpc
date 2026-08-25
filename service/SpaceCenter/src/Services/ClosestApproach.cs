using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A close approach between an orbit and a target orbit. Obtained by calling
    /// <see cref="Orbit.NextClosestApproach"/> or <see cref="Orbit.ClosestApproaches"/>.
    /// </summary>
    /// <remarks>
    /// The object names the approach rather than describing it once: each member
    /// estimates the time of closest approach from where the two orbits are now, and
    /// describes the state at that time. The estimate moves as the game runs, so
    /// members read at different moments can differ a little; members read in one
    /// physics tick all describe the same estimate. Relative quantities are the target
    /// relative to the orbiting object (target minus self).
    ///
    /// The search runs forward over one orbital period from the current time. Once an
    /// approach has passed and the two orbits are moving apart, the closest point in
    /// that period is the current one, so the object reports the approach as now, at
    /// the present separation, until the orbits bring another one within range.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class ClosestApproach : Equatable<ClosestApproach>, IGameObjectState
    {
        readonly Orbit orbit;
        readonly Orbit target;
        // The index of this approach among the successive ones: the search covers one
        // orbital period, starting this many periods from now
        readonly int orbitsAhead;
        // The approach as it was last solved, with the physics tick and the game state it
        // was solved in.
        float solvedFixedTime = float.NaN;
        uint solvedGeneration;
        double solvedUT;
        double solvedDistance;

        internal ClosestApproach (Orbit orbit, Orbit target, int orbitsAhead)
        {
            if (ReferenceEquals (orbit, null))
                throw new ArgumentNullException (nameof (orbit));
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
            this.orbit = orbit;
            this.target = target;
            this.orbitsAhead = orbitsAhead;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        /// <remarks>
        /// What the object stands for is which approach between the two orbits it is,
        /// so asking for the same one again gives back the same object. The estimated
        /// time of the approach is not part of that: it is read from the orbits and
        /// moves as they do.
        /// </remarks>
        public override bool Equals (ClosestApproach other)
        {
            return !ReferenceEquals (other, null) &&
                   orbit.Equals (other.orbit) &&
                   target.Equals (other.target) &&
                   orbitsAhead == other.orbitsAhead;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (orbit).And (target).And (orbitsAhead);
        }

        /// <summary>
        /// The state of the approach. Every member reads both orbits, so it takes the less
        /// alive of their two states.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return orbit.GameObjectState.LeastAlive (target.GameObjectState); }
        }

        // The time the search for this approach starts from, covering one orbital period
        // of the approaching object. The next approach is searched for from now and needs
        // no period to step by, so a hyperbolic orbit still has one
        double BeginTime {
            get {
                if (orbitsAhead == 0)
                    return SpaceCenter.UT;
                return SpaceCenter.UT + orbitsAhead * orbit.InternalOrbit.period;
            }
        }

        // The universal time of the closest approach, and the distance there, estimated
        // from where the two orbits are now. The estimate searches an orbital period,
        // sampling both orbits at some seventy points, so it is solved once per physics
        // tick and shared by the members read in that tick
        double Solve (out double approachDistance)
        {
            if (solvedFixedTime != Time.fixedTime || solvedGeneration != GameState.Generation) {
                solvedUT = Orbit.CalcClosestAproach (orbit, target, BeginTime, out solvedDistance);
                solvedFixedTime = Time.fixedTime;
                solvedGeneration = GameState.Generation;
            }
            approachDistance = solvedDistance;
            return solvedUT;
        }

        double Solve ()
        {
            double approachDistance;
            return Solve (out approachDistance);
        }

        // The world-space position of the given orbit at the closest approach.
        Vector3d WorldPosition (Orbit o, double ut)
        {
            return o.InternalOrbit.getPositionAtUT (ut);
        }

        // The world-space velocity of the given orbit at the closest approach: the motion
        // around its reference body then, plus the motion of that body now. The body is
        // taken as it is now to match WorldPosition, which places the orbit against the
        // body's current position, and the velocity a reference frame moves at. Both
        // objects are treated the same way, so the relative quantities stay the difference
        // of the absolute ones
        Vector3d WorldVelocity (Orbit o, double ut)
        {
            var internalOrbit = o.InternalOrbit;
            return internalOrbit.getOrbitalVelocityAtUT (ut).SwapYZ () +
                   internalOrbit.referenceBody.GetWorldVelocity ();
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
            get { return Solve (); }
        }

        /// <summary>
        /// The time until the closest approach, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeTo {
            get { return Solve () - SpaceCenter.UT; }
        }

        /// <summary>
        /// The distance between the objects at the closest approach, in meters.
        /// </summary>
        [KRPCProperty]
        public double Distance {
            get {
                double approachDistance;
                Solve (out approachDistance);
                return approachDistance;
            }
        }

        /// <summary>
        /// The relative speed of the objects at the closest approach, in meters per
        /// second. This is the magnitude of <see cref="RelativeVelocity"/>, and does
        /// not depend on the choice of reference frame.
        /// </summary>
        [KRPCProperty]
        public double RelativeSpeed {
            get {
                var ut = Solve ();
                return (WorldVelocity (target, ut) - WorldVelocity (orbit, ut)).magnitude;
            }
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
        public Tuple3 Position (ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.PositionFromWorldSpace (WorldPosition (orbit, Solve ())).ToTuple ();
        }

        /// <summary>
        /// The position of the target object at the closest approach.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned position
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 TargetPosition (ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            return referenceFrame.PositionFromWorldSpace (WorldPosition (target, Solve ())).ToTuple ();
        }

        /// <summary>
        /// The velocity of the orbiting object at the closest approach.
        /// </summary>
        /// <returns>The velocity as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned velocity
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            var ut = Solve ();
            return referenceFrame.VelocityFromWorldSpace (
                WorldPosition (orbit, ut), WorldVelocity (orbit, ut)).ToTuple ();
        }

        /// <summary>
        /// The velocity of the target object at the closest approach.
        /// </summary>
        /// <returns>The velocity as a vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned velocity
        /// vector is in. Defaults to the orbital reference frame of the object the orbit
        /// belongs to.</param>
        [KRPCMethod]
        public Tuple3 TargetVelocity (ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            var ut = Solve ();
            return referenceFrame.VelocityFromWorldSpace (
                WorldPosition (target, ut), WorldVelocity (target, ut)).ToTuple ();
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
        public Tuple3 RelativePosition (ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            var ut = Solve ();
            return referenceFrame.DirectionFromWorldSpace (
                WorldPosition (target, ut) - WorldPosition (orbit, ut)).ToTuple ();
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
        public Tuple3 RelativeVelocity (ReferenceFrame referenceFrame = null)
        {
            referenceFrame = DefaultedFrame (referenceFrame);
            var ut = Solve ();
            return referenceFrame.DirectionFromWorldSpace (
                WorldVelocity (target, ut) - WorldVelocity (orbit, ut)).ToTuple ();
        }
    }
}
