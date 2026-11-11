using System;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.ScanSat
{
    /// <summary>
    /// The file format of an image.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "ScanSat")]
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

    static class ImageEncoder
    {
        public static void CheckQuality (ImageFormat format, int quality)
        {
            if (format == ImageFormat.JPEG && (quality < 1 || quality > 100))
                throw new ArgumentException ("The quality is not between 1 and 100");
        }

        public static byte[] Encode (Texture2D image, ImageFormat format, int quality)
        {
            switch (format) {
            case ImageFormat.PNG:
                return image.EncodeToPNG ();
            case ImageFormat.JPEG:
                return image.EncodeToJPG (quality);
            default:
                throw new ArgumentException ("The image format is not valid");
            }
        }
    }
}
