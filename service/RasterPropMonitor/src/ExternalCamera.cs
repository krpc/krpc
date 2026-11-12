using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.Utils;
using UnityEngine;
using CameraAPI = KRPC.RasterPropMonitor.API.Camera;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// A Raster Prop Monitor external camera: a part that the MFD camera pages show, such as
    /// the JSI External Radial-mount Camera. Obtained by calling
    /// <see cref="RasterPropMonitor.Cameras"/>.
    /// </summary>
    [KRPCClass (Service = "RasterPropMonitor", GameScene = GameScene.Flight)]
    public class ExternalCamera : Equatable<ExternalCamera>, IGameObjectState
    {
        // The field of view each camera renders at, by part. It is not saved with the game
        static readonly Dictionary<uint, float> fieldsOfView = new Dictionary<uint, float> ();

        readonly SpaceCenter.Services.Parts.Part part;

        internal ExternalCamera (SpaceCenter.Services.Parts.Part innerPart)
        {
            part = innerPart;
        }

        internal static PartModule Selector (global::Part part)
        {
            var modules = part.Modules;
            for (int i = 0; i < modules.Count; i++) {
                if (CameraAPI.SelectorType.IsInstanceOfType (modules [i]))
                    return modules [i];
            }
            return null;
        }

        internal static IList<ExternalCamera> Of (global::Vessel vessel)
        {
            return vessel.parts.Where (p => Selector (p) != null)
                .Select (p => new ExternalCamera (new SpaceCenter.Services.Parts.Part (p)))
                .ToList ();
        }

        /// <summary>
        /// Check that the cameras are the same.
        /// </summary>
        public override bool Equals (ExternalCamera other)
        {
            return !ReferenceEquals (other, null) && part == other.part;
        }

        /// <summary>
        /// Hash the camera.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The state of the camera. It takes the state of its part.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return part.GameObjectState; }
        }

        PartModule InternalSelector {
            get {
                var selector = Selector (part.InternalPart);
                if (selector == null)
                    throw new KRPC.Service.KRPC.ObjectDestroyedException ("The camera no longer exists, as its part no longer has one.");
                return selector;
            }
        }

        /// <summary>
        /// The part the camera is on.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part {
            get { return part; }
        }

        /// <summary>
        /// The camera's identifier, e.g. <c>ExtCam1</c>, which the MFD camera pages select it
        /// by. The player sets the number in the part's menu.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get {
                var selector = InternalSelector;
                return CameraAPI.Prefix (selector) + CameraAPI.Current (selector).ToString (System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The smallest field of view of the camera, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MinFoV {
            get { return CameraAPI.MinFoV (InternalSelector); }
        }

        /// <summary>
        /// The largest field of view of the camera, in degrees.
        /// </summary>
        [KRPCProperty]
        public float MaxFoV {
            get { return CameraAPI.MaxFoV (InternalSelector); }
        }

        /// <summary>
        /// The field of view that <see cref="Image"/> renders at, in degrees. It starts at
        /// <see cref="MaxFoV"/>, as on an MFD camera page, and a value set is clamped
        /// to the camera's range.
        /// </summary>
        [KRPCProperty]
        public float FoV {
            get {
                var selector = InternalSelector;
                float fieldOfView;
                if (!fieldsOfView.TryGetValue (part.InternalPart.flightID, out fieldOfView))
                    fieldOfView = CameraAPI.MaxFoV (selector);
                return Mathf.Clamp (fieldOfView, CameraAPI.MinFoV (selector), CameraAPI.MaxFoV (selector));
            }
            set {
                var selector = InternalSelector;
                fieldsOfView [part.InternalPart.flightID] =
                    Mathf.Clamp (value, CameraAPI.MinFoV (selector), CameraAPI.MaxFoV (selector));
            }
        }

        /// <summary>
        /// The camera's view as an image, rendered now. Streamed at a rate, it is a video of
        /// the camera, for which JPEG is smaller and faster.
        /// Throws an exception if the camera is not on the active vessel.
        /// </summary>
        /// <param name="width">The width of the image, in pixels.</param>
        /// <param name="height">The height of the image, in pixels.</param>
        /// <param name="format">The format of the image.</param>
        /// <param name="quality">The quality of a JPEG image, from 1 to 100.</param>
        /// <returns>The image data.</returns>
        [KRPCMethod]
        public byte[] Image (int width, int height, ImageFormat format = ImageFormat.PNG, int quality = 75)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException ("The image size is not positive");
            ImageEncoder.CheckQuality (format, quality);
            return Render (width, height, image => ImageEncoder.Encode (image, format, quality));
        }

        byte[] Render (int width, int height, Func<Texture2D, byte[]> encode)
        {
            var innerPart = part.InternalPart;
            if (innerPart.vessel != FlightGlobals.ActiveVessel)
                throw new InvalidOperationException ("The camera is not on the active vessel");
            var name = Name;
            var fieldOfView = FoV;
            // RPM's camera, as an MFD camera page uses it. It looks for the camera on its own
            // part first, then the rest of the vessel
            var camera = Activator.CreateInstance (CameraAPI.FlyingCameraType, innerPart, (float)width / height);
            var texture = RenderTexture.GetTemporary (width, height, 24);
            var image = new Texture2D (width, height, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            try {
                if (!CameraAPI.PointCamera (camera, name, fieldOfView) || !CameraAPI.Render (camera, texture, 0, 0))
                    throw new InvalidOperationException ("The camera did not render");
                // Render sets the cameras up to render at the end of the frame. We render
                // them now, from the farthest out
                foreach (var unityCamera in CameraAPI.Cameras (camera)) {
                    if (unityCamera != null && unityCamera.enabled) {
                        unityCamera.enabled = false;
                        unityCamera.Render ();
                    }
                }
                RenderTexture.active = texture;
                image.ReadPixels (new Rect (0, 0, width, height), 0, 0);
                image.Apply ();
                return encode (image);
            } finally {
                CameraAPI.Cleanup (camera);
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary (texture);
                UnityEngine.Object.Destroy (image);
            }
        }
    }
}
