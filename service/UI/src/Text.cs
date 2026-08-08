using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A text label.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Text : Object
    {
        readonly UnityEngine.UI.Text text;

        internal Text (GameObject parent, string content, bool visible)
            : base (Widgets.Create (parent, "krpc.text", 160, 30), visible)
        {
            text = Widgets.AddText (
                GameObject, Widgets.Style (skin => skin.label), UnityEngine.TextAnchor.UpperLeft);
            Content = content;
        }

        internal Text (GameObject obj)
            : base (obj, true, false)
        {
            text = obj.GetComponent<UnityEngine.UI.Text> ();
        }

        /// <summary>
        /// A list of all available fonts.
        /// </summary>
        [KRPCProperty]
        public IList<string> AvailableFonts {
            get { return UnityEngine.Font.GetOSInstalledFontNames ().ToList (); }
        }

        /// <summary>
        /// The text string
        /// </summary>
        [KRPCProperty]
        public string Content {
            get { return text.text; }
            set { text.text = value; }
        }

        /// <summary>
        /// Name of the font
        /// </summary>
        /// <remarks>
        /// Empty if the game provided no font to draw the text with, in which case it is
        /// not drawn. See <see cref="AvailableFonts" /> for the names that can be set.
        /// </remarks>
        [KRPCProperty]
        public string Font {
            get { return text.font == null ? string.Empty : text.font.name; }
            set {
                if (!AvailableFonts.Contains (value))
                    throw new ArgumentException ("Font does not exist");
                text.font = Widgets.OSFont (value);
            }
        }

        /// <summary>
        /// Font size.
        /// </summary>
        [KRPCProperty]
        public int Size {
            get { return text.fontSize; }
            set { text.fontSize = value; }
        }

        /// <summary>
        /// Font style.
        /// </summary>
        [KRPCProperty]
        public FontStyle Style {
            get { return text.fontStyle.ToFontStyle (); }
            set { text.fontStyle = value.FromFontStyle (); }
        }

        /// <summary>
        /// Alignment.
        /// </summary>
        [KRPCProperty]
        public TextAnchor Alignment {
            get { return text.alignment.ToTextAnchor (); }
            set { text.alignment = value.FromTextAnchor (); }
        }

        /// <summary>
        /// Line spacing.
        /// </summary>
        [KRPCProperty]
        public float LineSpacing {
            get { return text.lineSpacing; }
            set { text.lineSpacing = value; }
        }

        /// <summary>
        /// Whether the text wraps onto further lines when it is wider than the space it
        /// has. When wrapping is off, the text carries on past the edge instead, and a
        /// label asked for its preferred size asks for a single line, so a value that
        /// changes does not reflow a layout.
        /// </summary>
        [KRPCProperty]
        public bool WordWrap {
            get { return text.horizontalOverflow == HorizontalWrapMode.Wrap; }
            set {
                text.horizontalOverflow = value
                    ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            }
        }

        /// <summary>
        /// The color the text is drawn in, as (red, green, blue, alpha). An alpha of 0 is
        /// fully transparent and 1 is fully opaque.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Color {
            get { return text.color.ToRgbaTuple (); }
            set { text.color = value.ToColor (); }
        }
    }
}
