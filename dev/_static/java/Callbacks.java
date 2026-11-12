import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Control;


public class Callbacks {
  public static void main(String[] args) throws InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Control control = spaceCenter.getActiveVessel().getControl();
      Stream<Boolean> abort = connection.addStream(control, "getAbort");
      abort.addCallback(x -> System.out.println("Abort 1 called with a value of " + x));
      abort.addCallback(x -> System.out.println("Abort 2 called with a value of " + x));

      // Keep the program running, while the callbacks are called
      while (true) {
        Thread.sleep(1000);
      }
    }
  }
}
