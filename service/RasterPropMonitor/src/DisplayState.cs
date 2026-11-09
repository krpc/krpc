using System;
using System.Collections.Generic;
using UnityEngine;

namespace KRPC.RasterPropMonitor
{
    // The content of a display, kept in the service so that it outlives the handler modules.
    // KSP builds the IVA's modules again on IVA entry, and a new handler takes the content.
    sealed class DisplayState
    {
        static readonly Dictionary<Tuple<uint, int, int>, DisplayState> monitors =
            new Dictionary<Tuple<uint, int, int>, DisplayState> ();
        static readonly Dictionary<Tuple<Guid, string>, DisplayState> named =
            new Dictionary<Tuple<Guid, string>, DisplayState> ();

        readonly HashSet<KRPCDisplay> shownBy = new HashSet<KRPCDisplay> ();
        readonly Dictionary<int, int> presses = new Dictionary<int, int> ();
        readonly Dictionary<int, int> releases = new Dictionary<int, int> ();
        byte [] image;
        Texture2D texture;

        DisplayState (string name)
        {
            Name = name;
            Text = string.Empty;
        }

        /// <summary>
        /// The display of a monitor, by its part, its prop and its place among the prop's
        /// monitor modules.
        /// </summary>
        public static DisplayState Of (uint flightID, int propID, int index)
        {
            var key = Tuple.Create (flightID, propID, index);
            DisplayState state;
            if (!monitors.TryGetValue (key, out state)) {
                state = new DisplayState (null);
                monitors [key] = state;
            }
            return state;
        }

        /// <summary>
        /// A named display of a vessel.
        /// </summary>
        public static DisplayState Of (Guid vesselId, string name)
        {
            var key = Tuple.Create (vesselId, name);
            DisplayState state;
            if (!named.TryGetValue (key, out state)) {
                state = new DisplayState (name);
                named [key] = state;
            }
            return state;
        }

        public string Name { get; private set; }

        public string Text { get; set; }

        public byte [] Image {
            get { return image; }
            set {
                Texture2D decoded = null;
                if (value != null) {
                    // Unity decodes data that is not an image as a placeholder texture
                    if (!HasSignature (value, pngSignature) && !HasSignature (value, jpegSignature))
                        throw new ArgumentException ("The image is not a PNG or JPEG image", nameof (value));
                    decoded = new Texture2D (2, 2);
                    if (!decoded.LoadImage (value)) {
                        UnityEngine.Object.Destroy (decoded);
                        throw new ArgumentException ("The image is not a PNG or JPEG image", nameof (value));
                    }
                }
                if (texture != null)
                    UnityEngine.Object.Destroy (texture);
                image = value;
                texture = decoded;
            }
        }

        static readonly byte [] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        static readonly byte [] jpegSignature = { 0xFF, 0xD8, 0xFF };

        static bool HasSignature (byte [] data, byte [] signature)
        {
            if (data.Length < signature.Length)
                return false;
            for (int i = 0; i < signature.Length; i++) {
                if (data [i] != signature [i])
                    return false;
            }
            return true;
        }

        public Texture2D Texture {
            get { return texture; }
        }

        public bool Shown {
            get {
                // A destroyed module compares equal to null
                shownBy.RemoveWhere (handler => handler == null);
                return shownBy.Count > 0;
            }
        }

        public void SetShown (KRPCDisplay handler, bool shown)
        {
            if (shown)
                shownBy.Add (handler);
            else
                shownBy.Remove (handler);
        }

        // The number of presses and releases of each button, which the events compare
        public int Presses (int id)
        {
            int count;
            return presses.TryGetValue (id, out count) ? count : 0;
        }

        public int Releases (int id)
        {
            int count;
            return releases.TryGetValue (id, out count) ? count : 0;
        }

        public void Press (int id)
        {
            presses [id] = Presses (id) + 1;
        }

        public void Release (int id)
        {
            releases [id] = Releases (id) + 1;
        }
    }
}
