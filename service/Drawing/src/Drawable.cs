using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.Drawing
{
    /// <summary>
    /// Abstract base class for objects that can be drawn.
    /// </summary>
    public abstract class Drawable<T> : IDrawable
    {
        readonly Renderer renderer;
        // Whether the drawable has been taken out of the scene. The game tears a game
        // object down at the end of the frame, so this records that it is on its way out,
        // and an object removed and read again in the same frame reports it gone.
        bool removed;

        /// <summary>
        /// Create a drawable and register it with the draw addon.
        /// </summary>
        protected Drawable (Type rendererType)
        {
            GameObject = new GameObject ("KRPC.Drawing." + typeof(T).Name);
            renderer = (Renderer)GameObject.AddComponent (rendererType);
            Material = "Legacy Shaders/Particles/Additive";
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
            removed = true;
            UnityEngine.Object.Destroy (renderer);
            UnityEngine.Object.Destroy (GameObject);
        }

        /// <summary>
        /// The game object for the drawable.
        /// </summary>
        public GameObject GameObject { get; private set; }

        /// <summary>
        /// The state of the drawable, which is the game object it was made with. Removing
        /// it, clearing the drawing objects, disconnecting the client that made it and
        /// leaving the scene each destroy that object. A client's drawing is never built
        /// again, so it has no dormant state.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return removed || GameObject == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        /// <summary>
        /// The renderer object for the drawable, checked to still exist. Every member that
        /// reaches into the game goes through this, so that a drawable the game no longer
        /// has says so rather than failing on a torn down object.
        /// </summary>
        protected Renderer Renderer {
            get {
                CheckExists ();
                return renderer;
            }
        }

        /// <summary>
        /// Raise if the game no longer has the drawable.
        /// </summary>
        protected void CheckExists ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                throw new ObjectDestroyedException (
                    "The drawing object no longer exists, as it has been removed.");
        }

        /// <summary>
        /// Whether the drawable can be drawn where it is asked to be. A reference frame is
        /// defined against things the game can destroy, and a drawable is left where it is
        /// rather than removed when that happens, as it can be given another frame.
        /// </summary>
        protected bool CanBeDrawn {
            get {
                return GameObjectState == GameObjectState.Live &&
                ReferenceFrame.GameObjectState == GameObjectState.Live;
            }
        }

        /// <summary>
        /// Remove the object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
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
