using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// An input field.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class InputField : Control
    {
        readonly UnityEngine.UI.InputField inputField;
        readonly Text text;
        readonly Text placeholder;

        internal InputField (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.inputField", 160, 30), visible)
        {
            var style = Widgets.Style (skin => skin.textField);
            var background = Widgets.AddImage (GameObject, style);
            // The text is inset so that it does not run into the border drawn by the background.
            var content = Widgets.CreateFilling (GameObject, "krpc.inputField.text", 6);
            var contentText = Widgets.AddText (content, style, UnityEngine.TextAnchor.MiddleLeft);
            // An input field edits its text a character at a time, which rich text markup
            // cannot survive, and Unity requires it to be off.
            contentText.supportRichText = false;
            // The placeholder is shown while the field is empty. It is drawn faded so that a
            // hint does not read as a value the user has typed.
            var hint = Widgets.CreateFilling (GameObject, "krpc.inputField.placeholder", 6);
            var hintText = Widgets.AddText (hint, style, UnityEngine.TextAnchor.MiddleLeft);
            var hintColor = hintText.color;
            hintColor.a *= 0.5f;
            hintText.color = hintColor;
            inputField = GameObject.AddComponent<UnityEngine.UI.InputField> ();
            inputField.targetGraphic = background;
            inputField.textComponent = contentText;
            inputField.placeholder = hintText;
            Widgets.AddTransition (inputField, style);
            text = new Text (content);
            placeholder = new Text (hint);
            inputField.onValueChanged.AddListener (x => {
                Changed = true;
            });
        }

        /// <inheritdoc />
        protected override UnityEngine.UI.Selectable Selectable {
            get { return inputField; }
        }

        /// <summary>
        /// The value of the input field.
        /// </summary>
        [KRPCProperty]
        public string Value {
            get { return inputField.text; }
            // Unity notifies its listeners of a value set by a client just as it does one
            // typed by the user, so the value is set without notifying them.
            set { inputField.SetTextWithoutNotify (value); }
        }

        /// <summary>
        /// The text component of the input field.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Value"/> to get and set the value in the field.
        /// This object can be used to alter the style of the input field's text.
        /// </remarks>
        [KRPCProperty]
        public Text Text {
            get { return text; }
        }

        /// <summary>
        /// The placeholder text component of the input field.
        /// </summary>
        /// <remarks>
        /// Set its <see cref="Text.Content"/> to the hint to show while the field is empty.
        /// The hint is hidden as soon as the field has a value.
        /// </remarks>
        [KRPCProperty]
        public Text Placeholder {
            get { return placeholder; }
        }

        /// <summary>
        /// Whether the input field has been changed.
        /// </summary>
        /// <remarks>
        /// This property is set to true when the user modifies the value of the input field,
        /// and not when a client sets it.
        /// A client script should reset the property to false in order to detect subsequent changes.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }
    }
}
