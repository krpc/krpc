using System;
using UnityEngine;

namespace KRPC.UI
{
    sealed class Icons
    {
        /// <summary>
        /// Path to directory in GameData when icons are stored
        /// </summary>
        const string iconsPath = "kRPC/icons";

        static Icons instance;

        public static Icons Instance {
            get {
                if (instance == null) {
                    instance = new Icons ();
                }
                return instance;
            }
        }

        public Texture2D ButtonDisconnectClient { get; private set; }

        public Texture2D ButtonCloseWindow { get; private set; }

        Icons ()
        {
            ButtonDisconnectClient = LoadTexture ("button-disconnect-client.png");
            ButtonCloseWindow = LoadTexture ("button-close-window.png");
        }

        /// <summary>
        /// Load a file as a 2D texture.
        /// </summary>
        static Texture2D LoadTexture (string filepath)
        {
            if (!filepath.EndsWith (".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException ("Not a PNG file", nameof (filepath));
            filepath = iconsPath + "/" + filepath.Substring (0, filepath.Length - 4);
            Utils.Logger.WriteLine ("Loading texture " + filepath);
            return GameDatabase.Instance.GetTexture (filepath, false);
        }
    }
}
