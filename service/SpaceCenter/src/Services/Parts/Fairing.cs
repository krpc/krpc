using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A fairing. Obtained by calling <see cref="Part.Fairing"/>.
    /// Supports both stock fairings, and those from the ProceduralFairings mod.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Fairing : Equatable<Fairing>, IGameObjectState
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
        /// The state of the fairing. The module objects it is built from look their part
        /// modules up on each access, and this follows them.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return (fairing ?? proceduralFairing).GameObjectState; }
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
            return Hash.Of (Part).And (fairing).And (proceduralFairing);
        }

        /// <summary>
        /// The part object for this fairing.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Jettison the fairing. Has no effect if it has already been jettisoned.
        /// </summary>
        [KRPCMethod]
        public void Jettison ()
        {
            if (!Jettisoned) {
                if (fairing != null) {
                    fairing.TriggerVisibleEventById("DeployFairing");
                } else {
                    // Older versions of ProceduralFairings have the "Jettison" event, newer
                    // versions have the "Jettison Fairing" event. These match the display name
                    // rather than the event id, as the mod's method names are not known here;
                    // the mod does not appear to translate them.
                    foreach (var e in proceduralFairing.VisibleEventNames) {
                        if (e == "Jettison")
                            proceduralFairing.TriggerVisibleEvent("Jettison");
                        if (e == "Jettison Fairing")
                            proceduralFairing.TriggerVisibleEvent("Jettison Fairing");
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
                    return !fairing.HasVisibleEventById("DeployFairing");
                } else {
                    // Older versions of ProceduralFairings have the "Jettison" event, newer
                    // versions have the "Jettison Fairing" event. These match the display name
                    // rather than the event id, as the mod's method names are not known here;
                    // the mod does not appear to translate them.
                    return !(proceduralFairing.HasVisibleEvent("Jettison") ||
                             proceduralFairing.HasVisibleEvent("Jettison Fairing"));
                }
            }
        }
    }
}
