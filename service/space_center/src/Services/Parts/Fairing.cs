using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A fairing. Obtained by calling <see cref="Part.Fairing"/>.
    /// Supports both stock fairings, and those from the ProceduralFairings mod.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Fairing : Equatable<Fairing>
    {
        readonly Module fairing;
        readonly Module proceduralFairing;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            // ProceduralFairingDecoupler is from the ProceduralFairings mod
            return internalPart.HasModule<ModuleProceduralFairing> () || internalPart.HasModule("ProceduralFairingDecoupler");
        }

        internal Fairing (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            if (internalPart.HasModule<ModuleProceduralFairing>())
                fairing = new Module(part, internalPart.Module<ModuleProceduralFairing>());
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
                if (fairing != null) {
                    fairing.TriggerEvent("Deploy");
                } else {
                    // Note: older versions of ProceduralFairings have the "Jettison" event,
                    // newer versions have the "Jettison Fairing" event
                    foreach (var e in proceduralFairing.Events) {
                        if (e == "Jettison")
                            proceduralFairing.TriggerEvent("Jettison");
                        if (e == "Jettison Fairing")
                            proceduralFairing.TriggerEvent("Jettison Fairing");
                    }
                }
            }
        }

        /// <summary>
        /// Whether the fairing has been jettisoned.
        /// </summary>
        [KRPCProperty]
        public bool Jettisoned
        {
            get {
                if (fairing != null) {
                    return !fairing.Events.Contains("Deploy");
                } else {
                    // Note: older versions of ProceduralFairings have the "Jettison" event,
                    // newer versions have the "Jettison Fairing" event
                    var events = proceduralFairing.Events;
                    return !(events.Contains("Jettison") ||
                             events.Contains("Jettison Fairing"));
                }
            }
        }
    }
}
