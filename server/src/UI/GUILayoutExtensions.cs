#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KRPC.UI
{
    public static class GUILayoutExtensions
    {
        public static void Init (GameObject gameObject)
        {
            ComboBoxWindow.MainInit (gameObject);
        }

        /// <summary>
        /// A text field that is tinted with the given colour when its current value is not valid.
        /// </summary>
        public static string ValidatedTextField (string value, int maxLength, GUIStyle style, bool valid, Color invalidColor)
        {
            Color background, content;
            BeginTint (valid, invalidColor, out background, out content);
            var result = GUILayout.TextField (value, maxLength, style);
            EndTint (background, content);
            return result;
        }

        /// <summary>
        /// A scrolling text field that is tinted with the given colour when its current value
        /// is not valid.
        /// </summary>
        public static string ValidatedScrollingTextField (string name, string value, ref float offset, int maxLength, GUIStyle style, bool valid, Color invalidColor)
        {
            Color background, content;
            BeginTint (valid, invalidColor, out background, out content);
            var result = ScrollingTextField (name, value, ref offset, maxLength, style);
            EndTint (background, content);
            return result;
        }

        static void BeginTint (bool valid, Color invalidColor, out Color background, out Color content)
        {
            background = GUI.backgroundColor;
            content = GUI.contentColor;
            if (!valid) {
                GUI.backgroundColor = invalidColor;
                GUI.contentColor = invalidColor;
            }
        }

        static void EndTint (Color background, Color content)
        {
            GUI.backgroundColor = background;
            GUI.contentColor = content;
        }

        /// <summary>
        /// A single line of text, for giving a field the height of one line whatever it holds.
        /// </summary>
        static readonly GUIContent singleLine = new GUIContent (" ");

        /// <summary>
        /// How much room to leave beyond the caret, so that it is never drawn against the edge
        /// of the field and the character it sits after stays readable.
        /// </summary>
        const float caretRoom = 6f;

        /// <summary>
        /// A text field whose content scrolls sideways to keep the caret in view, for a value
        /// too long to fit. A text field clips what it cannot fit and leaves the caret outside,
        /// so the end of a long value can be neither seen nor edited.
        ///
        /// The field is drawn as wide as its content, inside a group the width of the field, so
        /// that what is shown is a window onto it. The offset is how far that window has been
        /// scrolled, and the caller keeps it so that each field scrolls on its own. The name has
        /// to be unique within the window, as it is what tells whether this field has the
        /// keyboard, and so whether there is a caret to follow.
        /// </summary>
        public static string ScrollingTextField (string name, string value, ref float offset, int maxLength, GUIStyle style)
        {
            var area = GUILayoutUtility.GetRect (singleLine, style, GUILayout.ExpandWidth (true));
            var content = new GUIContent (value);
            var width = Mathf.Max (area.width, style.CalcSize (content).x + caretRoom);
            // A field's width is only settled once the layout has been worked out, so the
            // window onto its content moves on the passes that know it and holds still on
            // the one that does not. A width of zero would pin the window to the caret, and
            // slide the text along behind it
            if (Event.current.type != EventType.Layout)
                offset = Mathf.Clamp (ScrolledTo (name, content, style, area.width, width, offset), 0, width - area.width);
            GUI.BeginGroup (area);
            GUI.SetNextControlName (name);
            var result = GUI.TextField (new Rect (-offset, 0, width, area.height), value, maxLength, style);
            GUI.EndGroup ();
            return result;
        }

        /// <summary>
        /// Where the window onto a field's content has to be for its caret to be visible: where
        /// it already is, unless the caret has moved out of view, and then only as far as is
        /// needed to bring it back. A field that does not have the keyboard has no caret to
        /// follow, so its window stays where it was left.
        /// </summary>
        static float ScrolledTo (string name, GUIContent content, GUIStyle style, float visible, float width, float offset)
        {
            if (GUI.GetNameOfFocusedControl () != name)
                return offset;
            var editor = GUIUtility.GetStateObject (typeof (TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (editor == null)
                return offset;
            var index = Mathf.Clamp (editor.cursorIndex, 0, content.text.Length);
            var caret = style.GetCursorPixelPosition (new Rect (0, 0, width, 0), content, index).x;
            if (offset > caret - caretRoom)
                return caret - caretRoom;
            if (offset < caret - visible + caretRoom)
                return caret - visible + caretRoom;
            return offset;
        }

        /// <summary>
        /// Strip any character that is not a digit.
        /// </summary>
        public static string FilterDigits (string value)
        {
            return Filter (value, false);
        }

        /// <summary>
        /// Strip any character that is not a digit or a period.
        /// </summary>
        public static string FilterDigitsAndPeriods (string value)
        {
            return Filter (value, true);
        }

        static string Filter (string value, bool allowPeriod)
        {
            var result = new StringBuilder (value.Length);
            foreach (char c in value)
                if ((c >= '0' && c <= '9') || (allowPeriod && c == '.'))
                    result.Append (c);
            return result.ToString ();
        }

        public static void Destroy ()
        {
            ComboBoxWindow.MainDestroy ();
        }

        public static void OnGUI ()
        {
            ComboBoxWindow.MainUpdateGUI ();
        }

        public static GUIStyle SeparatorStyle (Color color)
        {
            var style = new GUIStyle ();
            var texture = new Texture2D (1, 1);
            texture.SetPixel (0, 0, color);
            texture.Apply ();
            style.normal.background = texture;
            return style;
        }

        public static void Separator (GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Label (string.Empty, style, options);
        }

        public static GUIStyle LightStyle ()
        {
            var style = new GUIStyle (HighLogic.Skin.toggle);
            style.active = HighLogic.Skin.toggle.normal;
            style.focused = HighLogic.Skin.toggle.normal;
            style.hover = HighLogic.Skin.toggle.normal;
            SetLightStyleSize (style, style.lineHeight);
            style.padding = new RectOffset (0, 0, 0, 0);
            style.overflow = new RectOffset (0, 0, 0, 0);
            style.imagePosition = ImagePosition.ImageOnly;
            style.clipping = TextClipping.Overflow;
            return style;
        }

        public static void SetLightStyleSize (GUIStyle style, float size)
        {
            style.fixedWidth = size;
            style.fixedHeight = size;
            var offset = (int)(-0.8 * size);
            style.border = new RectOffset (offset - 4, offset + 4, offset + 4, offset - 4);
            style.margin = new RectOffset (4, 0, 0, 0);
        }

        public static void Light (bool enabled, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Toggle (enabled, string.Empty, style, options);
        }

        public static GUIStyle ComboOptionsStyle ()
        {
            var style = new GUIStyle (Skin.DefaultSkin.window);
            var texture = new Texture2D (16, 16, TextureFormat.RGBA32, false);
            const int border = 2;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height; y++) {
                    if (x < border || x > texture.width - border || y < border || y > texture.height - border)
                        texture.SetPixel (x, y, new Color (0, 0, 0, 0));
                    else
                        texture.SetPixel (x, y, new Color (0, 0, 0, 0.9f));
                }
            }
            texture.Apply ();
            style.normal.background = texture;
            style.onNormal.background = texture;
            style.border.top = style.border.bottom;
            style.padding.top = style.padding.bottom;
            return style;
        }

        public static GUIStyle ComboOptionStyle ()
        {
            var style = new GUIStyle (Skin.DefaultSkin.label);
            // Highlighting of the hovered option (a gray background and lighter text) is
            // handled in ComboBoxWindow.Draw. Give the text a known base color so that the
            // manual GUI.contentColor tint is predictable, and clear the built-in
            // interactive states so that they never recolor the text from a stale hover
            // position
            style.normal.textColor = Color.white;
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.onHover.textColor = style.normal.textColor;
            style.onActive.textColor = style.normal.textColor;
            var texture = new Texture2D (1, 1);
            texture.SetPixel (0, 0, new Color (0, 0, 0, 0));
            texture.Apply ();
            style.hover.background = texture;
            style.active.background = texture;
            return style;
        }

        static readonly IDictionary<object, Rect> comboButtonPositions = new Dictionary<object, Rect> ();

        public static int ComboBox (object caller, int selectedItem, IList<string> entries, GUIStyle buttonStyle, GUIStyle optionsStyle, GUIStyle optionStyle)
        {
            // Main button. Expand to fill the row so that combo boxes line up with the text
            // fields in the edit-server form, and left-align the label to match the
            // drop-down items. buttonStyle is shared with the action buttons, so its
            // alignment is restored below
            var oldAlignment = buttonStyle.alignment;
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            var clicked = GUILayout.Button (entries [selectedItem], buttonStyle, GUILayout.ExpandWidth (true));
            buttonStyle.alignment = oldAlignment;
            if (clicked) {
                if (ComboBoxWindow.Instance.Caller != caller || !ComboBoxWindow.Instance.Visible) {
                    ComboBoxWindow.Instance.Show (caller, entries, optionsStyle, optionStyle);
                } else if (ComboBoxWindow.Instance.Caller == caller && ComboBoxWindow.Instance.Visible) {
                    ComboBoxWindow.Instance.Hide ();
                }
            }

            // On repaint, store the position of the main button so that it can
            // be used to set the position of the combo window later
            if (Event.current.type == EventType.Repaint) {
                var position = GUILayoutUtility.GetLastRect ();
                // Convert from GUI-space (window-relative) to screen coordinates
                var screenPosition = GUIUtility.GUIToScreenPoint (new Vector2 (position.x, position.y));
                position.x = screenPosition.x;
                position.y = screenPosition.y;
                comboButtonPositions [caller] = position;
            }

            // Set the position of the combo box window
            if (ComboBoxWindow.Instance.Caller == caller && ComboBoxWindow.Instance.Visible && comboButtonPositions.ContainsKey (caller)) {
                ComboBoxWindow.Instance.SetPosition (comboButtonPositions [caller]);
            }

            // Return the selected item
            if (ComboBoxWindow.Instance.Caller == caller && ComboBoxWindow.Instance.SelectedOption != -1) {
                ComboBoxWindow.Instance.Hide ();
                return ComboBoxWindow.Instance.SelectedOption;
            }
            return selectedItem;
        }

        sealed class ComboBoxWindow : Window
        {
            public static ComboBoxWindow Instance { get; private set; }

            public object Caller { get; private set; }

            public int SelectedOption { get; private set; }

            IList<string> Options { get; set; }

            GUIStyle OptionStyle { get; set; }

            bool stalePosition;

            // Screen positions of the drawn options, used to highlight the one under
            // the mouse (see Draw).
            readonly List<Rect> optionRects = new List<Rect> ();

            // Grey background drawn behind the hovered option.
            Texture2D hoverBackground;
            static readonly Color hoverTextColor = Color.white;
            static readonly Color normalTextColor = new Color (0.8f, 0.8f, 0.8f);

            public static void MainInit (GameObject gameObject)
            {
                Instance = gameObject.AddComponent<ComboBoxWindow> ();
            }

            public static void MainDestroy ()
            {
                Destroy (Instance);
            }

            public static void MainUpdateGUI ()
            {
                if (!Instance)
                    return;
                if (Event.current.type == EventType.MouseDown && !Instance.Position.Contains (Event.current.mousePosition))
                    Instance.Hide ();
            }

            protected override void Init ()
            {
                Title = string.Empty;
                Visible = false;
                Style.border.top = Style.border.bottom;
                Style.padding.top = Style.padding.bottom;
                stalePosition = true;
                hoverBackground = new Texture2D (1, 1);
                hoverBackground.SetPixel (0, 0, new Color (0.5f, 0.5f, 0.5f, 0.4f));
                hoverBackground.Apply ();
            }

            protected override void Draw (bool needRescale)
            {
                if (Options == null)
                    return;

                // Highlight the option under the mouse from the live pointer position. The
                // built-in hover state is driven by Event.current.mousePosition, which the
                // KSP runtime only refreshes when an input event is processed, so it
                // freezes between mouse movements. The options do not move once the window
                // is shown, so the rects captured on the last repaint are exact. Position
                // is top-left in screen space and Input.mousePosition is bottom-left,
                // hence the y flip
                var mouse = new Vector2 (
                    Input.mousePosition.x - Position.x,
                    Screen.height - Input.mousePosition.y - Position.y);

                while (optionRects.Count < Options.Count)
                    optionRects.Add (new Rect ());

                var contentColor = GUI.contentColor;
                for (int i = 0; i < Options.Count; i++) {
                    bool hover = optionRects [i].Contains (mouse);
                    // Draw the grey highlight behind the hovered option, using the rect
                    // captured on the previous repaint (the options do not move).
                    if (hover && Event.current.type == EventType.Repaint)
                        GUI.DrawTexture (optionRects [i], hoverBackground);
                    GUI.contentColor = hover ? hoverTextColor : normalTextColor;
                    if (GUILayout.Button (Options [i], OptionStyle))
                        SelectedOption = i;
                    if (Event.current.type == EventType.Repaint)
                        optionRects [i] = GUILayoutUtility.GetLastRect ();
                }
                GUI.contentColor = contentColor;
            }

            public void Show (object caller, IList<string> options, GUIStyle windowStyle, GUIStyle optionStyle)
            {
                Style = windowStyle;
                Visible = true;
                Caller = caller;
                SelectedOption = -1;
                Options = options;
                OptionStyle = optionStyle;
                optionRects.Clear ();
                stalePosition = true;
                GUI.BringWindowToFront (Id);
            }

            public void Hide ()
            {
                Visible = false;
                Caller = null;
                Options = null;
                OptionStyle = null;
            }

            public void SetPosition (Rect position)
            {
                if (stalePosition) {
                    Position = position;
                    stalePosition = false;
                }
            }
        }
    }
}
