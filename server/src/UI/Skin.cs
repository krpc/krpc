using UnityEngine;

namespace KRPC.UI
{
    static class Skin
    {
        static GUISkin defaultSkin;

        internal static GUISkin DefaultSkin {
            get {
                if (defaultSkin == null)
                    defaultSkin = GUI.skin;
                return defaultSkin;
            }
        }
    }
}
