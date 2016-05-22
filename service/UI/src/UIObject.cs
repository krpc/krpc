using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Abstract base class for all UI objects.
    /// </summary>
    public abstract class UIObject : Equatable<UIObject>
    {
        static int nextId;
        readonly int id;

        /// <summary>
        /// Unity game object for the UI element.
        /// </summary>
        protected GameObject obj;

        /// <summary>
        /// Create a UI object.
        /// </summary>
        protected UIObject (GameObject obj, bool visible, bool register = true)
        {
            id = nextId;
            nextId++;
            this.obj = obj;
            obj.SetActive (visible);
            if (register)
                Addon.AddObject (this);
        }

        /// <summary>
        /// Check if UI objects are equal.
        /// </summary>
        public override bool Equals (UIObject obj)
        {
            return id == obj.id;
        }

        /// <summary>
        /// Hash the UI object.
        /// </summary>
        public override int GetHashCode ()
        {
            return id;
        }

        /// <summary>
        /// Whether the UI object is visible.
        /// </summary>
        [KRPCProperty]
        public bool Visible {
            get { return obj.activeInHierarchy; }
            set { obj.SetActive (value); }
        }

        /// <summary>
        /// Destroy the UI object.
        /// </summary>
        public void Destroy ()
        {
            Object.Destroy (obj);
        }

        /// <summary>
        /// Remove the UI object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            Addon.RemoveObject (this);
        }
    }
}
