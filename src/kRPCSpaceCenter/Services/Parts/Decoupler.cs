using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Decoupler : Equatable<Decoupler>
    {
        readonly Part part;
        readonly ModuleDecouple decoupler;
        readonly ModuleAnchoredDecoupler anchoredDecoupler;

        internal Decoupler (Part part)
        {
            this.part = part;
            decoupler = part.InternalPart.Module<ModuleDecouple> ();
            anchoredDecoupler = part.InternalPart.Module<ModuleAnchoredDecoupler> ();
            if (decoupler == null && anchoredDecoupler == null)
                throw new ArgumentException ("Part does not have a ModuleDecouple or ModuleAnchoredDecouple PartModule");
        }

        public override bool Equals (Decoupler obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        [KRPCMethod]
        public void Decouple ()
        {
            if (decoupler != null)
                decoupler.Decouple ();
            else
                anchoredDecoupler.Decouple ();
        }

        [KRPCProperty]
        public bool IsDecoupled {
            get { return decoupler != null ? decoupler.isDecoupled : anchoredDecoupler.isDecoupled; }
        }

        [KRPCProperty]
        public float Force {
            get { return decoupler != null ? decoupler.ejectionForce : anchoredDecoupler.ejectionForce; }
        }
    }
}
