using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// How a size fitter sizes a panel along one direction.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum ContentSizeFit
    {
        /// <summary>
        /// Leave the size alone.
        /// </summary>
        Unconstrained,
        /// <summary>
        /// Use the smallest size the contents will fit in.
        /// </summary>
        MinSize,
        /// <summary>
        /// Use the size the contents ask for.
        /// </summary>
        PreferredSize
    }
}
