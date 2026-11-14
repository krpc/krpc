#pragma warning disable 1591

using UnityEngine;

namespace KRPC.UI
{
    public static class Skin
    {
        static GUISkin defaultSkin;
        static Texture2D windowBackground;

        public static GUISkin DefaultSkin {
            get {
                if (defaultSkin == null)
                    defaultSkin = GUI.skin;
                return defaultSkin;
            }
        }

        /// <summary>
        /// An opaque background for the windows, in place of the skin's translucent one.
        /// </summary>
        public static Texture2D WindowBackground {
            get {
                if (windowBackground == null) {
                    windowBackground = new Texture2D (1, 1);
                    windowBackground.SetPixel (0, 0, new Color (0.19f, 0.19f, 0.2f, 1f));
                    windowBackground.Apply ();
                }
                return windowBackground;
            }
        }
    }
}
