using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.Drawing;

class LandingSite
{
    public static void Main ()
    {
        var conn = new Connection ("Landing Site");
        var vessel = conn.SpaceCenter ().ActiveVessel;
        var body = vessel.Orbit.Body;

        // Define the landing site as the top of the VAB
        double landingLatitude = -(0.0+(5.0/60.0)+(48.38/60.0/60.0));
        double landingLongitude = -(74.0+(37.0/60.0)+(12.2/60.0/60.0));
        double landingAltitude = 111;

        // Determine landing site reference frame
        // (orientation: x=zenith, y=north, z=east)
        var landingReferenceFrame = body.SurfaceReferenceFrame(
            landingLatitude, landingLongitude, landingAltitude);

        // Draw axes
        conn.Drawing().AddAxes(landingReferenceFrame);

        while (true)
          System.Threading.Thread.Sleep (1000);
    }
}
