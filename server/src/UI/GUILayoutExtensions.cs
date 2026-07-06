using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KRPC.UI
{
    static class GUILayoutExtensions
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
            var oldBackgroundColor = GUI.backgroundColor;
            var oldContentColor = GUI.contentColor;
            if (!valid) {
                GUI.backgroundColor = invalidColor;
                GUI.contentColor = invalidColor;
            }
            var result = GUILayout.TextField (value, maxLength, style);
            GUI.backgroundColor = oldBackgroundColor;
            GUI.contentColor = oldContentColor;
            return result;
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
            // Highlighting of the hovered option is handled manually in
            // ComboBoxWindow.Draw via GUI.contentColor. Neutralise the built-in
            // interactive states so they never recolour (or, in the case of the skin's
            // default hover colour, hide) the text based on the stale hover position.
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
            // Main button
            if (GUILayout.Button (entries [selectedItem], buttonStyle)) {
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
                // Convert from relative to absolute coordinates
                // TODO: ugly hack...
                Vector2 mousePosition = Input.mousePosition;
                mousePosition.y = Screen.height - mousePosition.y;
                Vector2 clippedMousePos = Event.current.mousePosition;
                position.x = position.x + mousePosition.x - clippedMousePos.x;
                position.y = position.y + mousePosition.y - clippedMousePos.y;
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
            }

            protected override void Draw (bool needRescale)
            {
                if (Options == null)
                    return;

                // Highlight the option under the mouse using the live pointer position
                // (Input.mousePosition) rather than the built-in hover state, which is
                // driven by Event.current.mousePosition. In the KSP runtime the latter is
                // only refreshed when an input event is processed, so the built-in
                // highlight freezes between mouse movements (the classic "wiggle the mouse
                // to update it" behaviour). The options do not move once the window is
                // shown, so hit-testing against the rects captured on the last repaint is
                // exact. Position is the window's top-left in screen space; Input.mousePosition
                // is bottom-left origin, hence the y flip.
                var mouse = new Vector2 (
                    Input.mousePosition.x - Position.x,
                    Screen.height - Input.mousePosition.y - Position.y);

                while (optionRects.Count < Options.Count)
                    optionRects.Add (new Rect ());

                var contentColor = GUI.contentColor;
                for (int i = 0; i < Options.Count; i++) {
                    GUI.contentColor = optionRects [i].Contains (mouse) ? Color.yellow : contentColor;
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
