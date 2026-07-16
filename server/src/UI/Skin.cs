#pragma warning disable 1591

using UnityEngine;

namespace KRPC.UI
{
    public static class Skin
    {
        static GUISkin defaultSkin;

        public static GUISkin DefaultSkin {
            get {
                if (defaultSkin == null)
                    defaultSkin = GUI.skin;
                return defaultSkin;
            }
        }
    }
}
