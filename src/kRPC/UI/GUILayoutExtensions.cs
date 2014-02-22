using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KRPC.UI
{
    public static class GUILayoutExtensions
    {
        public static GUIStyle SeparatorStyle (Color color)
        {
            var style = new GUIStyle ();
            Texture2D texture = new Texture2D (1, 1);
            texture.SetPixel (0, 0, color);
            texture.Apply ();
            style.normal.background = texture;
            return style;
        }

        public static void Separator (GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Label ("", style, options);
        }

        public static GUIStyle LightStyle ()
        {
            var style = new GUIStyle (HighLogic.Skin.toggle);
            style.active = HighLogic.Skin.toggle.normal;
            style.focused = HighLogic.Skin.toggle.normal;
            style.hover = HighLogic.Skin.toggle.normal;
            float size = style.lineHeight;
            style.fixedWidth = size;
            style.fixedHeight = size;
            int offset = (int)(-0.8 * size);
            style.border = new RectOffset (offset - 4, offset + 4, offset + 4, offset - 4);
            style.padding = new RectOffset (0, 0, 0, 0);
            style.overflow = new RectOffset (0, 0, 0, 0);
            style.margin = new RectOffset (4, 0, 0, 0);
            style.imagePosition = ImagePosition.ImageOnly;
            style.clipping = TextClipping.Overflow;
            return style;
        }

        public static void Light (bool enabled, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Toggle (enabled, "", style, options);
        }
    }
}
