using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.LiDAR
{
    /// <summary>
    /// Provides functionality to interact with the LiDAR mod.
    /// </summary>
    [Obsolete ("Use <see cref=\"M:KRPC.SpaceCenter.Services.SpaceCenter.RaycastDistance\"/> instead.")]
    [KRPCService(Id = ServiceInfo.Id, GameScene = GameScene.All)]
    [RequiresMod(typeof(API))]
    public static class LIDAR
    {
        /// <summary>
        /// Whether the LiDAR mod is installed.
        /// </summary>
        [KRPCProperty]
        public static bool Available
        {
            get { return ModAPI.Get<API>().IsAvailable; }
        }

        /// <summary>
        /// Get the LiDAR on a part.
        /// </summary>
        [KRPCProcedure]
        public static Laser Laser(SpaceCenter.Services.Parts.Part part)
        {
            return new Laser(part);
        }
    }
}
