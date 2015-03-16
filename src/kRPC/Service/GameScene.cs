using System;

namespace KRPC.Service
{
    [Flags]
    public enum GameScene
    {
        None = 0,
        SpaceCenter = 1 << 0,
        Flight = 1 << 1,
        TrackingStation = 1 << 2,
        EditorVAB = 1 << 3,
        EditorSPH = 1 << 4,
        All = ~0,
        Editor = EditorSPH | EditorVAB
    }
}
