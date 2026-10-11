using System;
using KRPC.Client;
using KRPC.Client.Services.Drawing;
using KRPC.Client.Services.SpaceCenter;

class DrawingExample
{
    public static void Main ()
    {
        var conn = new Connection ("Drawing Example");
        var spaceCenter = conn.SpaceCenter ();
        var drawing = conn.Drawing ();
        var vessel = spaceCenter.ActiveVessel;
        var kerbin = spaceCenter.Bodies ["Kerbin"];

        // Draw a circular orbit 250 km above Kerbin, as a dashed green path
        // with a label
        var orbit = Orbit.CreateFromOrbitalElements (
            conn, kerbin, kerbin.EquatorialRadius + 250000, 0, 0, 0, 0, 0,
            spaceCenter.UT);
        var path = drawing.AddOrbit (orbit);
        path.Color = Tuple.Create (0.0, 1.0, 0.0);
        path.Dashed = true;
        path.Label = "Target orbit";

        // Mark the north pole in the map view, with the game's flag icon
        var site = kerbin.SurfacePosition (90, 0, kerbin.ReferenceFrame);
        drawing.AddMapMarker (
            site, kerbin.ReferenceFrame, MapMarkerIcon.Flag, "North pole");

        // Draw a yellow arrow from the vessel, pointing prograde
        var arrow = drawing.AddDirectionFromCom (
            Tuple.Create (0.0, 1.0, 0.0), vessel.OrbitalReferenceFrame);
        arrow.Color = Tuple.Create (1.0, 1.0, 0.0);
        arrow.EndArrow = true;

        // Label the arrow with the orbital speed of the vessel
        while (true) {
            arrow.Label = (int)vessel.Orbit.Speed + " m/s";
            System.Threading.Thread.Sleep (1000);
        }
    }
}
