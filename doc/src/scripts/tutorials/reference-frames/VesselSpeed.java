import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class VesselSpeed {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException {
        Connection connection = Connection.newInstance("Vessel speed");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        ReferenceFrame obtFrame = vessel.getOrbit().getBody().getNonRotatingReferenceFrame();
        ReferenceFrame srfFrame = vessel.getOrbit().getBody().getReferenceFrame();
        while (true) {
            double obtSpeed = vessel.flight(obtFrame).getSpeed();
            double srfSpeed = vessel.flight(srfFrame).getSpeed();
            System.out.printf(
              "Orbital speed = %.1f m/s, Surface speed = %.1f m/s\n",
              obtSpeed, srfSpeed);
            Thread.sleep(1000);
        }
    }
}
