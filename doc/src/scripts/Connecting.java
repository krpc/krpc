import java.io.IOException;
import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KRPC;

public class Connecting {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Remote example", "my.domain.name", 1000, 1001);
        System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    }
}
