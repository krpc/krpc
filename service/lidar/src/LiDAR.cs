using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.LiDAR
{
    /// <summary>
    /// LaserDist service.
    /// </summary>
    [KRPCService(Id = 10, GameScene = GameScene.All)]
    public static class LiDAR
    {
        static void CheckAPI()
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException("LiDAR is not available");
        }

        /// <summary>
        /// Check if the LaserDist API is available.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return API.IsAvailable; }
        }

        /// <summary>
        /// Get a LaserDist part.
        /// </summary>
        [KRPCProcedure]
        public static Laser Laser(SpaceCenter.Services.Parts.Part part)
        {
            CheckAPI();
            return new Laser(part);
        }
    }
}
