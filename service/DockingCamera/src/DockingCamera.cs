using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// This service provides functionality to interact with
    /// <a href="https://spacedock.info/mod/1542">Docking Camera (KURS)</a>.
    /// </summary>
    [KRPCService(Id = 11, GameScene = GameScene.All)]
    public static class DockingCamera
    {
        static void CheckAPI()
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException("Docking Camera (KURS) is not available");
        }

        /// <summary>
        /// Whether Docking Camera (KURS) is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return API.IsAvailable; }
        }

        /// <summary>
        /// Get the camera on a part.
        /// Throws an exception if the part is not an onboard camera.
        /// </summary>
        /// <param name="part">The part.</param>
        [KRPCProcedure]
        public static Camera Camera(SpaceCenter.Services.Parts.Part part)
        {
            CheckAPI();
            return new Camera(part);
        }
    }
}
