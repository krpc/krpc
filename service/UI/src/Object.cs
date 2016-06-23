using System;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Abstract base class for all UI objects.
    /// </summary>
    public abstract class Object
    {
        readonly bool removable;

        /// <summary>
        /// Unity game object for the UI element.
        /// </summary>
        protected GameObject GameObject { get; private set; }

        /// <summary>
        /// Create a UI object.
        /// </summary>
        protected Object (GameObject gameObject, bool visible, bool register = true)
        {
            GameObject = gameObject;
            gameObject.SetActive (visible);
            if (register)
                Addon.Add (this);
            removable = true;
        }

        /// <summary>
        /// Create a UI object from a canvas.
        /// </summary>
        protected Object (UnityEngine.Canvas canvas)
        {
            GameObject = canvas.gameObject;
        }

        /// <summary>
        /// Whether the UI object is visible.
        /// </summary>
        [KRPCProperty]
        public bool Visible {
            get { return GameObject.activeInHierarchy; }
            set { GameObject.SetActive (value); }
        }

        /// <summary>
        /// Destroy the UI object.
        /// </summary>
        public void Destroy ()
        {
            CheckNotRemovable ();
            UnityEngine.Object.Destroy (GameObject);
        }

        /// <summary>
        /// Remove the UI object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            CheckNotRemovable ();
            Addon.Remove (this);
        }

        void CheckNotRemovable ()
        {
            if (!removable)
                throw new InvalidOperationException ("UI object is not removable");
        }
    }
}
