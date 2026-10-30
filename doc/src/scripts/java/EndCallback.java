import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;

import java.io.IOException;

public class EndCallback {
  public static void main(String[] args)
      throws IOException, RPCException, StreamException, InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Part part = spaceCenter.getActiveVessel().getParts().getRoot();
      Stream<Double> temperature = connection.addStream(part, "getTemperature");
      temperature.addCallback(value -> System.out.println("Temperature " + value));
      temperature.addEndCallback(
          exn -> System.out.println("The stream ended: " + exn.getMessage()));

      // Keep the program running, while the callbacks are called
      while (true) {
        Thread.sleep(1000);
      }
    }
  }
}
