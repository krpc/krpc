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

        ReferenceFrame refFrame = vessel.getSurfaceVelocityReferenceFrame();
        drawing.addDirection(
          new Triplet<Double, Double, Double>(0.0, 1.0, 0.0), refFrame, 10, true);
        while (true) {
        }
    }
}
