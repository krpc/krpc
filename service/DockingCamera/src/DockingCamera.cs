using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// Provides functionality to interact with Docking Camera (KURS).
    /// </summary>
    [KRPCService(Id = ServiceInfo.Id, GameScene = GameScene.All)]
    public static class DockingCamera
    {
        static void CheckAPI()
        {
            ModAPI.Get<API>().CheckAvailable();
        }

        /// <summary>
        /// Whether Docking Camera (KURS) is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return ModAPI.Get<API>().IsAvailable; }
        }

        /// <summary>
        /// Get the camera on a part.
        /// Throws an exception if the part is not an onboard camera or a docking port.
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
