using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// What fixes the shape of a grid layout.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum GridConstraint
    {
        /// <summary>
        /// Fit as many columns and rows as the size of the panel allows.
        /// </summary>
        Flexible,
        /// <summary>
        /// Use a fixed number of columns, and as many rows as are needed.
        /// </summary>
        FixedColumnCount,
        /// <summary>
        /// Use a fixed number of rows, and as many columns as are needed.
        /// </summary>
        FixedRowCount
    }
}
