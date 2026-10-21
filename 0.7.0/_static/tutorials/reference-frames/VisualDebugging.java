import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.Drawing;

import org.javatuples.Triplet;

import java.io.IOException;

public class VisualDebugging {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Visual Debugging");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Drawing drawing = Drawing.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

        // Draw the axes of the surface reference frame
        drawing.addAxes(vessel.getSurfaceReferenceFrame(), 10, true);

        // Draw the direction of the surface velocity, as a labeled arrow
        ReferenceFrame refFrame = vessel.getSurfaceVelocityReferenceFrame();
        Drawing.Line line = drawing.addDirectionFromCom(
          new Triplet<Double, Double, Double>(0.0, 1.0, 0.0), refFrame, 10, true);
        line.setEndArrow(true);
        line.setLabel("Surface velocity");
        while (true) {
        }
    }
}
