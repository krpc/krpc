using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A control that is either checked or not.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    /// <remarks>
    /// On its own a toggle is a check box. Put several of them in the same
    /// <see cref="ToggleGroup" /> and they become radio buttons, as only one toggle in a
    /// group can be checked at a time.
    /// </remarks>
    [KRPCClass (Service = "UI")]
    public class Toggle : Control
    {
        /// <summary>
        /// The size of the box that is drawn to the left of the label.
        /// </summary>
        const float boxSize = 20;

        readonly UnityEngine.UI.Toggle toggle;
        readonly Text text;
        ToggleGroup group;

        internal Toggle (GameObject parent, string content, bool visible)
            : base (Widgets.Create (parent, "krpc.toggle", 160, boxSize), visible)
        {
            var style = Widgets.Style (skin => skin.toggle);
            var box = Widgets.CreateLeftEdge (GameObject, "krpc.toggle.box", boxSize);
            var background = Widgets.AddImage (box, style);
            // The check mark is drawn on top of the box, using the sprite the skin draws a
            // toggle in when it is on. Unity shows and hides it as the toggle changes.
            var check = Widgets.CreateFilling (box, "krpc.toggle.check", 0);
            var checkImage = Widgets.AddImage (check, style, s => s.active);

            var label = Widgets.CreateFilling (GameObject, "krpc.toggle.text", 0);
            label.GetComponent<UnityEngine.RectTransform> ().offsetMin =
                new Vector2 (boxSize + 4, 0);
            Widgets.AddText (label, style, UnityEngine.TextAnchor.MiddleLeft);
            text = new Text (label);
            text.Content = content;

            toggle = GameObject.AddComponent<UnityEngine.UI.Toggle> ();
            toggle.targetGraphic = background;
            toggle.graphic = checkImage;
            toggle.isOn = false;
            Widgets.AddTransition (toggle, style);
            toggle.onValueChanged.AddListener (x => {
                Changed = true;
            });
        }

        /// <inheritdoc />
        protected override UnityEngine.UI.Selectable Selectable {
            get { return toggle; }
        }

        /// <summary>
        /// The text for the toggle.
        /// </summary>
        [KRPCProperty]
        public Text Text {
            get { return text; }
        }

        /// <summary>
        /// Whether the toggle is checked.
        /// </summary>
        /// <remarks>
        /// Checking a toggle that belongs to a <see cref="ToggleGroup" /> unchecks the rest
        /// of that group, whether or not it is on the screen.
        /// </remarks>
        [KRPCProperty]
        public bool Checked {
            get { return toggle.isOn; }
            set {
                var toggleGroup = Group;
                if (toggleGroup == null)
                    SetChecked (value);
                else
                    toggleGroup.Check (this, value);
            }
        }

        /// <summary>
        /// Check or uncheck the toggle, without telling its listeners. Unity notifies them
        /// of a change made by a client just as it does one made by the user, so a client
        /// setting a value would otherwise see it reported back as the user's.
        /// </summary>
        internal void SetChecked (bool value)
        {
            toggle.SetIsOnWithoutNotify (value);
        }

        /// <summary>
        /// The group that the toggle belongs to, or <c>null</c> if it does not belong to one.
        /// </summary>
        /// <remarks>
        /// Only one toggle in a group can be checked at a time, so a group turns a set of
        /// toggles into radio buttons. A group does not have to contain the toggles, so
        /// they can be grouped independently of how they are laid out.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public ToggleGroup Group {
            get {
                // A group that has been removed is dropped as it is found, so that the
                // toggle does not hand out a group that is no longer there.
                if (group != null && !group.Exists)
                    group = null;
                return group;
            }
            set {
                // Unity tells the listeners of a group's toggles when a checked toggle
                // joins it, which is not a change the user made. So the toggle leaves the
                // group it is in and joins the new one unchecked, and is checked again
                // once it is a member: that unchecks the rest of the new group, as it
                // would had the toggle been checked while already a member of it.
                var wasChecked = Checked;
                Checked = false;
                var current = Group;
                if (current != null)
                    current.RemoveMember (this);
                group = value;
                toggle.group = value == null ? null : value.InnerGroup;
                if (value != null)
                    value.AddMember (this);
                Checked = wasChecked;
            }
        }

        /// <summary>
        /// Whether the toggle has been changed.
        /// </summary>
        /// <remarks>
        /// This property is set to true when the user changes the toggle, and not when a
        /// client checks or unchecks it.
        /// A client script should reset the property to false in order to detect subsequent changes.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }
    }
}
