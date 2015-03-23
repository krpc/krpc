using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Light : Equatable<Light>
    {
        readonly Part part;
        readonly ModuleLight light;

        internal Light (Part part)
        {
            this.part = part;
            light = part.InternalPart.Module<ModuleLight> ();
            if (light == null)
                throw new ArgumentException ("Part does not have a ModuleLight PartModule");
        }

        public override bool Equals (Light obj)
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

        [KRPCProperty]
        public bool Active {
            get { return light.isOn; }
            set {
                if (value)
                    light.LightsOn ();
                else
                    light.LightsOff ();
            }
        }

        [KRPCProperty]
        public float PowerUsage {
            get { return Active ? light.resourceAmount : 0f; }
        }
    }
}
