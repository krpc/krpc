import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KRPC;

import java.io.IOException;

public class ConnectingLocal {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newLocalInstance("My Example Program")) {
      System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    }
  }
}
