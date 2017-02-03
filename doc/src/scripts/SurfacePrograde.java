import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class SurfacePrograde {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Surface prograde");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        AutoPilot ap = vessel.getAutoPilot();

        ap.setReferenceFrame(vessel.getSurfaceVelocityReferenceFrame());
        ap.setTargetDirection(new Triplet<Double,Double,Double>(0.0, 1.0, 0.0));
        ap.engage();
        ap.wait_();
        ap.disengage();
        connection.close();
    }
}
