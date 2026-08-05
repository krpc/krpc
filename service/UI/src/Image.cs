using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A picture, or a plain colored rectangle.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Image : Object
    {
        readonly UnityEngine.UI.Image image;
        byte[] content = new byte[0];
        Texture2D texture;
        Sprite sprite;

        internal Image (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.image", 100, 100), visible)
        {
            image = Widgets.AddImage (GameObject, null);
        }

        /// <summary>
        /// The picture to show, as the contents of a PNG or JPEG file. Set it to an empty
        /// array to show a plain rectangle instead.
        /// </summary>
        /// <remarks>
        /// Reading this returns what was last set, not what is on the screen, and returns
        /// an empty array if no picture has been set.
        /// </remarks>
        [KRPCProperty]
        public byte[] Content {
            get { return content; }
            set {
                if (value == null || value.Length == 0) {
                    image.sprite = null;
                    DestroyContent ();
                    return;
                }
                if (!IsPngOrJpeg (value))
                    throw new ArgumentException ("Image data is not a PNG or JPEG file");
                var loaded = new Texture2D (1, 1);
                if (!loaded.LoadImage (value)) {
                    UnityEngine.Object.Destroy (loaded);
                    throw new ArgumentException ("Image data could not be read");
                }
                var loadedSprite = Sprite.Create (
                    loaded, new Rect (0, 0, loaded.width, loaded.height),
                    new Vector2 (0.5f, 0.5f));
                // The image is pointed at the new picture before the old one is freed, so
                // that it is never left drawing something that has been destroyed.
                image.sprite = loadedSprite;
                image.type = UnityEngine.UI.Image.Type.Simple;
                DestroyContent ();
                content = value;
                texture = loaded;
                sprite = loadedSprite;
            }
        }

        /// <summary>
        /// Destroy the image, along with the picture it is showing.
        /// </summary>
        public override void Destroy ()
        {
            base.Destroy ();
            DestroyContent ();
        }

        /// <summary>
        /// Whether the data starts with the signature of one of the two formats Unity
        /// reads. Its loader reports success for data that is neither, handing back a
        /// placeholder texture, so the format is checked here rather than relying on it.
        /// </summary>
        static bool IsPngOrJpeg (byte[] data)
        {
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var jpeg = new byte[] { 0xFF, 0xD8, 0xFF };
            return StartsWith (data, png) || StartsWith (data, jpeg);
        }

        static bool StartsWith (byte[] data, byte[] signature)
        {
            if (data.Length < signature.Length)
                return false;
            for (var i = 0; i < signature.Length; i++) {
                if (data [i] != signature [i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Free the sprite and texture holding the picture. Unity does not destroy them
        /// along with the game object that draws them, as they are assets in their own
        /// right, so they are destroyed by hand or they are left behind.
        /// </summary>
        void DestroyContent ()
        {
            content = new byte[0];
            if (sprite != null) {
                UnityEngine.Object.Destroy (sprite);
                sprite = null;
            }
            if (texture != null) {
                UnityEngine.Object.Destroy (texture);
                texture = null;
            }
        }

        /// <summary>
        /// The color the image is tinted with, or the color of the rectangle when no
        /// picture is set.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return image.color.ToTuple (); }
            set { image.color = value.ToColor (); }
        }
    }
}
