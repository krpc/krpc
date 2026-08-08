using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A set of toggles of which only one can be checked at a time, which is what turns
    /// a set of <see cref="Toggle" /> controls into radio buttons.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    /// <remarks>
    /// A group does not contain its toggles, it only refers to them, so toggles can be
    /// grouped independently of how they are laid out. A column of radio buttons in a grid
    /// of them is one group, even though each row is laid out separately.
    /// </remarks>
    [KRPCClass (Service = "UI")]
    public class ToggleGroup : Object
    {
        readonly UnityEngine.UI.ToggleGroup group;
        readonly IList<Toggle> members = new List<Toggle> ();

        internal ToggleGroup (GameObject parent)
            : base (Widgets.Create (parent, "krpc.toggleGroup", 0, 0), true)
        {
            group = GameObject.AddComponent<UnityEngine.UI.ToggleGroup> ();
            group.allowSwitchOff = true;
            // A group is not drawn, so it must not be given space of its own by a layout.
            Widgets.IgnoreLayout (GameObject);
        }

        internal UnityEngine.UI.ToggleGroup InnerGroup {
            // Checked, so that a toggle joining a group the game no longer has says so
            // rather than being tied to a torn down one.
            get { return Internal; }
        }

        // The game's toggle group, checked to still exist.
        UnityEngine.UI.ToggleGroup Internal {
            get {
                CheckExists ();
                return group;
            }
        }

        internal void AddMember (Toggle toggle)
        {
            if (!members.Contains (toggle))
                members.Add (toggle);
        }

        internal void RemoveMember (Toggle toggle)
        {
            members.Remove (toggle);
        }

        /// <summary>
        /// The toggles that are still in the group. Removed ones are dropped as they are
        /// found, so that a group does not keep hold of them and does not ask a destroyed
        /// toggle whether it is checked.
        /// </summary>
        IList<Toggle> Members {
            get {
                for (var i = members.Count - 1; i >= 0; i--) {
                    if (!members [i].Exists)
                        members.RemoveAt (i);
                }
                return members.ToList ();
            }
        }

        /// <summary>
        /// Check or uncheck one of the toggles in the group, keeping to the group's rule
        /// that only one of them is checked at a time.
        /// </summary>
        /// <remarks>
        /// Unity applies that rule from the toggle rather than from the group, and only
        /// while the toggle is being drawn. A group built in a panel that has not been
        /// shown yet would therefore let every one of its toggles be checked at once, and
        /// would put itself right when the panel appeared, reporting the toggle it cleared
        /// as changed by the user. The rule is applied here instead, so that it holds
        /// whether or not the group is on the screen.
        /// </remarks>
        internal void Check (Toggle toggle, bool value)
        {
            // Unity refuses to uncheck the last checked toggle of a group that does not
            // allow being switched off. That rule is for the user, who can only check a
            // different toggle; a client saying what a toggle is set to is obeyed.
            var switchOff = Internal.allowSwitchOff;
            Internal.allowSwitchOff = true;
            try {
                toggle.SetChecked (value);
                if (!value)
                    return;
                foreach (var member in Members) {
                    if (!ReferenceEquals (member, toggle))
                        member.SetChecked (false);
                }
            } finally {
                Internal.allowSwitchOff = switchOff;
            }
        }

        /// <summary>
        /// The toggle in the group that is checked, or <c>null</c> if none of them are.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Toggle Selected {
            get {
                foreach (var member in Members) {
                    if (member.Checked)
                        return member;
                }
                return null;
            }
        }

        /// <summary>
        /// Whether all of the toggles in the group are allowed to be unchecked at once.
        /// </summary>
        /// <remarks>
        /// When this is false, the user cannot clear the checked toggle, they can only
        /// check a different one. It does not stop a client setting
        /// <see cref="Toggle.Checked" />, which is obeyed either way.
        /// A group starts out with no toggle checked either way.
        /// </remarks>
        [KRPCProperty]
        public bool AllowSwitchOff {
            get { return Internal.allowSwitchOff; }
            set { Internal.allowSwitchOff = value; }
        }
    }
}
