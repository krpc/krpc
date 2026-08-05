using System;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.UI
{
    /// <summary>
    /// Sizes a panel to fit what it contains, rather than the other way round.
    /// See <see cref="Panel.SizeFitter" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class SizeFitter : Equatable<SizeFitter>
    {
        readonly UnityEngine.UI.ContentSizeFitter fitter;

        internal SizeFitter (UnityEngine.UI.ContentSizeFitter innerFitter)
        {
            fitter = innerFitter;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (SizeFitter other)
        {
            return !ReferenceEquals (other, null) && fitter == other.fitter;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return fitter.GetHashCode ();
        }

        /// <summary>
        /// How the width of the panel is chosen.
        /// </summary>
        [KRPCProperty]
        public ContentSizeFit HorizontalFit {
            get { return fitter.horizontalFit.ToContentSizeFit (); }
            set { fitter.horizontalFit = value.FromContentSizeFit (); }
        }

        /// <summary>
        /// How the height of the panel is chosen.
        /// </summary>
        [KRPCProperty]
        public ContentSizeFit VerticalFit {
            get { return fitter.verticalFit.ToContentSizeFit (); }
            set { fitter.verticalFit = value.FromContentSizeFit (); }
        }
    }
}
