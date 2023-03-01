using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.LiDAR
{
    static class API
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(API), "LiDAR", "LiDAR.API") != null);
        }

        public static Func<Part, IList<double>> GetCloud { get; internal set; }
    }
}
