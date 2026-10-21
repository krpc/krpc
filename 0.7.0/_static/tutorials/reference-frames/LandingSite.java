import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.Drawing;
import krpc.client.services.SpaceCenter;

import java.io.IOException;

public class LandingSite {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException {
        Connection connection = Connection.newInstance("Landing Site");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Drawing drawing = Drawing.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();
        SpaceCenter.CelestialBody body = vessel.getOrbit().getBody();

        // Define the landing site as the top of the VAB
        double landingLatitude = -(0.0+(5.0/60.0)+(48.38/60.0/60.0));
        double landingLongitude = -(74.0+(37.0/60.0)+(12.2/60.0/60.0));
        double landingAltitude = 111;

        // Determine landing site reference frame
        // (orientation: x=zenith, y=north, z=east)
        SpaceCenter.ReferenceFrame landingReferenceFrame =
          body.surfaceReferenceFrame(landingLatitude, landingLongitude, landingAltitude);

        // Draw axes
        drawing.addAxes(landingReferenceFrame, 10, true);

        while (true)
          Thread.sleep(1000);
    }
}
