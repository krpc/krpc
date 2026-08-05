using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Abstract base class for user interface objects that the user can interact with.
    /// </summary>
    public abstract class Control : Object
    {
        /// <summary>
        /// Create a control.
        /// </summary>
        protected Control (GameObject gameObject, bool visible)
            : base (gameObject, visible)
        {
        }

        /// <summary>
        /// The Unity engine component that handles interaction with the control.
        /// </summary>
        protected abstract UnityEngine.UI.Selectable Selectable { get; }

        /// <summary>
        /// Whether the control responds to the user.
        /// </summary>
        /// <remarks>
        /// A control that is not interactable is drawn grayed out and ignores the user,
        /// but is still visible. Set its visibility to false to hide it instead.
        /// </remarks>
        [KRPCProperty]
        public bool Interactable {
            get { return Selectable.interactable; }
            set { Selectable.interactable = value; }
        }
    }
}
