using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace KRPC.UI
{
    static class GUILayoutExtensions
    {
        public static void Init (GameObject gameObject)
        {
            ComboBoxWindow.MainInit (gameObject);
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
            style.hover.textColor = Color.yellow;
            var texture = new Texture2D (1, 1);
            texture.SetPixel (0, 0, new Color (0, 0, 0, 0));
            texture.Apply ();
            style.hover.background = texture;
            return style;
        }

        static readonly IDictionary<object, Rect> comboButtonPositions = new Dictionary<object, Rect> ();

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
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
            } else
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
                if (Options != null) {
                    int selectedOption = GUILayout.SelectionGrid (-1, Options.ToArray (), 1, OptionStyle);
                    if (selectedOption >= 0) {
                        SelectedOption = selectedOption;
                    }
                }
            }

            public void Show (object caller, IList<string> options, GUIStyle windowStyle, GUIStyle optionStyle)
            {
                Style = windowStyle;
                Visible = true;
                Caller = caller;
                SelectedOption = -1;
                Options = options;
                OptionStyle = optionStyle;
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
