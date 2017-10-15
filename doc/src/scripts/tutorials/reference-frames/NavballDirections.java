import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class NavballDirections {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Navball directions");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        AutoPilot ap = vessel.getAutoPilot();
        ap.setReferenceFrame(vessel.getSurfaceReferenceFrame());
        ap.engage();

        // Point the vessel north on the navball, with a pitch of 0 degrees
        ap.setTargetDirection(new Triplet<Double,Double,Double> (0.0, 1.0, 0.0));
        ap.wait_();

        // Point the vessel vertically upwards on the navball
        ap.setTargetDirection(new Triplet<Double,Double,Double> (1.0, 0.0, 0.0));
        ap.wait_();

        // Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
        ap.setTargetDirection(new Triplet<Double,Double,Double> (0.0, 0.0, -1.0));
        ap.wait_();

        ap.disengage();
        connection.close();
    }
}
