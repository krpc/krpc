import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

import java.io.IOException;
import java.util.Arrays;

public class NotLoadedEvent {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);

      // above = meanAltitude > 1000, or false if the vessel is not loaded
      Expression above = Expression.variable(connection, "above", Type.bool(connection));
      Expression above1000m = Expression.blockWithVariables(
        connection,
        Arrays.asList(above),
        Arrays.asList(
          Expression.tryCatch(connection,
            Expression.assign(connection, above,
              Expression.greaterThan(connection,
                Expression.call(connection, connection.getCall(flight, "getMeanAltitude")),
                Expression.constantDouble(connection, 1000))),
            "KRPC", "ObjectNotLoadedException", null,
            Expression.assign(connection, above,
              Expression.constantBool(connection, false))),
          above));

      Event event = connection.addEvent(above1000m);
      synchronized (event.getCondition()) {
        event.waitFor();
        System.out.println("Altitude reached 1000m");
      }
    }
  }
}
