using System;
using KRPC.Service.Attributes;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// The type of a Raster Prop Monitor variable. See <see cref="Computer.VariableType"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "RasterPropMonitor")]
    public enum VariableType
    {
        /// <summary>
        /// A number. Access using <see cref="Computer.NumberVariable"/>.
        /// </summary>
        Number,
        /// <summary>
        /// A string. Access using <see cref="Computer.StringVariable"/>.
        /// </summary>
        String
    }
}
