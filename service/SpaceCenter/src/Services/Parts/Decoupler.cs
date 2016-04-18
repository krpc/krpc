using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Decoupler"/>
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Decoupler : Equatable<Decoupler>
    {
        readonly Part part;
        readonly ModuleDecouple decoupler;
        readonly ModuleAnchoredDecoupler anchoredDecoupler;

        internal static bool Is (Part part)
        {
            return
                part.InternalPart.HasModule<ModuleDecouple> () ||
            part.InternalPart.HasModule<ModuleAnchoredDecoupler> ();
        }

        internal Decoupler (Part part)
        {
            this.part = part;
            decoupler = part.InternalPart.Module<ModuleDecouple> ();
            anchoredDecoupler = part.InternalPart.Module<ModuleAnchoredDecoupler> ();
            if (decoupler == null && anchoredDecoupler == null)
                throw new ArgumentException ("Part is not a decoupler");
        }

        /// <summary>
        /// Check the decouplers are equal.
        /// </summary>
        public override bool Equals (Decoupler obj)
        {
            return part == obj.part && decoupler == obj.decoupler && anchoredDecoupler == obj.anchoredDecoupler;
        }

        /// <summary>
        /// Hash the decoupler.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = part.GetHashCode ();
            if (decoupler != null)
                hash ^= decoupler.GetHashCode ();
            if (anchoredDecoupler != null)
                hash ^= anchoredDecoupler.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part object for this decoupler.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Fires the decoupler. Returns the new vessel created when the decoupler fires.
        /// Throws an exception if the decoupler has already fired.
        /// </summary>
        [KRPCMethod]
        public Vessel Decouple ()
        {
            if (Decoupled)
                throw new InvalidOperationException ("Decoupler has already fired");

            var preVesselIds = FlightGlobals.Vessels.Select (v => v.id).ToList ();

            // Fire the decoupler
            if (decoupler != null)
                decoupler.Decouple ();
            else
                anchoredDecoupler.Decouple ();

            return PostDecouple (preVesselIds);
        }

        Vessel PostDecouple (IList<Guid> preVesselIds, int wait = 0)
        {
            //FIXME: sometimes after decoupling, KSP changes it's mind as to what the active vessel is, so we wait for 10 frames before getting the active vessel
            // Wait while the decoupler hasn't fired
            if (wait < 10 || !Decoupled)
                throw new YieldException (new ParameterizedContinuation<Vessel, IList<Guid>, int> (PostDecouple, preVesselIds, wait + 1));
            // Return the newly created vessel
            return new Vessel (FlightGlobals.Vessels.Select (v => v.id).Except (preVesselIds).Single ());
        }

        /// <summary>
        /// Whether the decoupler has fired.
        /// </summary>
        [KRPCProperty]
        public bool Decoupled {
            get { return decoupler != null ? decoupler.isDecoupled : anchoredDecoupler.isDecoupled; }
        }

        /// <summary>
        /// The impulse that the decoupler imparts when it is fired, in Newton seconds.
        /// </summary>
        [KRPCProperty]
        public float Impulse {
            get { return (decoupler != null ? decoupler.ejectionForce : anchoredDecoupler.ejectionForce) * 10f; }
        }
    }
}
