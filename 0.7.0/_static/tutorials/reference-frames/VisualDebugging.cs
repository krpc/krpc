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

        // Draw the axes of the surface reference frame
        conn.Drawing ().AddAxes (vessel.SurfaceReferenceFrame);

        // Draw the direction of the surface velocity, as a labeled arrow
        var refFrame = vessel.SurfaceVelocityReferenceFrame;
        var line = conn.Drawing ().AddDirectionFromCom(
            new Tuple<double, double, double>(0, 1, 0), refFrame);
        line.EndArrow = true;
        line.Label = "Surface velocity";
        while (true) {
        }
    }
}
