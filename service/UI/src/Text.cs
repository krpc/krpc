using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A text label. See <see cref="Panel.AddText" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Text : Object
    {
        readonly UnityEngine.UI.Text text;

        internal Text (GameObject parent, string content, bool visible)
            : base (Addon.Instantiate (parent, "Text"), visible)
        {
            text = GameObject.GetComponent<UnityEngine.UI.Text> ();
            Content = content;
        }

        internal Text (GameObject obj)
            : base (obj, true, false)
        {
            text = obj.GetComponent<UnityEngine.UI.Text> ();
        }

        /// <summary>
        /// The rect transform for the text.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get{ return new RectTransform (GameObject.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// A list of all available fonts.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
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
        [KRPCProperty]
        public string Font {
            get { return text.font.name; }
            set {
                if (!AvailableFonts.Contains (value))
                    throw new ArgumentException ("Font does not exist");
                text.font = UnityEngine.Font.CreateDynamicFontFromOSFont (value, 16);
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
        /// Set the color
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return text.color.ToTuple (); }
            set { text.color = value.ToColor (); }
        }
    }
}
