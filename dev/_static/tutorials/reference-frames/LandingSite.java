import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.Drawing;
import krpc.client.services.SpaceCenter;

import org.javatuples.Triplet;
import org.javatuples.Quartet;

import java.io.IOException;
import java.lang.Math;

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
        Triplet<Double, Double, Double> landingPosition = body.surfacePosition(
          landingLatitude, landingLongitude, body.getReferenceFrame());
        Quartet<Double, Double, Double, Double> qLong =
          new Quartet<Double, Double, Double, Double>(
            0.0,
            Math.sin(-landingLongitude * 0.5 * Math.PI / 180.0),
            0.0,
            Math.cos(-landingLongitude * 0.5 * Math.PI / 180.0));
        Quartet<Double, Double, Double, Double> qLat =
          new Quartet<Double, Double, Double, Double>(
            0.0,
            0.0,
            Math.sin(landingLatitude * 0.5 * Math.PI / 180.0),
            Math.cos(landingLatitude * 0.5 * Math.PI / 180.0));
        Quartet<Double, Double, Double, Double> qIdentity =
          new Quartet<Double, Double, Double, Double>(0.0, 0.0, 0.0, 1.0);
        Triplet<Double, Double, Double> zero =
          new Triplet<Double, Double, Double>(0.0, 0.0, 0.0);
        SpaceCenter.ReferenceFrame landingReferenceFrame =
          SpaceCenter.ReferenceFrame.createRelative(
            connection,
            SpaceCenter.ReferenceFrame.createRelative(
              connection,
              SpaceCenter.ReferenceFrame.createRelative(
                connection,
                body.getReferenceFrame(),
                landingPosition, qLong, zero, zero),
              zero, qLat, zero, zero),
            new Triplet<Double, Double, Double>(landingAltitude, 0.0, 0.0),
            qIdentity, zero, zero);

        // Draw axes
        drawing.addLine(
          zero, new Triplet<Double, Double, Double>(1.0, 0.0, 0.0),
          landingReferenceFrame, true);
        drawing.addLine(
          zero, new Triplet<Double, Double, Double>(0.0, 1.0, 0.0),
          landingReferenceFrame, true);
        drawing.addLine(
          zero, new Triplet<Double, Double, Double>(0.0, 0.0, 1.0),
          landingReferenceFrame, true);

        while (true)
          Thread.sleep(1000);
    }
}
