import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class VesselPosition {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance();
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        Triplet<Double, Double, Double> position =
          vessel.position(vessel.getOrbit().getBody().getReferenceFrame());
        System.out.printf("(%.1f, %.1f, %.1f)\n",
                          position.getValue0(),
                          position.getValue1(),
                          position.getValue2());
        connection.close();
    }
}
