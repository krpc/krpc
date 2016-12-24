import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.RemoteTech;
import krpc.client.services.RemoteTech.Antenna;
import krpc.client.services.RemoteTech.Comms;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.Vessel;

import java.io.IOException;

public class RemoteTechExample {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("RemoteTech Example");
        SpaceCenter sc = SpaceCenter.newInstance(connection);
        RemoteTech rt = RemoteTech.newInstance(connection);
        Vessel vessel = sc.getActiveVessel();

        // Set a dish target
        Part part = vessel.getParts().withTitle("Reflectron KR-7").get(0);
        Antenna antenna = rt.antenna(part);
        antenna.setTargetBody(sc.getBodies().get("Jool"));

        // Get info about the vessels communications
        Comms comms = rt.comms(vessel);
        System.out.println("Signal delay = " + comms.getSignalDelay());
    }
}
