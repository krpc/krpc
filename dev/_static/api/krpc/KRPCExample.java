import krpc.client.Connection;
import krpc.client.services.KRPC;


public class KRPCExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("KRPC Example");
        System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
        connection.close();
    }
}
