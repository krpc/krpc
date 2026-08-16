using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// Text. Created using <see cref="Drawing.AddText" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Text : Drawable<Text>
    {
        readonly MeshRenderer renderer;
        readonly TextMesh mesh;
        Vector3d position;
        QuaternionD rotation;

        internal Text (string content, ReferenceFrame referenceFrame, Vector3d textPosition, QuaternionD textRotation, bool visible)
            : base (typeof(MeshRenderer))
        {
            mesh = GameObject.AddComponent<TextMesh> ();
            mesh.text = content;
            mesh.fontSize = 12;
            mesh.color = UnityEngine.Color.white;
            renderer = GameObject.GetComponent<MeshRenderer> ();
            renderer.material = mesh.font.material;
            ReferenceFrame = referenceFrame;
            position = textPosition;
            rotation = textRotation;
            Visible = visible;
        }

        /// <summary>
        /// Update the text.
        /// </summary>
        public override void Update ()
        {
            if (!CanBeDrawn) {
                renderer.enabled = false;
                return;
            }
            renderer.enabled = Visible;
            renderer.transform.position = ReferenceFrame.PositionToWorldSpace (position);
            renderer.transform.rotation = ReferenceFrame.RotationToWorldSpace (rotation);
        }

        /// <summary>
        /// Position of the text.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Position {
            get { return position.ToTuple (); }
            set { position = value.ToVector (); }
        }

        /// <summary>
        /// Rotation of the text as a quaternion.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return rotation.ToTuple (); }
            set { rotation = value.ToQuaternion (); }
        }

        /// <summary>
        /// Destroy the drawable.
        /// </summary>
        public override void Destroy ()
        {
            UnityEngine.Object.Destroy (mesh);
            base.Destroy ();
        }

        // The text mesh, checked to still exist.
        TextMesh Mesh {
            get {
                CheckExists ();
                return mesh;
            }
        }

        /// <summary>
        /// A list of all available fonts.
        /// </summary>
        [KRPCMethod]
        public static IList<string> AvailableFonts ()
        {
            return UnityEngine.Font.GetOSInstalledFontNames ().ToList ();
        }

        /// <summary>
        /// The text string
        /// </summary>
        [KRPCProperty]
        public string Content {
            get { return Mesh.text; }
            set { Mesh.text = value; }
        }

        /// <summary>
        /// Name of the font
        /// </summary>
        [KRPCProperty]
        public string Font {
            get { return Mesh.font.name; }
            set {
                if (!AvailableFonts ().Contains (value))
                    throw new ArgumentException ("Font does not exist");
                var textMesh = Mesh;
                textMesh.font = UnityEngine.Font.CreateDynamicFontFromOSFont (value, 1024);
                renderer.material = textMesh.font.material;
            }
        }

        /// <summary>
        /// Font size.
        /// </summary>
        [KRPCProperty]
        public int Size {
            get { return Mesh.fontSize; }
            set { Mesh.fontSize = value; }
        }

        /// <summary>
        /// Character size.
        /// </summary>
        [KRPCProperty]
        public float CharacterSize {
            get { return Mesh.characterSize; }
            set { Mesh.characterSize = value; }
        }

        /// <summary>
        /// Font style.
        /// </summary>
        [KRPCProperty]
        public UI.FontStyle Style {
            get { return Mesh.fontStyle.ToFontStyle (); }
            set { Mesh.fontStyle = value.FromFontStyle (); }
        }

        /// <summary>
        /// Alignment.
        /// </summary>
        [KRPCProperty]
        public UI.TextAlignment Alignment {
            get { return Mesh.alignment.ToTextAlignment (); }
            set { Mesh.alignment = value.FromTextAlignment (); }
        }

        /// <summary>
        /// Line spacing.
        /// </summary>
        [KRPCProperty]
        public float LineSpacing {
            get { return Mesh.lineSpacing; }
            set { Mesh.lineSpacing = value; }
        }

        /// <summary>
        /// Anchor.
        /// </summary>
        [KRPCProperty]
        public UI.TextAnchor Anchor {
            get { return Mesh.anchor.ToTextAnchor (); }
            set { Mesh.anchor = value.FromTextAnchor (); }
        }

        /// <summary>
        /// Set the color
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return Mesh.color.ToTuple (); }
            set { Mesh.color = value.ToColor (); }
        }
    }
}
