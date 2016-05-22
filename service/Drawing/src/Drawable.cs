using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// Abstract base class for objects that can be drawn.
    /// </summary>
    public abstract class Drawable : Equatable<Drawable>, IDrawingObject
    {
        readonly ulong id;
        static ulong nextId;

        /// <summary>
        /// Create a drawable and register it with the draw addon.
        /// </summary>
        protected Drawable (string type, Type rendererType)
        {
            id = nextId;
            nextId++;
            GameObject = new GameObject ("krpc.drawing." + type + "." + id);
            Renderer = (Renderer)GameObject.AddComponent (rendererType);
            Material = "Particles/Additive";
            Addon.AddObject (this);
        }

        /// <summary>
        /// Check if drawables are equal.
        /// </summary>
        public override bool Equals (Drawable obj)
        {
            return id == obj.id;
        }

        /// <summary>
        /// Hash the drawable.
        /// </summary>
        public override int GetHashCode ()
        {
            return (int)id;
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
