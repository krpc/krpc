using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.LiDAR
{
    /// <summary>
    /// LaserDist Service
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    [KRPCService(Id = 10, GameScene = GameScene.All)]
    public static class LiDAR
    {
        static void CheckAPI()
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException("LiDAR is not available");
        }

        /// <summary>
        /// Check if the LaserDist API is avaiable
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return API.IsAvailable; }
        }

        /// <summary>
        /// Get a LaserDist part
        /// </summary>
        [KRPCProcedure]
        public static Laser Laser(SpaceCenter.Services.Parts.Part part)
        {
            CheckAPI();
            return new Laser(part);
        }
    }
}
