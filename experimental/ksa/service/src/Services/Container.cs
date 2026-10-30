using System.Collections.Generic;
using Brutal.ImGuiApi;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The members shared by the widgets that hold other widgets.
    /// </summary>
    public abstract class Container : Widget
    {
        readonly List<Widget> children = new List<Widget> ();

        /// <summary>
        /// Create a container, with no widgets.
        /// </summary>
        protected Container (bool visible) : base (visible)
        {
        }

        /// <summary>
        /// Add a label.
        /// </summary>
        /// <param name="content">The text of the label.</param>
        /// <param name="visible">Whether the label is visible.</param>
        [KRPCMethod]
        public Label AddLabel (string content, bool visible = true)
        {
            return Add (new Label (content, visible));
        }

        /// <summary>
        /// Add a button.
        /// </summary>
        /// <param name="content">The text on the button.</param>
        /// <param name="visible">Whether the button is visible.</param>
        [KRPCMethod]
        public Button AddButton (string content, bool visible = true)
        {
            return Add (new Button (content, visible));
        }

        /// <summary>
        /// Add a text input field, empty.
        /// </summary>
        /// <param name="label">The text shown next to the field.</param>
        /// <param name="visible">Whether the field is visible.</param>
        [KRPCMethod]
        public InputField AddInputField (string label = "", bool visible = true)
        {
            return Add (new InputField (label, visible));
        }

        /// <summary>
        /// Add a slider, at its minimum value.
        /// </summary>
        /// <param name="label">The text shown next to the slider.</param>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <param name="visible">Whether the slider is visible.</param>
        /// <remarks>Throws an exception if the minimum is not less than the maximum.</remarks>
        [KRPCMethod]
        public Slider AddSlider (string label = "", float minimum = 0, float maximum = 1, bool visible = true)
        {
            return Add (new Slider (label, minimum, maximum, visible));
        }

        /// <summary>
        /// Add a toggle, unchecked.
        /// </summary>
        /// <param name="content">The text shown next to the toggle.</param>
        /// <param name="visible">Whether the toggle is visible.</param>
        [KRPCMethod]
        public Toggle AddToggle (string content, bool visible = true)
        {
            return Add (new Toggle (content, visible));
        }

        /// <summary>
        /// Add a dropdown, with no option selected.
        /// </summary>
        /// <param name="options">The options to choose from.</param>
        /// <param name="label">The text shown next to the dropdown.</param>
        /// <param name="visible">Whether the dropdown is visible.</param>
        [KRPCMethod]
        public Dropdown AddDropdown (IList<string> options, string label = "", bool visible = true)
        {
            return Add (new Dropdown (label, options, visible));
        }

        /// <summary>
        /// Add a horizontal line.
        /// </summary>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCMethod]
        public Separator AddSeparator (bool visible = true)
        {
            return Add (new Separator (visible));
        }

        /// <summary>
        /// Add a row, which lays out its widgets from left to right.
        /// </summary>
        /// <param name="visible">Whether the row is visible.</param>
        [KRPCMethod]
        public Row AddRow (bool visible = true)
        {
            return Add (new Row (visible));
        }

        T Add<T> (T widget) where T : Widget
        {
            widget.Parent = this;
            children.Add (widget);
            return widget;
        }

        /// <summary>
        /// Remove a widget from the container.
        /// </summary>
        internal void Remove (Widget widget)
        {
            if (!children.Remove (widget))
                return;
            widget.Removed ();
            // Calls through the removed widgets then raise an object destroyed exception
            GameState.Sweep ();
        }

        internal override void Removed ()
        {
            base.Removed ();
            foreach (var child in children)
                child.Removed ();
        }

        /// <summary>
        /// Draw the visible widgets, one per line or all on one line.
        /// </summary>
        protected void DrawChildren (bool sameLine)
        {
            var first = true;
            foreach (var child in children) {
                if (!child.Visible)
                    continue;
                if (sameLine && !first)
                    ImGui.SameLine ();
                child.Draw ();
                first = false;
            }
        }
    }
}
