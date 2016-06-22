using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// Abstract base class for objects that can be drawn.
    /// </summary>
    public abstract class Drawable<T> : IDrawable
    {
        /// <summary>
        /// Create a drawable and register it with the draw addon.
        /// </summary>
        protected Drawable (Type rendererType)
        {
            GameObject = new GameObject ("KRPC.Drawing." + typeof(T).Name);
            Renderer = (Renderer)GameObject.AddComponent (rendererType);
            Material = "Particles/Additive";
            Addon.AddObject (this);
        }

        /// <summary>
        /// Update the drawable.
        /// </summary>
        public abstract void Update ();

        /// <summary>
        /// Destroy the drawable.
        /// </summary>
        public virtual void Destroy ()
        {
            UnityEngine.Object.Destroy (Renderer);
            UnityEngine.Object.Destroy (GameObject);
        }

        /// <summary>
        /// The game object for the drawable.
        /// </summary>
        public GameObject GameObject { get; private set; }

        /// <summary>
        /// The renderer object for the drawable.
        /// </summary>
        protected Renderer Renderer { get; private set; }

        /// <summary>
        /// Remove the object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            Addon.RemoveObject (this);
        }

        /// <summary>
        /// Reference frame for the positions of the object.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// Whether the object is visible.
        /// </summary>
        [KRPCProperty]
        public bool Visible { get; set; }

        /// <summary>
        /// Material used to render the object.
        /// Creates the material from a shader with the given name.
        /// </summary>
        [KRPCProperty]
        public string Material {
            get { return Renderer.material.shader.name; }
            set { Renderer.material = new Material (Shader.Find (value)); }
        }
    }
}
