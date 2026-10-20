using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.UI.ExtensionMethods;
using UnityEngine;

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

        // The game's text component, checked to still exist.
        UnityEngine.UI.Text Internal {
            get {
                CheckExists ();
                return text;
            }
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
            get { return Internal.text; }
            set { Internal.text = value; }
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
            get {
                var font = Internal.font;
                return font == null ? string.Empty : font.name;
            }
            set {
                if (!AvailableFonts.Contains (value))
                    throw new ArgumentException ("Font does not exist");
                Internal.font = Widgets.OSFont (value);
            }
        }

        /// <summary>
        /// Font size.
        /// </summary>
        [KRPCProperty]
        public int Size {
            get { return Internal.fontSize; }
            set { Internal.fontSize = value; }
        }

        /// <summary>
        /// Font style.
        /// </summary>
        [KRPCProperty]
        public FontStyle Style {
            get { return Internal.fontStyle.ToFontStyle (); }
            set { Internal.fontStyle = value.FromFontStyle (); }
        }

        /// <summary>
        /// Alignment.
        /// </summary>
        [KRPCProperty]
        public TextAnchor Alignment {
            get { return Internal.alignment.ToTextAnchor (); }
            set { Internal.alignment = value.FromTextAnchor (); }
        }

        /// <summary>
        /// Line spacing.
        /// </summary>
        [KRPCProperty]
        public float LineSpacing {
            get { return Internal.lineSpacing; }
            set { Internal.lineSpacing = value; }
        }

        /// <summary>
        /// Whether the text wraps onto further lines when it is wider than the space it
        /// has. Text that does not wrap carries on past the edge, and its preferred size
        /// is that of a single line, so a changing value leaves a layout in place.
        /// </summary>
        [KRPCProperty]
        public bool WordWrap {
            get { return Internal.horizontalOverflow == HorizontalWrapMode.Wrap; }
            set {
                Internal.horizontalOverflow = value
                    ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            }
        }

        /// <summary>
        /// The color the text is drawn in.
        /// </summary>
        [KRPCProperty]
        public ColorRGBA Color {
            get { return Internal.color.ToColorRGBA (); }
            set { Internal.color = value.ToColor (); }
        }
    }
}
