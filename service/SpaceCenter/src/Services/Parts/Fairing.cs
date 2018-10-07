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
        readonly Module fairing;
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
            if (internalPart.HasModule("ModuleProceduralFairing"))
                fairing = new Module(part, internalPart.Module("ModuleProceduralFairing"));
            if (internalPart.HasModule("ProceduralFairingDecoupler"))
                proceduralFairing = new Module(part, internalPart.Module("ProceduralFairingDecoupler"));
            if (fairing == null && proceduralFairing == null)
                throw new ArgumentException ("Part is not a fairing");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Fairing other)
        {
            return !ReferenceEquals (other, null) &&
                Part == other.Part && fairing == other.fairing && proceduralFairing == other.proceduralFairing;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            int h = Part.GetHashCode ();
            if (fairing != null)
                h ^= fairing.GetHashCode();
            if (proceduralFairing != null)
                h ^= proceduralFairing.GetHashCode();
            return h;
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
                    fairing.TriggerEvent("Deploy");
                else
                    proceduralFairing.TriggerEvent("Jettison");
            }
        }

        /// <summary>
        /// Whether the fairing has been jettisoned.
        /// </summary>
        [KRPCProperty]
        public bool Jettisoned
        {
            get {
                if (fairing != null)
                    return !fairing.Events.Contains("Deploy");
                else
                    return !proceduralFairing.Events.Contains("Jettison");
            }
        }
    }
}
