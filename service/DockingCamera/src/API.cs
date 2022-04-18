using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.DockingCamera
{
    static class API
    {
        public static bool IsAvailable { get; private set; }

        public static void Load()
        {
            IsAvailable = (APILoader.Load(typeof(API), "DockingCamKURS", "OLDD_camera.API") != null);
        }

        public static Func<Part, byte[]>  GetImage { get; internal set; }
    }
}