using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.StdLib;
using KRPC.Utils;
using UnityEngine;
using Logger = KRPC.Utils.Logger;

namespace KRPC.DockingCamera
{
    // Reads the cameras of Docking Camera (KURS) by reflection. The mod has no public API, so
    // we bind to the private members of its 1.3.9 release.
    static class API
    {
        const string assemblyName = "DockingCamKURS";

        // The camera modules, in order of preference for a part with both. MM_KSPCamera.cfg adds
        // DockingCameraModule to each docking port.
        static readonly string[] moduleNames = { "PartCameraModule", "DockingCameraModule" };
        // The _camera field of each module, a subclass of BaseCamera
        static readonly Dictionary<string, Func<PartModule, object>> cameras =
            new Dictionary<string, Func<PartModule, object>>();
        // BaseCamera.AllCameras, the Unity cameras for each render layer. Empty while the camera is off.
        static Func<object, List<UnityEngine.Camera>> allCameras;

        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = false;
            var assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == assemblyName);
            if (assembly == null) {
                Logger.WriteLine("Load API: " + assemblyName + " not found; skipping");
                return;
            }
            cameras.Clear();
            foreach (var name in moduleNames) {
                var moduleType = assembly.assembly.GetType("OLDD_camera.Modules." + name);
                var getter = moduleType == null ? null : Accessor.Getter<PartModule, object>(moduleType, "_camera");
                if (getter != null)
                    cameras[name] = getter;
            }
            var cameraType = assembly.assembly.GetType("OLDD_camera.Camera.BaseCamera");
            if (cameraType != null)
                allCameras = Accessor.Getter<object, List<UnityEngine.Camera>>(cameraType, "AllCameras");
            if (cameras.Count != moduleNames.Length || allCameras == null) {
                Logger.WriteLine("Load API: " + assemblyName + " camera members not found; skipping", Logger.Severity.Error);
                return;
            }
            IsAvailable = true;
            Logger.WriteLine("Load API: Successfully loaded " + assemblyName + " version " + assembly.assembly.GetName().Version);
        }

        // The camera module on a part, or null if it has none
        public static PartModule GetModule(global::Part part)
        {
            var name = moduleNames.FirstOrDefault(n => part.Modules.Contains(n));
            return name == null ? null : part.Modules[name];
        }

        // Renders the cameras into a texture as BaseCamera.GetPic does, and encodes it
        public static byte[] GetImage(PartModule module, int width, int height, ImageFormat format, int quality)
        {
            var camera = cameras[module.moduleName](module);
            var unityCameras = camera == null ? null : allCameras(camera);
            if (unityCameras == null || unityCameras.Count == 0)
                throw new InvalidOperationException("The camera is off or not started");
            var renderTexture = RenderTexture.GetTemporary(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            var targets = unityCameras.Select(c => c.targetTexture).ToList();
            try {
                foreach (var c in unityCameras) {
                    c.targetTexture = renderTexture;
                    c.Render();
                }
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                return ImageEncoder.Encode(image, format, quality);
            } finally {
                // Restore the mod's own render targets
                for (int i = 0; i < unityCameras.Count; i++)
                    unityCameras[i].targetTexture = targets[i];
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
