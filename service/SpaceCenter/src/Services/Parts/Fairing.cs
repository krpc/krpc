using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A fairing. Obtained by calling <see cref="Part.Fairing"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Fairing : Equatable<Fairing>
    {
        readonly ModuleProceduralFairing fairing;
        readonly Module proceduralFairing;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return internalPart.HasModule<ModuleProceduralFairing> () || internalPart.HasModule("ProceduralFairingDecoupler");
        }

        internal Fairing (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            fairing = internalPart.Module<ModuleProceduralFairing> ();
            if (internalPart.HasModule ("ProceduralFairingDecoupler"))
                proceduralFairing = new Module (part, internalPart.Module ("ProceduralFairingDecoupler"));
            if (fairing == null && proceduralFairing == null)
                throw new ArgumentException ("Part is not a fairing");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Fairing other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this fairing.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Jettison the fairing. Has no effect if it has already been jettisoned.
        /// </summary>
        [KRPCMethod]
        public void Jettison ()
        {
            if (!Jettisoned) {
                if (fairing != null)
                    fairing.DeployFairing ();
                else
                    proceduralFairing.TriggerEvent ("Jettison");
            }
        }

        /// <summary>
        /// Whether the fairing has been jettisoned.
        /// </summary>
        [KRPCProperty]
        public bool Jettisoned {
            get { return fairing != null ? !fairing.CanMove : !proceduralFairing.Events.Contains ("Jettison"); }
        }
    }
}
