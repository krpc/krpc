using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<float, float, float>;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A light. Obtained by calling <see cref="Part.Light"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Light : Equatable<Light>
    {
        readonly ModuleLight light;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleLight> ();
        }

        internal Light (Part part)
        {
            Part = part;
            light = part.InternalPart.Module<ModuleLight> ();
            if (light == null)
                throw new ArgumentException ("Part is not a light");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Light other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && light.Equals (other.light);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ light.GetHashCode ();
        }

        /// <summary>
        /// The part object for this light.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the light is switched on.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return light.isOn; }
            set { light.ToggleLightAction(new KSPActionParam(0, value ? KSPActionType.Activate : KSPActionType.Deactivate)); }
        }

        /// <summary>
        /// The color of the light, as an RGB triple.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return new Tuple3 (light.lightR, light.lightG, light.lightB); }
            set {
                
                light.lightR = value.Item1;
                light.lightG = value.Item2;
                light.lightB = value.Item3;
                light.SetFlareColor(new Color(value.Item1, value.Item2, value.Item3));
                foreach (var unityLight in light.lights)
                    unityLight.color = new Color (value.Item1, value.Item2, value.Item3);
                
            }
        }

        /// <summary>
        /// The color of the light, as an RGB triple.
        /// </summary>
        [KRPCProperty]
        public float BlinkRate
        {
            get { return light.blinkRate; }
            set { light.blinkRate = value; }
        }

        /// <summary>
        /// Enables blinking
        /// </summary>
        [KRPCMethod]
        public void BlinkStart()
        {
            light.SetBlinkState(true);
        }

        /// <summary>
        /// Disables Blinking blinking
        /// </summary>
        [KRPCMethod]
        public void BlinkStop()
        {
            light.SetBlinkState(false);
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
