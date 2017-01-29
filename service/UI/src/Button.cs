using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A text label. See <see cref="Panel.AddButton" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Button : Object
    {
        readonly UnityEngine.UI.Button button;
        readonly Text text;

        internal Button (GameObject parent, string content, bool visible)
            : base (Addon.Instantiate (parent, "Button"), visible)
        {
            button = GameObject.GetComponent<UnityEngine.UI.Button> ();
            text = new Text (GameObject.GetChild ("Text"));
            text.Content = content;
            button.onClick.AddListener (() => {
                Clicked = true;
            });
        }

        /// <summary>
        /// The rect transform for the text.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get { return new RectTransform (GameObject.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// The text for the button.
        /// </summary>
        [KRPCProperty]
        public Text Text {
            get { return text; }
        }

        /// <summary>
        /// Whether the button has been clicked.
        /// </summary>
        /// <remarks>
        /// This property is set to true when the user clicks the button.
        /// A client script should reset the property to false in order to detect subsequent button presses.
        /// </remarks>
        [KRPCProperty]
        public bool Clicked { get; set; }
    }
}
