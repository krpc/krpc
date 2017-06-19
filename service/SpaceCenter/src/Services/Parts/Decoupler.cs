using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A decoupler. Obtained by calling <see cref="Part.Decoupler"/>
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Decoupler : Equatable<Decoupler>
    {
        readonly ModuleDecouple decoupler;
        readonly ModuleAnchoredDecoupler anchoredDecoupler;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleDecouple> () ||
            internalPart.HasModule<ModuleAnchoredDecoupler> ();
        }

        internal Decoupler (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            decoupler = internalPart.Module<ModuleDecouple> ();
            anchoredDecoupler = internalPart.Module<ModuleAnchoredDecoupler> ();
            if (decoupler == null && anchoredDecoupler == null)
                throw new ArgumentException ("Part is not a decoupler");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Decoupler other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part != other.Part &&
            (decoupler == other.decoupler || decoupler.Equals (other.decoupler)) &&
            (anchoredDecoupler == other.anchoredDecoupler || anchoredDecoupler.Equals (other.anchoredDecoupler));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = Part.GetHashCode ();
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
        public Part Part { get; private set; }

        /// <summary>
        /// Fires the decoupler. Returns the new vessel created when the decoupler fires.
        /// Throws an exception if the decoupler has already fired.
        /// </summary>
        /// <remarks>
        /// When called, the active vessel may change. It is therefore possible that,
        /// after calling this function, the object(s) returned by previous call(s) to
        /// <see cref="SpaceCenter.ActiveVessel"/> no longer refer to the active vessel.
        /// </remarks>
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
            // FIXME: sometimes after decoupling, KSP changes it's mind as to what the active vessel is, so we wait for 10 frames before getting the active vessel
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public bool Decoupled {
            get { return decoupler != null ? decoupler.isDecoupled : anchoredDecoupler.isDecoupled; }
        }

        /// <summary>
        /// Whether the decoupler is enabled in the staging sequence.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public bool Staged {
            get { return decoupler != null ? decoupler.StagingEnabled () : anchoredDecoupler.StagingEnabled (); }
        }

        /// <summary>
        /// The impulse that the decoupler imparts when it is fired, in Newton seconds.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float Impulse {
            get { return (decoupler != null ? decoupler.ejectionForce : anchoredDecoupler.ejectionForce) * 10f; }
        }
    }
}
