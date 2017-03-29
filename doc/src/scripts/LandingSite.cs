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
        var landingPosition = body.SurfacePosition(
            landingLatitude, landingLongitude, body.ReferenceFrame);
        var qLong = Tuple.Create(
          0.0,
          Math.Sin(-landingLongitude * 0.5 * Math.PI / 180.0),
          0.0,
          Math.Cos(-landingLongitude * 0.5 * Math.PI / 180.0)
        );
        var qLat = Tuple.Create(
          0.0,
          0.0,
          Math.Sin(landingLatitude * 0.5 * Math.PI / 180.0),
          Math.Cos(landingLatitude * 0.5 * Math.PI / 180.0)
        );
        var landingReferenceFrame =
          ReferenceFrame.CreateRelative(
            conn,
            ReferenceFrame.CreateRelative(
              conn,
              ReferenceFrame.CreateRelative(
                conn,
                body.ReferenceFrame,
                landingPosition,
                qLong),
              Tuple.Create(0.0, 0.0, 0.0),
              qLat),
            Tuple.Create(landingAltitude, 0.0, 0.0));

        // Draw axes
        var zero = Tuple.Create(0.0, 0.0, 0.0);
        conn.Drawing().AddLine(
            zero, Tuple.Create(1.0, 0.0, 0.0), landingReferenceFrame);
        conn.Drawing().AddLine(
            zero, Tuple.Create(0.0, 1.0, 0.0), landingReferenceFrame);
        conn.Drawing().AddLine(
            zero, Tuple.Create(0.0, 0.0, 1.0), landingReferenceFrame);

        while (true)
          System.Threading.Thread.Sleep (1000);
    }
}
