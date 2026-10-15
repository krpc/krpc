using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A light. Obtained by calling <see cref="Part.Light"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Light : Equatable<Light>, IGameObjectState
    {
        ModuleRef lightRef;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleLight> ();
        }

        internal Light (Part part)
        {
            Part = part;
            lightRef = ModuleRef.ForType<ModuleLight> (part.InternalPart);
            if (lightRef.Find (part.InternalPart) == null)
                throw new ArgumentException ("Part is not a light");
        }

        ModuleLight InternalLight {
            get { return (ModuleLight)lightRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the light, or destroyed once that part loses the
        /// module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return lightRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Light other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && lightRef == other.lightRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (lightRef);
        }

        /// <summary>
        /// The part object for this light.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the light is switched on.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return InternalLight.isOn; }
            set { InternalLight.ToggleLightAction(new KSPActionParam(0, value ? KSPActionType.Activate : KSPActionType.Deactivate)); }
        }

        /// <summary>
        /// The color of the light.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public ColorRGB Color {
            get { return new ColorRGB (InternalLight.lightR, InternalLight.lightG, InternalLight.lightB); }
            set {
                InternalLight.lightR = value.R;
                InternalLight.lightG = value.G;
                InternalLight.lightB = value.B;
                var color = value.ToColor ();
                InternalLight.SetFlareColor (color);
                foreach (var unityLight in InternalLight.lights)
                    unityLight.color = color;
            }
        }

        /// <summary>
        /// Whether blinking is enabled.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Blink
        {
            get { return InternalLight.blinkState; }
            set { InternalLight.SetBlinkState(value); }
        }

        /// <summary>
        /// The blink rate of the light.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float BlinkRate
        {
            get { return InternalLight.blinkRate; }
            set { InternalLight.blinkRate = value; }
        }

        /// <summary>
        /// The current power usage, in units of charge per second.
        /// </summary>
        [KRPCProperty]
        public float PowerUsage {
            get { return Active ? InternalLight.resourceAmount : 0f; }
        }
    }
}
