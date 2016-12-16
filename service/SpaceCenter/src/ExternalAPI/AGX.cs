using System;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    static class AGX
    {
        public static void Load ()
        {
            IsAvailable = APILoader.Load (typeof(AGX), "AGExt", "ActionGroupsExtended.AGExtExternal");
        }

        public static bool IsAvailable { get; private set; }

        public static Func<uint, int, bool> AGX2VslGroupState { get; internal set; }

        public static Func<uint, int, bool> AGX2VslToggleGroup { get; internal set; }

        public static Func<uint, int, bool, bool> AGX2VslActivateGroup { get; internal set; }
    }
}
