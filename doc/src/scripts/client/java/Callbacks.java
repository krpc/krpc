import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Control;

import java.io.IOException;

public class Callbacks {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    Connection connection = Connection.newInstance();
    SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
    Control control = spaceCenter.getActiveVessel().getControl();
    Stream<Boolean> abort = connection.addStream(control, "getAbort");
    abort.addCallback(
      (Boolean x) -> {
        System.out.println("Abort 1 called with a value of " + x);
      });
    abort.addCallback(
      (Boolean x) -> {
        System.out.println("Abort 2 called with a value of " + x);
      });
    abort.start();

    // Keep the program running...
    while (true) {
    }
  }
}
