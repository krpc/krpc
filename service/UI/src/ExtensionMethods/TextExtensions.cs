using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.UI.ExtensionMethods
{
    /// <summary>
    /// Text extensions.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
    public static class TextExtensions
    {
        /// <summary>
        /// Convert a Unity font style to a kRPC font style.
        /// </summary>
        public static FontStyle ToFontStyle (this UnityEngine.FontStyle style)
        {
            switch (style) {
            case UnityEngine.FontStyle.Normal:
                return FontStyle.Normal;
            case UnityEngine.FontStyle.Bold:
                return FontStyle.Bold;
            case UnityEngine.FontStyle.Italic:
                return FontStyle.Italic;
            case UnityEngine.FontStyle.BoldAndItalic:
                return FontStyle.BoldAndItalic;
            default:
                throw new ArgumentOutOfRangeException (nameof (style));
            }
        }

        /// <summary>
        /// Convert a kRPC font style to a Unity font style.
        /// </summary>
        public static UnityEngine.FontStyle FromFontStyle (this FontStyle style)
        {
            switch (style) {
            case FontStyle.Normal:
                return UnityEngine.FontStyle.Normal;
            case FontStyle.Bold:
                return UnityEngine.FontStyle.Bold;
            case FontStyle.Italic:
                return UnityEngine.FontStyle.Italic;
            case FontStyle.BoldAndItalic:
                return UnityEngine.FontStyle.BoldAndItalic;
            default:
                throw new ArgumentOutOfRangeException (nameof (style));
            }
        }

        /// <summary>
        /// Convert a Unity text alignment to a kRPC text alignment.
        /// </summary>
        public static TextAlignment ToTextAlignment (this UnityEngine.TextAlignment style)
        {
            switch (style) {
            case UnityEngine.TextAlignment.Left:
                return TextAlignment.Left;
            case UnityEngine.TextAlignment.Right:
                return TextAlignment.Right;
            case UnityEngine.TextAlignment.Center:
                return TextAlignment.Center;
            default:
                throw new ArgumentOutOfRangeException (nameof (style));
            }
        }

        /// <summary>
        /// Convert a kRPC text alignment to a Unity text alignment.
        /// </summary>
        public static UnityEngine.TextAlignment FromTextAlignment (this TextAlignment style)
        {
            switch (style) {
            case TextAlignment.Left:
                return UnityEngine.TextAlignment.Left;
            case TextAlignment.Right:
                return UnityEngine.TextAlignment.Right;
            case TextAlignment.Center:
                return UnityEngine.TextAlignment.Center;
            default:
                throw new ArgumentOutOfRangeException (nameof (style));
            }
        }

        /// <summary>
        /// Convert a Unity text anchor to a kRPC text anchor.
        /// </summary>
        public static TextAnchor ToTextAnchor (this UnityEngine.TextAnchor style)
        {
            switch (style) {
            case UnityEngine.TextAnchor.LowerCenter:
                return TextAnchor.LowerCenter;
            case UnityEngine.TextAnchor.LowerLeft:
                return TextAnchor.LowerLeft;
            case UnityEngine.TextAnchor.LowerRight:
                return TextAnchor.LowerRight;
            case UnityEngine.TextAnchor.MiddleCenter:
                return TextAnchor.MiddleCenter;
            case UnityEngine.TextAnchor.MiddleLeft:
                return TextAnchor.MiddleLeft;
            case UnityEngine.TextAnchor.MiddleRight:
                return TextAnchor.MiddleRight;
            case UnityEngine.TextAnchor.UpperCenter:
                return TextAnchor.UpperCenter;
            case UnityEngine.TextAnchor.UpperLeft:
                return TextAnchor.UpperLeft;
            case UnityEngine.TextAnchor.UpperRight:
                return TextAnchor.UpperRight;
            default:
                throw new ArgumentOutOfRangeException (nameof (style));
            }
        }

        /// <summary>
        /// Convert a kRPC text anchor to a Unity text anchor.
        /// </summary>
        public static UnityEngine.TextAnchor FromTextAnchor (this TextAnchor style)
        {
            switch (style) {
            case TextAnchor.LowerCenter:
                return UnityEngine.TextAnchor.LowerCenter;
            case TextAnchor.LowerLeft:
                return UnityEngine.TextAnchor.LowerLeft;
            case TextAnchor.LowerRight:
                return UnityEngine.TextAnchor.LowerRight;
            case TextAnchor.MiddleCenter:
                return UnityEngine.TextAnchor.MiddleCenter;
            case TextAnchor.MiddleLeft:
                return UnityEngine.TextAnchor.MiddleLeft;
            case TextAnchor.MiddleRight:
                return UnityEngine.TextAnchor.MiddleRight;
            case TextAnchor.UpperCenter:
                return UnityEngine.TextAnchor.UpperCenter;
            case TextAnchor.UpperLeft:
                return UnityEngine.TextAnchor.UpperLeft;
            case TextAnchor.UpperRight:
                return UnityEngine.TextAnchor.UpperRight;
            default:
                throw new ArgumentOutOfRangeException (nameof (style));
            }
        }

        /// <summary>
        /// Convert a kRPC message position to a screen message style.
        /// </summary>
        public static ScreenMessageStyle ToScreenMessageStyle (this MessagePosition position)
        {
            switch (position) {
            case MessagePosition.BottomCenter:
                return ScreenMessageStyle.LOWER_CENTER;
            case MessagePosition.TopCenter:
                return ScreenMessageStyle.UPPER_CENTER;
            case MessagePosition.TopLeft:
                return ScreenMessageStyle.UPPER_LEFT;
            case MessagePosition.TopRight:
                return ScreenMessageStyle.UPPER_RIGHT;
            default:
                throw new ArgumentOutOfRangeException (nameof (position));
            }
        }
    }
}
