using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Abstract base class for all UI objects.
    /// </summary>
    public abstract class Object : Equatable<Object>
    {
        /// <summary>
        /// Whether a client can remove the object. Only the objects a client owns can be
        /// removed; the stock canvas and the parts a control is built from live and die
        /// with what they belong to.
        /// </summary>
        readonly bool removable;

        /// <summary>
        /// Whether the object has been destroyed. Unity carries out a destroy at the end
        /// of the frame it was asked for in, and the server answers more than one call in
        /// a frame, so the game object is still there to be asked afterwards and cannot be
        /// used on its own to tell whether the object is gone.
        /// </summary>
        bool destroyed;

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
            removable = register;
        }

        /// <summary>
        /// Create a UI object from a canvas.
        /// </summary>
        protected Object (UnityEngine.Canvas canvas)
        {
            GameObject = canvas.gameObject;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        /// <remarks>
        /// Two objects are equal when they refer to the same user interface element, so
        /// that a client is handed the same object identifier however it reaches the
        /// element, and asking for the same element repeatedly does not accumulate
        /// identifiers.
        /// </remarks>
        public override bool Equals (Object other)
        {
            return !ReferenceEquals (other, null) &&
                GetType () == other.GetType () &&
                GameObject == other.GameObject;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return GameObject.GetHashCode ();
        }

        /// <summary>
        /// Whether the UI object still exists. False once it has been removed, or once the
        /// game object behind it has been destroyed by a scene change.
        /// </summary>
        internal bool Exists {
            get { return !destroyed && GameObject != null; }
        }

        /// <summary>
        /// The rect transform for the UI object.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get { return new RectTransform (GameObject.GetComponent<UnityEngine.RectTransform> ()); }
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
        /// <remarks>
        /// The teardown itself, with no say in whether it should happen: whether a client
        /// may ask for it is <see cref="Remove" />'s to decide. This runs from the addon
        /// sweeping up after a client or a scene as well, where throwing would abandon
        /// whatever was left in the sweep.
        /// </remarks>
        public void Destroy ()
        {
            UnityEngine.Object.Destroy (GameObject);
            destroyed = true;
        }

        /// <summary>
        /// Remove the UI object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            if (!removable)
                throw new InvalidOperationException ("UI object is not removable");
            Addon.Remove (this);
        }
    }
}
