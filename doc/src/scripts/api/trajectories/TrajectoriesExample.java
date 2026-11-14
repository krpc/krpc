import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.Trajectories;


public class TrajectoriesExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("Trajectories Example");
        CelestialBody body =
            SpaceCenter.newInstance(connection).getActiveVessel().getOrbit().getBody();
        System.out.println(
            Trajectories.newInstance(connection).impactPosition(body.getReferenceFrame()));
        connection.close();
    }
}
