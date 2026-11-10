import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

import java.io.IOException;

public class EventScope {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);
      Expression.Bound e = Expression.bind(connection);
      Expression expr = e.greaterThan(
          e.call(connection.getCall(flight, "getMeanAltitude")), e.constantDouble(1000));
      try (Event event = connection.addEvent(expr)) {
        synchronized (event.getCondition()) {
          event.waitFor();
          System.out.println("Altitude reached 1000m");
        }
      }
      // The event was removed at the end of the block
    }
  }
}
