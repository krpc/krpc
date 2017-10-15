import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class OrbitalDirections {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Orbital directions");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        AutoPilot ap = vessel.getAutoPilot();
        ap.setReferenceFrame(vessel.getOrbitalReferenceFrame());
        ap.engage();

        // Point the vessel in the prograde direction
        ap.setTargetDirection(new Triplet<Double,Double,Double> (0.0, 1.0, 0.0));
        ap.wait_();

        // Point the vessel in the orbit normal direction
        ap.setTargetDirection(new Triplet<Double,Double,Double> (0.0, 0.0, 1.0));
        ap.wait_();

        // Point the vessel in the orbit radial direction
        ap.setTargetDirection(new Triplet<Double,Double,Double> (-1.0, 0.0, 0.0));
        ap.wait_();

        ap.disengage();
        connection.close();
    }
}
