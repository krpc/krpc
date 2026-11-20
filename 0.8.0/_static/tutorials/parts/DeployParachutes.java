import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Parachute;
import krpc.client.services.SpaceCenter.Vessel;


public class DeployParachutes {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance();
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        for (Parachute parachute : vessel.getParts().getParachutes()) {
            parachute.deploy();
        }
        connection.close();
    }
}
