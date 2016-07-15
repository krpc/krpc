using KRPC.Service.Attributes;
using UnityEngine;
using Tuple2 = KRPC.Utils.Tuple<double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A container for user interface elements. See <see cref="Canvas.AddPanel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Panel : Object
    {
        internal Panel (GameObject parent, bool visible)
            : base (Addon.Instantiate (parent, "Panel"), visible)
        {
            RectTransform.Anchor = new Tuple2 (0.5f, 0.5f);
        }

        /// <summary>
        /// The rect transform for the panel.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get{ return new RectTransform (GameObject.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// Create a panel within this panel.
        /// </summary>
        /// <param name="visible">Whether the new panel is visible.</param>
        [KRPCMethod]
        public Panel AddPanel (bool visible = true)
        {
            return new Panel (GameObject, visible);
        }

        /// <summary>
        /// Add text to the panel.
        /// </summary>
        /// <param name="content">The text.</param>
        /// <param name="visible">Whether the text is visible.</param>
        [KRPCMethod]
        public Text AddText (string content, bool visible = true)
        {
            return new Text (GameObject, content, visible);
        }

        /// <summary>
        /// Add an input field to the panel.
        /// </summary>
        /// <param name="visible">Whether the input field is visible.</param>
        [KRPCMethod]
        public InputField AddInputField (bool visible = true)
        {
            return new InputField (GameObject, visible);
        }

        /// <summary>
        /// Add a button to the panel.
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
