using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

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
        /// Fires the decoupler. Has no effect if the decoupler has already fired.
        /// </summary>
        [KRPCMethod]
        public void Decouple ()
        {
            if (decoupler != null)
                decoupler.Decouple ();
            else
                anchoredDecoupler.Decouple ();
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
