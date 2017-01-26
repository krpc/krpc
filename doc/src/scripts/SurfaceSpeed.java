import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class SurfaceSpeed {
    public static void main(String[] args) throws IOException, RPCException, InterruptedException {
        Connection connection = Connection.newInstance("Surface speed");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();

        while (true) {

            Triplet<Double,Double,Double> velocity =
                vessel.flight(vessel.getOrbit().getBody().getReferenceFrame()).getVelocity();
            System.out.println("Surface velocity = ("
                               + velocity.getValue0() + "," + velocity.getValue1() + "," + velocity.getValue2()
                               + ")");

            double speed = vessel.flight(vessel.getOrbit().getBody().getReferenceFrame()).getSpeed();
            System.out.println("Surface speed = " + speed + " m/s");

            Thread.sleep(1000);
        }
    }
}
