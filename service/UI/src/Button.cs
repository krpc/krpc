using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A button that the user can click.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Button : Control
    {
        readonly UnityEngine.UI.Button button;
        readonly PressHandler press;
        readonly Text text;

        internal Button (GameObject parent, string content, bool visible)
            : base (Widgets.Create (parent, "krpc.button", 160, 30), visible)
        {
            var style = Widgets.Style (skin => skin.button);
            var background = Widgets.AddImage (GameObject, style);
            button = GameObject.AddComponent<UnityEngine.UI.Button> ();
            button.targetGraphic = background;
            Widgets.AddTransition (button, style);
            press = GameObject.AddComponent<PressHandler> ();
            var label = Widgets.CreateFilling (GameObject, "krpc.button.text", 0);
            Widgets.AddText (label, style, UnityEngine.TextAnchor.MiddleCenter);
            text = new Text (label);
            text.Content = content;
            button.onClick.AddListener (() => {
                Clicked = true;
            });
        }

        /// <inheritdoc />
        protected override UnityEngine.UI.Selectable Selectable {
            get { return button; }
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

        /// <summary>
        /// Whether the user is holding the button down right now.
        /// </summary>
        /// <remarks>
        /// True from the button being pressed until the pointer is released, however far
        /// it has moved by then. Polling this repeats an action for as long as the
        /// button is held, for nudging a value; <see cref="Clicked" /> reports each
        /// completed click instead.
        /// </remarks>
        [KRPCProperty]
        public bool Pressed {
            get { return press.Pressed; }
        }
    }
}
