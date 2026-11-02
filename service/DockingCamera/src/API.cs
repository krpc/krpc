using System;
using System.Collections.Generic;
using System.Linq;
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

        // PartCameraModule._camera, a PartCamera
        static Func<PartModule, object> camera;
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
            var moduleType = assembly.assembly.GetType("OLDD_camera.Modules.PartCameraModule");
            var cameraType = assembly.assembly.GetType("OLDD_camera.Camera.BaseCamera");
            if (moduleType != null)
                camera = Accessor.Getter<PartModule, object>(moduleType, "_camera");
            if (cameraType != null)
                allCameras = Accessor.Getter<object, List<UnityEngine.Camera>>(cameraType, "AllCameras");
            if (camera == null || allCameras == null) {
                Logger.WriteLine("Load API: " + assemblyName + " camera members not found; skipping", Logger.Severity.Error);
                return;
            }
            IsAvailable = true;
            Logger.WriteLine("Load API: Successfully loaded " + assemblyName + " version " + assembly.assembly.GetName().Version);
        }

        // Renders the cameras into a texture and encodes it as a PNG, as BaseCamera.GetPic does
        public static byte[] GetImage(PartModule module, int width, int height)
        {
            var partCamera = camera(module);
            var cameras = partCamera == null ? null : allCameras(partCamera);
            if (cameras == null || cameras.Count == 0)
                throw new InvalidOperationException("The camera is off or not started");
            var renderTexture = RenderTexture.GetTemporary(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            var targets = cameras.Select(c => c.targetTexture).ToList();
            try {
                foreach (var c in cameras) {
                    c.targetTexture = renderTexture;
                    c.Render();
                }
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                return image.EncodeToPNG();
            } finally {
                // Restore the mod's own render targets
                for (int i = 0; i < cameras.Count; i++)
                    cameras[i].targetTexture = targets[i];
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
