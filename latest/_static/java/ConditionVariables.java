import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Control;


public class ConditionVariables {
  public static void main(String[] args) {
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
