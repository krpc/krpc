using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class LaunchClamp : Equatable<LaunchClamp>
    {
        readonly Part part;
        readonly global::LaunchClamp launchClamp;

        internal LaunchClamp (Part part)
        {
            this.part = part;
            launchClamp = part.InternalPart.Module<global::LaunchClamp> ();
            if (launchClamp == null)
                throw new ArgumentException ("Part does not have a LaunchClamp PartModule");
        }

        public override bool Equals (LaunchClamp obj)
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
        public void Release ()
        {
            launchClamp.Release ();
        }
    }
}
