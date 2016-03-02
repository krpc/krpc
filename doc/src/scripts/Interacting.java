import java.io.IOException;
import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

public class Interacting {
    public static void main (String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Vessel Name");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        System.out.println(vessel.getName());
    }
}
