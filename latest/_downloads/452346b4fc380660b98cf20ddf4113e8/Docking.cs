using System;
using KRPC.Client;
using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.StdLib;

class Docking
{
    public static void Main ()
    {
        var conn = new Connection ("Docking");
        var spaceCenter = conn.SpaceCenter ();
        var drawing = conn.Drawing ();
        var vessel = spaceCenter.ActiveVessel;
        var port = vessel.Parts.Controlling.DockingPort;
        var target = spaceCenter.TargetDockingPort;
        var frame = target.ReferenceFrame;

        // Turn to face the target port
        Console.WriteLine ("Turning to face the target");
        vessel.Control.SAS = false;
        vessel.Control.RCS = true;
        vessel.AutoPilot.ReferenceFrame = frame;
        vessel.AutoPilot.TargetDirection = Tuple.Create (0.0, -1.0, 0.0);
        vessel.AutoPilot.Engaged = true;
        vessel.AutoPilot.Wait ();

        // Draw the docking axis, and a line from our port to the target
        var origin = Tuple.Create (0.0, 0.0, 0.0);
        var axis = drawing.AddLine (
            origin, Tuple.Create (0.0, 50.0, 0.0), frame);
        axis.Color = new ColorRGB (0, 1, 0);
        var approach = drawing.AddLine (origin, origin, frame);
        approach.Color = new ColorRGB (1, 1, 0);

        // Approach the target port along its axis
        Console.WriteLine ("Approaching");
        var position = conn.AddStream (() => port.Position (frame));
        var velocity = conn.AddStream (() => vessel.Velocity (frame));
        while (port.State != DockingPortState.Docking &&
               port.State != DockingPortState.Docked) {
            var p = position.Get ();
            approach.Start = p;

            // Line up on the axis, then close in along it
            double offAxis =
                Math.Sqrt (p.Item1 * p.Item1 + p.Item3 * p.Item3);
            double goalY = offAxis < 0.5 ? 0 : Math.Max (p.Item2, 5);
            double ex = -p.Item1, ey = goalY - p.Item2, ez = -p.Item3;
            double distance = Math.Sqrt (ex * ex + ey * ey + ez * ez);
            double speed = Math.Min (1, Math.Max (0.1, 0.2 * distance));
            double scale = speed / Math.Max (distance, 0.01);

            // Thrust towards the velocity we want, in the vessel's frame
            var v = velocity.Get ();
            var thrust = Tuple.Create (
                2 * (ex * scale - v.Item1),
                2 * (ey * scale - v.Item2),
                2 * (ez * scale - v.Item3));
            var t = spaceCenter.TransformDirection (
                thrust, frame, vessel.ReferenceFrame);
            vessel.Control.Right =
                (float)Math.Max (-1, Math.Min (1, t.Item1));
            vessel.Control.Forward =
                (float)Math.Max (-1, Math.Min (1, t.Item2));
            vessel.Control.Up =
                (float)Math.Max (-1, Math.Min (1, -t.Item3));
            System.Threading.Thread.Sleep (50);
        }

        // Stop the thrusters, and remove the drawing
        vessel.Control.Right = 0;
        vessel.Control.Forward = 0;
        vessel.Control.Up = 0;
        vessel.AutoPilot.Engaged = false;
        drawing.Clear ();
        Console.WriteLine ("Docked");
        conn.Dispose ();
    }
}
