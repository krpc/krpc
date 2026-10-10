using KRPC.Service.Attributes;
using KRPC.SpaceCenter.Services;

namespace KRPC.Drawing
{
    /// <summary>
    /// Abstract base class for paths whose points are positions in a reference frame.
    /// </summary>
    public abstract class FramedPath<T> : Path<T>
    {
        /// <summary>
        /// Whether the path can be drawn.
        /// </summary>
        protected override bool CanBeDrawn {
            get { return CanBeDrawnIn (ReferenceFrame); }
        }

        /// <summary>
        /// Reference frame for the positions of the object.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }
    }
}
