using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.LiDAR
{
    sealed class API : ModAPI
    {
        public API() : base(ServiceInfo.Mods.LiDAR.Name, ServiceInfo.Mods.LiDAR.Assembly)
        {
        }

        protected override void Bind(ModBinder mod)
        {
            mod.Statics(typeof(API), "LiDAR.API");
        }

        public static Func<Part, IList<double>> GetCloud { get; internal set; }
    }
}
