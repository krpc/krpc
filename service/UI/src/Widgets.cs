using System;
using System.Collections.Generic;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Builds the Unity user interface component graphs behind the controls, and draws them
    /// using the skin that the game uses for its own user interface.
    /// </summary>
    /// <remarks>
    /// Every part of the skin is optional. If the game does not provide a style, or a style
    /// does not provide a sprite, the control is built without it and falls back to an
    /// unstyled appearance rather than failing to be created.
    /// </remarks>
    static class Widgets
    {
        static Font font;

        static readonly IDictionary<string, Font> osFonts = new Dictionary<string, Font> ();

        /// <summary>
        /// The font of the given name, taken from the ones the operating system provides.
        /// </summary>
        /// <remarks>
        /// A dynamic font carries a texture of the characters drawn with it, and is an
        /// asset in its own right that Unity does not destroy along with whatever draws
        /// with it. One is therefore made per name and shared by every label asking for it,
        /// rather than one per label which would be left behind when the label went.
        /// </remarks>
        internal static Font OSFont (string name)
        {
            Font osFont;
            if (!osFonts.TryGetValue (name, out osFont) || osFont == null) {
                osFont = Font.CreateDynamicFontFromOSFont (name, 16);
                osFonts [name] = osFont;
            }
            return osFont;
        }

        /// <summary>
        /// Select a style from the skin the game uses for its own user interface.
        /// Returns null if the skin is not available.
        /// </summary>
        internal static UIStyle Style (Func<UISkinDef, UIStyle> select)
        {
            if (select == null)
                throw new ArgumentNullException ("select");
            var skin = UISkinManager.defaultSkin;
            return skin == null ? null : select (skin);
        }

        /// <summary>
        /// The font that text is drawn in unless a client changes it. Arial is used rather
        /// than the skin's font, which the game picks for the language it is being played
        /// in, so that a client gets the same text at the same size whoever runs it. The
        /// skin's font is the fallback for a game that has no Arial to give.
        /// </summary>
        static Font Font {
            get {
                if (font == null) {
                    font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
                    var skin = UISkinManager.defaultSkin;
                    if (font == null && skin != null)
                        font = skin.font;
                }
                return font;
            }
        }

        /// <summary>
        /// Create the game object for a control, of the given size, centered in its parent.
        /// </summary>
        internal static GameObject Create (GameObject parent, string name, float width, float height)
        {
            var obj = NewObject (parent, name);
            var rect = obj.GetComponent<UnityEngine.RectTransform> ();
            rect.anchorMin = new Vector2 (0.5f, 0.5f);
            rect.anchorMax = new Vector2 (0.5f, 0.5f);
            rect.pivot = new Vector2 (0.5f, 0.5f);
            rect.sizeDelta = new Vector2 (width, height);
            return obj;
        }

        /// <summary>
        /// Create a game object that covers its parent, for the parts of a control that are
        /// drawn on top of it, such as a button's label.
        /// </summary>
        internal static GameObject CreateFilling (GameObject parent, string name, float inset)
        {
            var obj = NewObject (parent, name);
            var rect = obj.GetComponent<UnityEngine.RectTransform> ();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2 (inset, inset);
            rect.offsetMax = new Vector2 (-inset, -inset);
            return obj;
        }

        /// <summary>
        /// Create a game object of a fixed size, against the left edge of its parent and
        /// centered vertically, for the box that a check box or radio button is drawn in.
        /// </summary>
        internal static GameObject CreateLeftEdge (GameObject parent, string name, float size)
        {
            var obj = NewObject (parent, name);
            var rect = obj.GetComponent<UnityEngine.RectTransform> ();
            rect.anchorMin = new Vector2 (0, 0.5f);
            rect.anchorMax = new Vector2 (0, 0.5f);
            rect.pivot = new Vector2 (0, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2 (size, size);
            return obj;
        }

        /// <summary>
        /// Create a game object against the top left corner of its parent, growing down and
        /// to the right, which is how the contents of a scroll view are measured.
        /// </summary>
        internal static GameObject CreateTopLeft (
            GameObject parent, string name, float width, float height)
        {
            var obj = NewObject (parent, name);
            var rect = obj.GetComponent<UnityEngine.RectTransform> ();
            rect.anchorMin = new Vector2 (0, 1);
            rect.anchorMax = new Vector2 (0, 1);
            rect.pivot = new Vector2 (0, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2 (width, height);
            return obj;
        }

        /// <summary>
        /// Create a scroll bar along one edge of its parent, with a handle that is drawn
        /// using the skin's thumb style.
        /// </summary>
        internal static UnityEngine.UI.Scrollbar CreateScrollbar (
            GameObject parent, bool vertical, float thickness)
        {
            var obj = NewObject (
                parent, vertical ? "krpc.scrollbar.vertical" : "krpc.scrollbar.horizontal");
            var rect = obj.GetComponent<UnityEngine.RectTransform> ();
            if (vertical) {
                rect.anchorMin = new Vector2 (1, 0);
                rect.anchorMax = new Vector2 (1, 1);
                rect.pivot = new Vector2 (1, 1);
                rect.sizeDelta = new Vector2 (thickness, 0);
            } else {
                rect.anchorMin = new Vector2 (0, 0);
                rect.anchorMax = new Vector2 (1, 0);
                rect.pivot = new Vector2 (0, 0);
                rect.sizeDelta = new Vector2 (0, thickness);
            }
            AddImage (obj, Style (
                skin => vertical ? skin.verticalScrollbar : skin.horizontalScrollbar));

            // Unity moves and resizes the handle within the sliding area as the view is
            // scrolled, so the handle has to be a child of the scroll bar.
            var slidingArea = CreateFilling (obj, "krpc.scrollbar.slidingArea", 0);
            var handle = CreateFilling (slidingArea, "krpc.scrollbar.handle", 0);
            var thumb = Style (
                skin => vertical ? skin.verticalScrollbarThumb : skin.horizontalScrollbarThumb);
            var handleImage = AddImage (handle, thumb);

            var scrollbar = obj.AddComponent<UnityEngine.UI.Scrollbar> ();
            scrollbar.handleRect = handle.GetComponent<UnityEngine.RectTransform> ();
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = vertical
                ? UnityEngine.UI.Scrollbar.Direction.BottomToTop
                : UnityEngine.UI.Scrollbar.Direction.LeftToRight;
            AddTransition (scrollbar, thumb);
            return scrollbar;
        }

        /// <summary>
        /// Keep an object that is not drawn, such as a toggle group, from being given space
        /// of its own by a layout group.
        /// </summary>
        internal static void IgnoreLayout (GameObject obj)
        {
            obj.AddComponent<UnityEngine.UI.LayoutElement> ().ignoreLayout = true;
        }

        static GameObject NewObject (GameObject parent, string name)
        {
            // Creating the object with a rect transform, rather than adding one afterwards,
            // avoids it being given the plain transform that a bare game object gets.
            var obj = new GameObject (name, typeof(UnityEngine.RectTransform));
            obj.transform.SetParent (parent.transform, false);
            return obj;
        }

        /// <summary>
        /// Draw the given object using the background sprite a style uses when it is in its
        /// normal state.
        /// </summary>
        internal static UnityEngine.UI.Image AddImage (GameObject obj, UIStyle style)
        {
            return AddImage (obj, style, s => s.normal);
        }

        /// <summary>
        /// Draw the given object using the background sprite a style uses in the state
        /// picked out by the given selector.
        /// </summary>
        internal static UnityEngine.UI.Image AddImage (
            GameObject obj, UIStyle style, Func<UIStyle, UIStyleState> select)
        {
            if (select == null)
                throw new ArgumentNullException ("select");
            var image = obj.AddComponent<UnityEngine.UI.Image> ();
            var sprite = Background (style, style == null ? null : select (style));
            if (sprite != null) {
                image.sprite = sprite;
                // The skin's sprites carry borders, so they are stretched by their edges
                // rather than as a whole and controls can be resized without distortion.
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            return image;
        }

        /// <summary>
        /// Draw the given object as text, using a style's font and color.
        /// </summary>
        internal static UnityEngine.UI.Text AddText (
            GameObject obj, UIStyle style, UnityEngine.TextAnchor alignment)
        {
            var text = obj.AddComponent<UnityEngine.UI.Text> ();
            text.font = Font;
            text.fontSize = style != null && style.fontSize > 0 ? style.fontSize : 14;
            text.alignment = alignment;
            text.color = style != null && style.normal != null
                ? style.normal.textColor : UnityEngine.Color.white;
            return text;
        }

        /// <summary>
        /// Make a control swap between a style's sprites as it is highlighted, pressed and
        /// disabled, which is what gives a disabled control its grayed out appearance.
        /// </summary>
        internal static void AddTransition (
            UnityEngine.UI.Selectable selectable, UIStyle style)
        {
            if (style == null || Background (style, style.normal) == null)
                return;
            var state = new UnityEngine.UI.SpriteState ();
            state.highlightedSprite = Background (style, style.highlight);
            state.pressedSprite = Background (style, style.active);
            state.disabledSprite = Background (style, style.disabled);
            selectable.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            selectable.spriteState = state;
        }

        static Sprite Background (UIStyle style, UIStyleState state)
        {
            return style == null || state == null ? null : state.background;
        }
    }
}
