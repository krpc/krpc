using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Abstract base class for user interface objects that other objects can be added to.
    /// </summary>
    public abstract class Container : Object
    {
        /// <summary>
        /// Create a container.
        /// </summary>
        protected Container (GameObject gameObject, bool visible)
            : base (gameObject, visible)
        {
        }

        /// <summary>
        /// Create a container from a canvas.
        /// </summary>
        protected Container (UnityEngine.Canvas canvas)
            : base (canvas)
        {
        }

        /// <summary>
        /// Create a panel within this object.
        /// </summary>
        /// <param name="visible">Whether the new panel is visible.</param>
        [KRPCMethod]
        public Panel AddPanel (bool visible = true)
        {
            return new Panel (GameObject, visible);
        }

        /// <summary>
        /// Add text to this object.
        /// </summary>
        /// <param name="content">The text.</param>
        /// <param name="visible">Whether the text is visible.</param>
        [KRPCMethod]
        public Text AddText (string content, bool visible = true)
        {
            return new Text (GameObject, content, visible);
        }

        /// <summary>
        /// Add an input field to this object.
        /// </summary>
        /// <param name="visible">Whether the input field is visible.</param>
        [KRPCMethod]
        public InputField AddInputField (bool visible = true)
        {
            return new InputField (GameObject, visible);
        }

        /// <summary>
        /// Add a button to this object.
        /// </summary>
        /// <param name="content">The label for the button.</param>
        /// <param name="visible">Whether the button is visible.</param>
        [KRPCMethod]
        public Button AddButton (string content, bool visible = true)
        {
            return new Button (GameObject, content, visible);
        }
    }
}
