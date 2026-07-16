import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class VesselVelocity {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException {
      Connection connection = Connection.newInstance("Vessel velocity");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        ReferenceFrame refFrame = ReferenceFrame.createHybrid(
          connection,
          vessel.getOrbit().getBody().getReferenceFrame(),
          vessel.getSurfaceReferenceFrame(),
          vessel.getOrbit().getBody().getReferenceFrame(),
          vessel.getOrbit().getBody().getReferenceFrame());
        while (true) {
            Triplet<Double,Double,Double> velocity =
                vessel.flight(refFrame).getVelocity();
            System.out.printf("Surface velocity = (%.1f, %.1f, %.1f)\n",
                              velocity.getValue0(),
                              velocity.getValue1(),
                              velocity.getValue2());
            Thread.sleep(1000);
        }
    }
}
