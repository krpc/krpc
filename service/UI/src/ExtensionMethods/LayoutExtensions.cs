using System;

namespace KRPC.UI.ExtensionMethods
{
    /// <summary>
    /// Extension methods for layout enumerations.
    /// </summary>
    public static class LayoutExtensions
    {
        /// <summary>
        /// Convert a Unity engine grid layout constraint to a kRPC one.
        /// </summary>
        public static GridConstraint ToGridConstraint (
            this UnityEngine.UI.GridLayoutGroup.Constraint constraint)
        {
            switch (constraint) {
            case UnityEngine.UI.GridLayoutGroup.Constraint.Flexible:
                return GridConstraint.Flexible;
            case UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount:
                return GridConstraint.FixedColumnCount;
            case UnityEngine.UI.GridLayoutGroup.Constraint.FixedRowCount:
                return GridConstraint.FixedRowCount;
            default:
                throw new ArgumentOutOfRangeException ("constraint");
            }
        }

        /// <summary>
        /// Convert a kRPC grid layout constraint to a Unity engine one.
        /// </summary>
        public static UnityEngine.UI.GridLayoutGroup.Constraint FromGridConstraint (
            this GridConstraint constraint)
        {
            switch (constraint) {
            case GridConstraint.Flexible:
                return UnityEngine.UI.GridLayoutGroup.Constraint.Flexible;
            case GridConstraint.FixedColumnCount:
                return UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            case GridConstraint.FixedRowCount:
                return UnityEngine.UI.GridLayoutGroup.Constraint.FixedRowCount;
            default:
                throw new ArgumentOutOfRangeException ("constraint");
            }
        }

        /// <summary>
        /// Convert a Unity engine content size fit mode to a kRPC one.
        /// </summary>
        public static ContentSizeFit ToContentSizeFit (
            this UnityEngine.UI.ContentSizeFitter.FitMode mode)
        {
            switch (mode) {
            case UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained:
                return ContentSizeFit.Unconstrained;
            case UnityEngine.UI.ContentSizeFitter.FitMode.MinSize:
                return ContentSizeFit.MinSize;
            case UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize:
                return ContentSizeFit.PreferredSize;
            default:
                throw new ArgumentOutOfRangeException ("mode");
            }
        }

        /// <summary>
        /// Convert a kRPC content size fit mode to a Unity engine one.
        /// </summary>
        public static UnityEngine.UI.ContentSizeFitter.FitMode FromContentSizeFit (
            this ContentSizeFit mode)
        {
            switch (mode) {
            case ContentSizeFit.Unconstrained:
                return UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            case ContentSizeFit.MinSize:
                return UnityEngine.UI.ContentSizeFitter.FitMode.MinSize;
            case ContentSizeFit.PreferredSize:
                return UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            default:
                throw new ArgumentOutOfRangeException ("mode");
            }
        }
    }
}
