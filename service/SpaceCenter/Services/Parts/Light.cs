using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.Light"/>.
    /// </summary>
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

        /// <summary>
        /// The part object for this light.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the light is switched on.
        /// </summary>
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

        /// <summary>
        /// The current power usage, in units of charge per second.
        /// </summary>
        [KRPCProperty]
        public float PowerUsage {
            get { return Active ? light.resourceAmount : 0f; }
        }
    }
}
