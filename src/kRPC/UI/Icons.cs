using System;
using UnityEngine;
using KRPC.Utils;

namespace KRPC.UI
{
    class Icons
    {
        /// <summary>
        /// Path to directory in GameData when icons are stored
        /// </summary>
        const string iconsPath = "kRPC";
        public Texture2D buttonDisconnectClient;
        static Icons instance;

        public static Icons Instance {
            get {
                if (instance == null) {
                    instance = new Icons ();
                }
                return instance;
            }
        }

        Icons ()
        {
            buttonDisconnectClient = LoadTexture ("button-disconnect-client.png");
        }

        /// <summary>
        /// Load a file as a 2D texture.
        /// </summary>
        Texture2D LoadTexture (string filepath)
        {
            if (!filepath.EndsWith (".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException ();
            filepath = iconsPath + "/" + filepath.Substring (0, filepath.Length - 4);
            Logger.WriteLine ("Loading texture " + filepath);
            return GameDatabase.Instance.GetTexture (filepath, false);
        }
    }
}

