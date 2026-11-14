import krpc.client.Connection;
import krpc.client.services.SpaceCenter;


public class SpaceCenterExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("SpaceCenter Example");
        System.out.println(SpaceCenter.newInstance(connection).getActiveVessel().getName());
        connection.close();
    }
}
