using System;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple4 = System.Tuple<double, double, double, double>;

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

        /// <summary>
        /// Whether the picture being shown was drawn from raw pixels, in which case the
        /// texture is the size of the picture, holds one byte per channel and can be
        /// redrawn in place.
        /// </summary>
        bool rawPixels;

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
        /// an empty array if no picture has been set. A picture drawn with
        /// <see cref="SetPixels" /> is not returned here either.
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
        /// Draw the picture from raw pixels rather than a file, so that a client can
        /// draw whatever it likes, a graph for instance, and redraw it as often as it
        /// likes.
        /// </summary>
        /// <param name="data">
        /// The pixels, 4 bytes per pixel: red, green, blue and alpha, each 0 to 255.
        /// Pixels run left to right and rows run top to bottom, the way image files
        /// store them, so the array must hold width times height times 4 bytes.
        /// </param>
        /// <param name="width">How many pixels wide the picture is.</param>
        /// <param name="height">How many pixels tall the picture is.</param>
        /// <remarks>
        /// Redrawing at an unchanged size draws into the picture already there, so a
        /// picture redrawn every update does not build a new one each time.
        /// </remarks>
        [KRPCMethod]
        public void SetPixels (byte[] data, int width, int height)
        {
            if (data == null)
                throw new ArgumentNullException (nameof (data));
            if (width <= 0 || height <= 0)
                throw new ArgumentException ("The size of the picture must be positive");
            if (data.LongLength != (long)width * height * 4)
                throw new ArgumentException (
                    "The picture needs width * height * 4 bytes, 4 bytes per pixel");
            // Rows arrive top to bottom, the way image files store them, and Unity
            // stores a texture bottom row first, so the rows are turned over.
            var flipped = FlipRows (data, width, height);
            if (rawPixels && texture != null &&
                texture.width == width && texture.height == height) {
                texture.LoadRawTextureData (flipped);
                texture.Apply ();
                return;
            }
            var loaded = new Texture2D (width, height, TextureFormat.RGBA32, false);
            loaded.LoadRawTextureData (flipped);
            loaded.Apply ();
            var loadedSprite = Sprite.Create (
                loaded, new Rect (0, 0, width, height), new Vector2 (0.5f, 0.5f));
            // The image is pointed at the new picture before the old one is freed, so
            // that it is never left drawing something that has been destroyed.
            image.sprite = loadedSprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
            DestroyContent ();
            texture = loaded;
            sprite = loadedSprite;
            rawPixels = true;
        }

        /// <summary>
        /// Redraw part of a picture drawn with <see cref="SetPixels" />: a block of the
        /// given size, with its top left corner x pixels in from the left edge and y
        /// pixels down from the top. The rest of the picture is left as it is, so a
        /// change to a small part of a large picture, one new column of a scrolling
        /// graph for instance, only sends the part that changed.
        /// </summary>
        /// <param name="data">
        /// The pixels of the block, laid out as for <see cref="SetPixels" />: 4 bytes
        /// per pixel, rows top to bottom, width times height times 4 bytes in all.
        /// </param>
        /// <param name="x">How many pixels the block sits in from the left edge.</param>
        /// <param name="y">How many pixels the block sits down from the top.</param>
        /// <param name="width">How many pixels wide the block is.</param>
        /// <param name="height">How many pixels tall the block is.</param>
        /// <remarks>
        /// The image must already be showing a picture drawn from raw pixels, which is
        /// what sets the size, and the block must fit inside it. A picture loaded from
        /// a file cannot be redrawn this way: the file contents a client reads back
        /// would no longer be what is on the screen.
        /// </remarks>
        [KRPCMethod]
        public void UpdatePixels (byte[] data, int x, int y, int width, int height)
        {
            if (data == null)
                throw new ArgumentNullException (nameof (data));
            if (width <= 0 || height <= 0)
                throw new ArgumentException ("The size of the block must be positive");
            if (data.LongLength != (long)width * height * 4)
                throw new ArgumentException (
                    "The block needs width * height * 4 bytes, 4 bytes per pixel");
            if (!rawPixels || texture == null)
                throw new InvalidOperationException (
                    "The image is not showing a picture drawn from raw pixels");
            if (x < 0 || y < 0 ||
                (long)x + width > texture.width || (long)y + height > texture.height)
                throw new ArgumentException ("The block does not fit inside the picture");
            // The block arrives top row first while Unity takes rows bottom first, so
            // the rows are turned over as they are converted, and the y offset is
            // measured down from the top while Unity measures up from the bottom.
            var pixels = new Color32[width * height];
            for (var row = 0; row < height; row++) {
                var source = row * width * 4;
                var target = (height - 1 - row) * width;
                for (var column = 0; column < width; column++) {
                    var i = source + column * 4;
                    pixels [target + column] = new Color32 (
                        data [i], data [i + 1], data [i + 2], data [i + 3]);
                }
            }
            texture.SetPixels32 (x, texture.height - y - height, width, height, pixels);
            texture.Apply ();
        }

        /// <summary>
        /// Turn the rows of a picture over, swapping the top row with the bottom one.
        /// </summary>
        static byte[] FlipRows (byte[] data, int width, int height)
        {
            var flipped = new byte[data.Length];
            var stride = width * 4;
            for (var row = 0; row < height; row++)
                Array.Copy (data, row * stride, flipped, (height - 1 - row) * stride, stride);
            return flipped;
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
            rawPixels = false;
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
        /// picture is set, as (red, green, blue, alpha). An alpha of 0 is fully
        /// transparent and 1 is fully opaque.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Color {
            get { return image.color.ToRgbaTuple (); }
            set { image.color = value.ToColor (); }
        }
    }
}
