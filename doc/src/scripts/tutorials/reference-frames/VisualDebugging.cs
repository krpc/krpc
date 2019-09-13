using System;
using KRPC.Client;
using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.SpaceCenter;

class VisualDebugging
{
    public static void Main ()
    {
        var conn = new Connection ("Visual Debugging");
        var vessel = conn.SpaceCenter ().ActiveVessel;

        var refFrame = vessel.SurfaceVelocityReferenceFrame;
        conn.Drawing ().AddDirectionFromCom(
            new Tuple<double, double, double>(0, 1, 0), refFrame);
        while (true) {
        }
    }
}
