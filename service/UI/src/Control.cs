using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple4 = System.Tuple<double, double, double, double>;

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
        /// The component that handles interaction, checked to still exist. Every member
        /// that reaches into the game goes through this, or through
        /// <see cref="Object.CheckedGameObject" />, so that a control the game no longer
        /// has says so rather than failing on a torn down object.
        /// </summary>
        protected UnityEngine.UI.Selectable CheckedSelectable {
            get {
                CheckExists ();
                return Selectable;
            }
        }

        /// <summary>
        /// Whether the control responds to the user.
        /// </summary>
        /// <remarks>
        /// A control that is not interactable is drawn grayed out and ignores the user,
        /// but is still visible. Set its visibility to false to hide it instead.
        /// </remarks>
        [KRPCProperty]
        public bool Interactable {
            get { return CheckedSelectable.interactable; }
            set { CheckedSelectable.interactable = value; }
        }

        /// <summary>
        /// The color the control is tinted with, as (red, green, blue, alpha). An alpha
        /// of 0 is fully transparent and 1 is fully opaque. White, the default, leaves
        /// the control drawn as the skin has it.
        /// </summary>
        /// <remarks>
        /// The tint multiplies the sprite the control is drawn with, in every state, so
        /// a control can show a state of its own, an engaged autopilot for instance, by
        /// color. It is applied to the part of the control that responds to the user:
        /// the body of a button, dropdown or input field, the box of a toggle, and the
        /// handle of a slider. A control's label is tinted through the color of its
        /// text.
        /// </remarks>
        [KRPCProperty]
        public Tuple4 Color {
            get { return CheckedSelectable.targetGraphic.color.ToRgbaTuple (); }
            set { CheckedSelectable.targetGraphic.color = value.ToColor (); }
        }

        /// <summary>
        /// A short piece of text shown beside the pointer while it rests on the
        /// control. Empty, the default, shows nothing.
        /// </summary>
        [KRPCProperty]
        public string Tooltip {
            get {
                var handler = CheckedGameObject.GetComponent<TooltipHandler> ();
                return handler == null || handler.Text == null
                    ? string.Empty : handler.Text;
            }
            set {
                var gameObject = CheckedGameObject;
                var handler = gameObject.GetComponent<TooltipHandler> ();
                if (handler == null) {
                    if (string.IsNullOrEmpty (value))
                        return;
                    handler = gameObject.AddComponent<TooltipHandler> ();
                }
                handler.Text = value;
            }
        }
    }
}
