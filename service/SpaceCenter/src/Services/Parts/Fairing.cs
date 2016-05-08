using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Fairing"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Fairing : Equatable<Fairing>
    {
        readonly Part part;
        readonly ModuleProceduralFairing fairing;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleProceduralFairing> ();
        }

        internal Fairing (Part part)
        {
            this.part = part;
            fairing = part.InternalPart.Module<ModuleProceduralFairing> ();
            if (fairing == null)
                throw new ArgumentException ("Part is not a fairing");
        }

        /// <summary>
        /// Check if fairings are equal.
        /// </summary>
        public override bool Equals (Fairing obj)
        {
            return part == obj.part && fairing == obj.fairing;
        }

        /// <summary>
        /// Hash the fairing.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ fairing.GetHashCode ();
        }

        /// <summary>
        /// The part object for this fairing.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Jettison the fairing. Has no effect if it has already been jettisoned.
        /// </summary>
        [KRPCMethod]
        public void Jettison ()
        {
            if (!Jettisoned)
                fairing.DeployFairing ();
        }

        /// <summary>
        /// Whether the fairing has been jettisoned.
        /// </summary>
        [KRPCProperty]
        public bool Jettisoned {
            get { return !fairing.CanMove; }
        }
    }
}
