using System;
using KRPC.Service.StdLib;
using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Encodes a texture as an image file.
    /// </summary>
    public static class ImageEncoder
    {
        /// <summary>
        /// Check the quality of an image in the given format. Throws an exception for a JPEG
        /// quality outside 1 to 100.
        /// </summary>
        public static void CheckQuality (ImageFormat format, int quality)
        {
            if (format == ImageFormat.JPEG && (quality < 1 || quality > 100))
                throw new ArgumentException ("The quality is not between 1 and 100");
        }

        /// <summary>
        /// The texture encoded in the given format. The quality applies to JPEG only.
        /// </summary>
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
