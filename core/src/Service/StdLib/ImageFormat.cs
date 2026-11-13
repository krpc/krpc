using System;
using KRPC.Service.Attributes;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// The file format of an image.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "StdLib")]
    public enum ImageFormat
    {
        /// <summary>
        /// PNG, which is lossless.
        /// </summary>
        PNG,
        /// <summary>
        /// JPEG, which is lossy, and smaller and faster to encode than PNG.
        /// </summary>
        JPEG
    }
}
