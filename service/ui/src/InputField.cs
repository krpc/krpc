using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// An input field. See <see cref="Panel.AddInputField" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class InputField : Object
    {
        readonly UnityEngine.UI.InputField inputField;

        internal InputField (GameObject parent, bool visible)
            : base (Addon.Instantiate (parent, "InputField"), visible)
        {
            inputField = GameObject.GetComponent<UnityEngine.UI.InputField> ();
            inputField.onValueChanged.AddListener (x => {
                Changed = true;
            });
        }

        /// <summary>
        /// The rect transform for the input field.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get { return new RectTransform (GameObject.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// The value of the input field.
        /// </summary>
        [KRPCProperty]
        public string Value {
            get { return inputField.text; }
            set { inputField.text = value; }
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
            get { return new Text (GameObject); }
        }

        /// <summary>
        /// Whether the input field has been changed.
        /// </summary>
        /// <remarks>
        /// This property is set to true when the user modifies the value of the input field.
        /// A client script should reset the property to false in order to detect subsequent changes.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }
    }
}
