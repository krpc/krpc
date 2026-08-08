using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A control for choosing one of a list of options, which are shown when it is clicked.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Dropdown : Control
    {
        /// <summary>
        /// How tall each option in the open list is drawn, in pixels.
        /// </summary>
        const float itemHeight = 20;

        /// <summary>
        /// How tall the open list is drawn before it starts scrolling, in pixels.
        /// </summary>
        const float listHeight = 100;

        readonly UnityEngine.UI.Dropdown dropdown;

        internal Dropdown (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.dropdown", 160, 30), visible)
        {
            var style = Widgets.Style (skin => skin.button);
            var background = Widgets.AddImage (GameObject, style);

            var caption = Widgets.CreateFilling (GameObject, "krpc.dropdown.caption", 6);
            var captionText = Widgets.AddText (
                caption, style, UnityEngine.TextAnchor.MiddleLeft);

            dropdown = GameObject.AddComponent<UnityEngine.UI.Dropdown> ();
            dropdown.targetGraphic = background;
            dropdown.captionText = captionText;
            Widgets.AddTransition (dropdown, style);

            BuildTemplate (style);

            dropdown.onValueChanged.AddListener (x => {
                Changed = true;
            });
        }

        /// <summary>
        /// Build the list that is shown while the dropdown is open. Unity clones this for
        /// each option, so it is left inactive and only one item is built.
        /// </summary>
        void BuildTemplate (UIStyle style)
        {
            var template = Widgets.CreateTopLeft (
                GameObject, "krpc.dropdown.template", 0, listHeight);
            var templateRect = template.GetComponent<UnityEngine.RectTransform> ();
            // The list hangs below the dropdown and is as wide as it is.
            templateRect.anchorMin = new Vector2 (0, 0);
            templateRect.anchorMax = new Vector2 (1, 0);
            templateRect.pivot = new Vector2 (0.5f, 1);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2 (0, listHeight);
            Widgets.AddImage (template, Widgets.Style (skin => skin.window));

            var viewport = Widgets.CreateFilling (template, "krpc.dropdown.viewport", 0);
            viewport.AddComponent<UnityEngine.UI.RectMask2D> ();

            var content = Widgets.CreateTopLeft (
                viewport, "krpc.dropdown.content", 0, itemHeight);
            var contentRect = content.GetComponent<UnityEngine.RectTransform> ();
            contentRect.anchorMin = new Vector2 (0, 1);
            contentRect.anchorMax = new Vector2 (1, 1);
            contentRect.sizeDelta = new Vector2 (0, itemHeight);

            var item = Widgets.CreateTopLeft (content, "krpc.dropdown.item", 0, itemHeight);
            var itemRect = item.GetComponent<UnityEngine.RectTransform> ();
            itemRect.anchorMin = new Vector2 (0, 0.5f);
            itemRect.anchorMax = new Vector2 (1, 0.5f);
            itemRect.sizeDelta = new Vector2 (0, itemHeight);

            var itemBackground = Widgets.AddImage (
                Widgets.CreateFilling (item, "krpc.dropdown.itemBackground", 0), style);
            var itemCheck = Widgets.AddImage (
                Widgets.CreateFilling (item, "krpc.dropdown.itemCheck", 0), style, s => s.active);
            var itemLabel = Widgets.CreateFilling (item, "krpc.dropdown.itemLabel", 0);
            itemLabel.GetComponent<UnityEngine.RectTransform> ().offsetMin = new Vector2 (6, 0);
            var itemText = Widgets.AddText (
                itemLabel, style, UnityEngine.TextAnchor.MiddleLeft);

            var itemToggle = item.AddComponent<UnityEngine.UI.Toggle> ();
            itemToggle.targetGraphic = itemBackground;
            itemToggle.graphic = itemCheck;

            var scrollRect = template.AddComponent<UnityEngine.UI.ScrollRect> ();
            scrollRect.viewport = viewport.GetComponent<UnityEngine.RectTransform> ();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

            dropdown.template = templateRect;
            dropdown.itemText = itemText;
            template.SetActive (false);
        }

        /// <inheritdoc />
        protected override UnityEngine.UI.Selectable Selectable {
            get { return dropdown; }
        }

        // The game's dropdown, checked to still exist.
        UnityEngine.UI.Dropdown Internal {
            get {
                CheckExists ();
                return dropdown;
            }
        }

        /// <summary>
        /// The options that the dropdown offers.
        /// </summary>
        /// <remarks>
        /// Setting the options resets the selection to the first of them, or to nothing
        /// when the list is empty.
        /// </remarks>
        [KRPCProperty]
        public IList<string> Options {
            get { return Internal.options.Select (option => option.text).ToList (); }
            set {
                // The selection is reset while the old options are still in place, as Unity
                // ignores a selection made when there is nothing to select.
                Internal.SetValueWithoutNotify (0);
                Internal.ClearOptions ();
                if (value != null)
                    Internal.AddOptions (value.ToList ());
                Internal.RefreshShownValue ();
            }
        }

        /// <summary>
        /// The position in <see cref="Options" /> of the option that is chosen.
        /// </summary>
        /// <remarks>
        /// Zero while the dropdown has no options, as there is nothing to choose.
        /// </remarks>
        [KRPCProperty]
        public int SelectedIndex {
            get { return Internal.value; }
            set {
                // Unity moves a position outside the list to the nearest one that is in
                // it, so a client that asked for the wrong one would be left reading back
                // a value it did not set and no way to tell that it had happened.
                if (value < 0 || value >= Internal.options.Count)
                    throw new ArgumentOutOfRangeException (
                        nameof (value), "The dropdown does not have that option");
                // Unity notifies its listeners of an option chosen by a client just as it
                // does one chosen by the user, so the option is chosen without notifying them.
                Internal.SetValueWithoutNotify (value);
            }
        }

        /// <summary>
        /// Whether the chosen option has been changed.
        /// </summary>
        /// <remarks>
        /// This property is set to true when the user chooses an option, and not when a
        /// client chooses one.
        /// A client script should reset the property to false in order to detect subsequent changes.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }
    }
}
