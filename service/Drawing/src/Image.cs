using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.Drawing
{
    /// <summary>
    /// An image that markers can be drawn with. Created using <see cref="Drawing.AddImage" />.
    /// </summary>
    /// <remarks>
    /// An image is kept across scene changes, until it is removed or the client that
    /// created it disconnects.
    /// </remarks>
    [KRPCClass (Service = "Drawing")]
    public class Image : IGameObjectState
    {
        static readonly byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        static readonly byte[] jpegSignature = { 0xFF, 0xD8, 0xFF };

        readonly Texture2D texture;
        readonly Sprite sprite;
        // Whether the image has been removed. The game destroys a texture at the end of
        // the frame, so this records that it is going
        bool removed;

        internal Image (byte[] data)
        {
            // Note: LoadImage accepts data it cannot decode, so the format is checked first
            if (!StartsWith (data, pngSignature) && !StartsWith (data, jpegSignature))
                throw new ArgumentException ("The data is not a PNG or JPEG image");
            texture = new Texture2D (2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage (data)) {
                UnityEngine.Object.Destroy (texture);
                throw new ArgumentException ("The data is not a PNG or JPEG image");
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            // The texture and sprite are kept when the scene they were made in is unloaded
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), new Vector2 (0.5f, 0.5f));
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Addon.AddImage (this);
        }

        static bool StartsWith (byte[] data, byte[] signature)
        {
            if (data.Length < signature.Length)
                return false;
            for (int i = 0; i < signature.Length; i++) {
                if (data [i] != signature [i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The texture of the image.
        /// </summary>
        internal Texture2D Texture {
            get { return texture; }
        }

        /// <summary>
        /// The image as a sprite.
        /// </summary>
        internal Sprite Sprite {
            get { return sprite; }
        }

        /// <summary>
        /// Destroy the image.
        /// </summary>
        internal void Destroy ()
        {
            removed = true;
            UnityEngine.Object.Destroy (sprite);
            UnityEngine.Object.Destroy (texture);
        }

        /// <summary>
        /// The state of the image, which is its texture.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return removed || texture == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        /// <summary>
        /// Whether the image can be drawn.
        /// </summary>
        internal static bool IsLive (Image image)
        {
            return !ReferenceEquals (image, null) && image.GameObjectState == GameObjectState.Live;
        }

        void CheckExists ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                throw new ObjectDestroyedException (
                    "The image no longer exists, as it has been removed.");
        }

        /// <summary>
        /// Remove the image. A marker drawn with it goes back to its own icon.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
            Addon.RemoveImage (this);
        }

        /// <summary>
        /// The width of the image, in pixels.
        /// </summary>
        [KRPCProperty]
        public int Width {
            get {
                CheckExists ();
                return texture.width;
            }
        }

        /// <summary>
        /// The height of the image, in pixels.
        /// </summary>
        [KRPCProperty]
        public int Height {
            get {
                CheckExists ();
                return texture.height;
            }
        }
    }
}
