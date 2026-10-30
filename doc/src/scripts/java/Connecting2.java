import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KRPC;

import java.io.IOException;

public class Connecting2 {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection =
             Connection.newInstance("My Example Program", "192.168.1.10", 1000)) {
      System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    }
  }
}
