using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<float, float, float>;

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
        /// The color of the light, as an RGB triple.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Tuple3 Color {
            get { return new Tuple3 (InternalLight.lightR, InternalLight.lightG, InternalLight.lightB); }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (Color));
                InternalLight.lightR = value.Item1;
                InternalLight.lightG = value.Item2;
                InternalLight.lightB = value.Item3;
                InternalLight.SetFlareColor(new Color(value.Item1, value.Item2, value.Item3));
                foreach (var unityLight in InternalLight.lights)
                    unityLight.color = new Color (value.Item1, value.Item2, value.Item3);
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
