using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.UI.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A line. Created using <see cref="Drawing.AddLine" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Line : Drawable
    {
        readonly LineRenderer renderer;
        Vector3d start;
        Vector3d end;
        Tuple3 color;
        float thickness;

        internal Line (Vector3d start, Vector3d end, ReferenceFrame referenceFrame)
            : base ("line", typeof(LineRenderer))
        {
            renderer = GameObject.GetComponent<LineRenderer> ();
            renderer.useWorldSpace = true;
            renderer.SetVertexCount (2);
            renderer.SetPosition (0, Vector3d.zero);
            renderer.SetPosition (1, Vector3d.zero);
            this.start = start;
            this.end = end;
            ReferenceFrame = referenceFrame;
            Color = new Tuple3 (1, 1, 1);
            Thickness = 0.25f;
        }

        /// <summary>
        /// Update the line
        /// </summary>
        public override void Update ()
        {
            renderer.SetPosition (0, ReferenceFrame.PositionToWorldSpace (start));
            renderer.SetPosition (1, ReferenceFrame.PositionToWorldSpace (end));
        }

        /// <summary>
        /// Start position of the line.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Start {
            get { return start.ToTuple (); }
            set { start = value.ToVector (); }
        }

        /// <summary>
        /// End position of the line.
        /// </summary>
        [KRPCProperty]
        public Tuple3 End {
            get { return end.ToTuple (); }
            set { end = value.ToVector (); }
        }

        /// <summary>
        /// Set the color
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return color; }
            set {
                color = value;
                var rgbColor = color.ToColor ();
                renderer.SetColors (rgbColor, rgbColor);
            }
        }

        /// <summary>
        /// Set the thickness
        /// </summary>
        [KRPCProperty]
        public float Thickness {
            get { return thickness; }
            set {
                thickness = value;
                renderer.SetWidth (thickness, thickness);
            }
        }
    }
}
