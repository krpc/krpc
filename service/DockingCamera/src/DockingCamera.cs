using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.DockingCamera
{
    /// <summary>
    /// Camera service.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    [KRPCService(Id = 11, GameScene = GameScene.All)]
    public static class DockingCamera
    {
        static void CheckAPI()
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException("Camera is not available");
        }

        /// <summary>
        /// Check if the Camera API is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return API.IsAvailable; }
        }

        /// <summary>
        /// Get a Camera part.
        /// </summary>
        [KRPCProcedure]
        public static Camera Camera(SpaceCenter.Services.Parts.Part part)
        {
            CheckAPI();
            return new Camera(part);
        }
    }
}
