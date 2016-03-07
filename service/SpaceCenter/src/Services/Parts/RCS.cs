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
        /// A list of thrusters, one of each thruster in the RCS.
        /// </summary>
        [KRPCProperty]
        public IList<Thruster> Thrusters {
            get { return Enumerable.Range (0, rcs.thrusterTransforms.Count).Select (i => new Thruster (part, rcs, i)).ToList (); }
        }
    }
}
