import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.Drawing;
import krpc.client.services.Drawing.Line;
import krpc.client.services.Drawing.MapMarkerIcon;
import krpc.client.services.Drawing.OrbitPath;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.Orbit;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.client.services.StdLib;

import org.javatuples.Triplet;

import java.io.IOException;

public class DrawingExample {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException {
        Connection connection = Connection.newInstance("Drawing Example");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Drawing drawing = Drawing.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        CelestialBody kerbin = spaceCenter.getBodies().get("Kerbin");

        // Draw a circular orbit 250 km above Kerbin, as a dashed green path
        // with a label
        Orbit orbit = Orbit.createFromOrbitalElements(
          connection, kerbin, kerbin.getEquatorialRadius() + 250000, 0, 0, 0, 0, 0,
          spaceCenter.getUT());
        OrbitPath path = drawing.addOrbit(orbit, true);
        path.setColor(new StdLib.ColorRGB(0f, 1f, 0f));
        path.setDashed(true);
        path.setLabel("Target orbit");

        // Mark the north pole in the map view, with the game's flag icon
        Triplet<Double, Double, Double> site =
          kerbin.surfacePosition(90, 0, kerbin.getReferenceFrame());
        drawing.addMapMarker(
          site, kerbin.getReferenceFrame(), MapMarkerIcon.FLAG, "North pole", true);

        // Draw a yellow arrow from the vessel, pointing prograde
        Line arrow = drawing.addDirectionFromCom(
          new Triplet<Double, Double, Double>(0.0, 1.0, 0.0),
          vessel.getOrbitalReferenceFrame(), 10, true);
        arrow.setColor(new StdLib.ColorRGB(1f, 1f, 0f));
        arrow.setEndArrow(true);

        // Label the arrow with the orbital speed of the vessel
        while (true) {
            arrow.setLabel(String.format("%.0f m/s", vessel.getOrbit().getSpeed()));
            Thread.sleep(1000);
        }
    }
}
