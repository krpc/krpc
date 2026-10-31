import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KRPC;

import java.io.IOException;

public class Connecting3 {
  public static void main(String[] args) throws IOException, RPCException {
    Connection connection = Connection.newInstance("My Example Program");
    System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    connection.close();
  }
}
