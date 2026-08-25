using System;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.UI
{
    /// <summary>
    /// Abstract base class for all UI objects.
    /// </summary>
    public abstract class Object : Equatable<Object>, IGameObjectState
    {
        /// <summary>
        /// Whether a client can remove the object. Only the objects a client owns can be
        /// removed; the stock canvas and the parts a control is built from live and die
        /// with what they belong to.
        /// </summary>
        readonly bool removable;
        // Whether the element has been taken out of the interface. The game tears a game
        // object down at the end of the frame, so this records that it is on its way out,
        // and an element removed and read again in the same frame reports it gone.
        bool removed;

        /// <summary>
        /// Unity game object for the UI element.
        /// </summary>
        protected GameObject GameObject { get; private set; }

        /// <summary>
        /// The state of the element, which is the game object it was made with. Removing
        /// it, clearing the interface elements, disconnecting the client that made it and
        /// leaving the scene each destroy that object. A client's element is never built
        /// again, so it has no dormant state.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return removed || GameObject == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        /// <summary>
        /// The game object for the element, checked to still exist. Every member that
        /// reaches into the game goes through this, or through a component the element
        /// found on it, so that an element the game no longer has says so rather than
        /// failing on a torn down object.
        /// </summary>
        protected GameObject CheckedGameObject {
            get {
                CheckExists ();
                return GameObject;
            }
        }

        /// <summary>
        /// Raise if the game no longer has the element.
        /// </summary>
        protected void CheckExists ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                throw new ObjectDestroyedException (
                    "The user interface object no longer exists, as it has been removed.");
        }

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
        ///
        /// The game objects are compared by reference rather than with Unity's own
        /// equality, which reports a destroyed object as equal to null and so would make
        /// two objects standing for different destroyed elements equal to each other.
        /// </remarks>
        public override bool Equals (Object other)
        {
            return !ReferenceEquals (other, null) &&
                GetType () == other.GetType () &&
                ReferenceEquals (GameObject, other.GameObject);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // The game object's identity hash rather than its own, so that nothing the
            // game does to it can change it while a client holds this object.
            return RuntimeHelpers.GetHashCode (GameObject);
        }

        /// <summary>
        /// Whether the UI object still exists. False once it has been removed, or once the
        /// game object behind it has been destroyed by a scene change.
        /// </summary>
        internal bool Exists {
            get { return GameObjectState != GameObjectState.Destroyed; }
        }

        /// <summary>
        /// The rect transform for the UI object.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get {
                return new RectTransform (
                    CheckedGameObject.GetComponent<UnityEngine.RectTransform> ());
            }
        }

        /// <summary>
        /// Whether the object is one that can be given a layout element. False for a
        /// canvas, which is not inside anything that could lay it out.
        /// </summary>
        internal virtual bool CanHaveLayoutElement {
            get { return true; }
        }

        /// <summary>
        /// How much space the UI object asks a layout for.
        /// </summary>
        /// <remarks>
        /// Only has an effect when the object is in a panel that has a layout. A canvas is
        /// not inside a layout and does not have one.
        /// </remarks>
        [KRPCProperty]
        public LayoutElement LayoutElement {
            get {
                // The component is added the first time it is asked for, so asking must be
                // refused for an object that would keep it for good. The stock canvas is
                // the game's own, and kRPC never destroys it, so a component added to it
                // would be left there for the rest of the session.
                if (!CanHaveLayoutElement)
                    throw new InvalidOperationException (
                        "A canvas is not inside a layout, so it has no layout element");
                var gameObject = CheckedGameObject;
                var element = gameObject.GetComponent<UnityEngine.UI.LayoutElement> ();
                if (element == null)
                    element = gameObject.AddComponent<UnityEngine.UI.LayoutElement> ();
                return new LayoutElement (element);
            }
        }

        /// <summary>
        /// Whether the UI object is visible.
        /// </summary>
        /// <remarks>
        /// This is the object's own setting, which is what it was last set to. An object is
        /// only drawn when everything it is inside is visible as well, so an interface can
        /// be built with its parts visible, inside a panel that is not, and shown all at
        /// once by making that panel visible.
        /// </remarks>
        [KRPCProperty]
        public bool Visible {
            get { return CheckedGameObject.activeSelf; }
            set { CheckedGameObject.SetActive (value); }
        }

        /// <summary>
        /// Destroy the UI object. Overridden by objects that hold something Unity does not
        /// destroy along with the game object, such as a texture.
        /// </summary>
        /// <remarks>
        /// The teardown itself, with no say in whether it should happen: whether a client
        /// may ask for it is <see cref="Remove" />'s to decide. This runs from the addon
        /// sweeping up after a client or a scene as well, where throwing would abandon
        /// whatever was left in the sweep.
        /// </remarks>
        public virtual void Destroy ()
        {
            removed = true;
            UnityEngine.Object.Destroy (GameObject);
        }

        /// <summary>
        /// Remove the UI object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
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
