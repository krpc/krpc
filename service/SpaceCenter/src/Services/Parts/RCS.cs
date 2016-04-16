using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.RCS"/>.
    /// Provides functionality to interact with RCS blocks and thrusters.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class RCS : Equatable<RCS>
    {
        readonly Part part;
        readonly ModuleRCS rcs;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleRCS> () && !part.InternalPart.HasModule<ModuleProceduralFairing> ();
        }

        internal RCS (Part part)
        {
            this.part = part;
            rcs = part.InternalPart.Module<ModuleRCS> ();
            if (rcs == null)
                throw new ArgumentException ("Part does not have a ModuleRCS PartModule");
        }

        /// <summary>
        /// Check the RCS are equal.
        /// </summary>
        public override bool Equals (RCS obj)
        {
            return part == obj.part && rcs == obj.rcs;
        }

        /// <summary>
        /// Hash the RCS.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ rcs.GetHashCode ();
        }

        /// <summary>
        /// The part object for this RCS.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the RCS thrusters are active.
        /// Note that if an RCS thruster is covered by a fairing it will not be active.
        /// Note also that for an RCS thruster to be active, the RCS action group needs to be enabled.
        /// See <see cref="Control.RCS" />.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                var p = part.InternalPart;
                return p.vessel.ActionGroups [KSPActionGroup.RCS] &&
                !p.ShieldedFromAirstream && rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow;
            }
            set { rcs.rcsEnabled = value; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when pitch control input is given.
        /// </summary>
        [KRPCProperty]
        public bool PitchEnabled {
            get { return rcs.enablePitch; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when yaw control input is given.
        /// </summary>
        [KRPCProperty]
        public bool YawEnabled {
            get { return rcs.enableYaw; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when roll control input is given.
        /// </summary>
        [KRPCProperty]
        public bool RollEnabled {
            get { return rcs.enableRoll; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when pitch control input is given.
        /// </summary>
        [KRPCProperty]
        public bool ForwardEnabled {
            get { return rcs.enableZ; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when yaw control input is given.
        /// </summary>
        [KRPCProperty]
        public bool UpEnabled {
            get { return rcs.enableY; }
        }

        /// <summary>
        /// Whether the RCS thruster will fire when roll control input is given.
        /// </summary>
        [KRPCProperty]
        public bool RightEnabled {
            get { return rcs.enableX; }
        }

        /// <summary>
        /// The current amount of thrust being produced by the RCS, in
        /// Newtons. Returns zero if the thrusters are not active or if there is no RCS fuel.
        /// </summary>
        [KRPCProperty]
        public float Thrust {
            get { throw new NotImplementedException (); }
        }

        /// <summary>
        /// The maximum available amount of thrust that can be produced by the
        /// RCS, in Newtons. This takes <see cref="Engine.ThrustLimit"/> into account,
        /// and is the amount of thrust produced by the RCS when activated.
        /// Returns zero if there is no RCS fuel.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get { throw new NotImplementedException (); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the RCS, in
        /// Newtons. This is the amount of thrust produced by the RCS when
        /// activated.
        /// </summary>
        [KRPCProperty]
        public float MaxThrust {
            get { throw new NotImplementedException (); }
        }

        /// <summary>
        /// The maximum amount of thrust that can be produced by the RCS in a
        /// vacuum, in Newtons. This is the amount of thrust produced by the RCS
        /// when activated, <see cref="Engine.ThrustLimit"/> is set to 100%,
        /// and the RCS is in a vacuum.
        /// </summary>
        [KRPCProperty]
        public float MaxVacuumThrust {
            get { throw new NotImplementedException (); }
        }

        /// <summary>
        /// A list of thrusters, one of each nozzel in the RCS part.
        /// </summary>
        [KRPCProperty]
        public IList<Thruster> Thrusters {
            get { return Enumerable.Range (0, rcs.thrusterTransforms.Count).Select (i => new Thruster (part, rcs, i)).ToList (); }
        }

        /// <summary>
        /// The current specific impulse of the RCS, in seconds. Returns zero
        /// if the RCS is not active.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get { return rcs.realISP; }
        }

        /// <summary>
        /// The vacuum specific impulse of the RCS, in seconds.
        /// </summary>
        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get { return rcs.atmosphereCurve.Evaluate (0); }
        }

        /// <summary>
        /// The specific impulse of the RCS at sea level on Kerbin, in seconds.
        /// </summary>
        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get { return rcs.atmosphereCurve.Evaluate (1); }
        }

        /// <summary>
        /// The names of resources that the RCS consumes.
        /// </summary>
        [KRPCProperty]
        public IList<string> Propellants {
            get { return rcs.propellants.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// The ratios of resources that the RCS consumes. A dictionary mapping resource names
        /// to the ratios at which they are consumed by the RCS.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, float> PropellantRatios {
            get {
                var max = rcs.propellants.Max (p => p.ratio);
                return rcs.propellants.ToDictionary (p => p.name, p => p.ratio / max);
            }
        }

        /// <summary>
        /// Whether the RCS has fuel available.
        /// </summary>
        [KRPCProperty]
        public bool HasFuel {
            get { throw new NotImplementedException (); }
        }
    }
}
