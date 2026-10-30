import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;

import java.io.IOException;

public class StreamEnd {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Part part = spaceCenter.getActiveVessel().getParts().getRoot();
      Stream<Double> temperature = connection.addStream(part, "getTemperature");
      try {
        synchronized (temperature.getCondition()) {
          while (true) {
            temperature.waitForUpdate();
            System.out.println("Temperature " + temperature.get());
          }
        }
      } catch (KRPC.ObjectDestroyedException exn) {
        System.out.println("The part was destroyed");
      }
    }
  }
}
