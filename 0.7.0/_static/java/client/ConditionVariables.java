import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Control;

import java.io.IOException;

public class ConditionVariables {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Control control = spaceCenter.getActiveVessel().getControl();
      Stream<Boolean> abort = connection.addStream(control, "getAbort");
      synchronized (abort.getCondition()) {
        while (!abort.get()) {
          abort.waitForUpdate();
        }
      }
    }
  }
}
