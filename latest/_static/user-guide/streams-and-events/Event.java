import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

public class Event {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);
      // [docs]
      Expression.Bound e = Expression.bind(connection);
      var event = connection.addEvent(e.greaterThan(
          e.call(connection.getCall(flight, "getMeanAltitude")),
          e.constantDouble(10000)));
      synchronized (event.getCondition()) {
        event.waitFor();
      }
      System.out.println("Above 10 km");
      // [/docs]
    }
  }
}
