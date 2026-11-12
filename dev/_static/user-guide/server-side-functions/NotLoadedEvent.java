import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

import java.util.Arrays;

public class NotLoadedEvent {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);

      // above = meanAltitude > 1000, or false if the vessel is not loaded
      Expression above = e.variable("above", t.bool());
      Expression altitude = e.call(connection.getCall(flight, "getMeanAltitude"));
      Expression above1000m = e.blockWithVariables(
        Arrays.asList(above),
        Arrays.asList(
          e.tryCatch(
            e.assign(above, e.greaterThan(altitude, e.constantDouble(1000))),
            "KRPC", "ObjectNotLoadedException", null,
            e.assign(above, e.constantBool(false))),
          above));

      Event event = connection.addEvent(above1000m);
      synchronized (event.getCondition()) {
        event.waitFor();
        System.out.println("Altitude reached 1000m");
      }
    }
  }
}
