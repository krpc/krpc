import krpc.client.Connection;
import krpc.client.services.Debug;


public class DebugExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("Debug Example");
        Debug.newInstance(connection).setInfinitePropellant(true);
        connection.close();
    }
}
