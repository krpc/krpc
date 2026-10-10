using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A line. Created using <see cref="Drawing.AddLine" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Line : FramedPath<Line>
    {
        Vector3d start;
        Vector3d end;

        internal Line (Vector3d lineStart, Vector3d lineEnd, ReferenceFrame referenceFrame, bool visible)
        {
            start = lineStart;
            end = lineEnd;
            ReferenceFrame = referenceFrame;
            Visible = visible;
        }

        /// <summary>
        /// The points of the line, in world space.
        /// </summary>
        protected override IList<Vector3d> WorldPoints ()
        {
            return new [] {
                ReferenceFrame.PositionToWorldSpace (start),
                ReferenceFrame.PositionToWorldSpace (end)
            };
        }

        /// <summary>
        /// Whether the line is closed, which it is not.
        /// </summary>
        protected override bool Closed {
            get { return false; }
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
    }
}
