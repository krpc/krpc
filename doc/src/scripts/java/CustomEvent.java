import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.schema.KRPC.ProcedureCall;


public class CustomEvent {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);

      // Get the remote procedure call as a message object,
      // so it can be passed to the server
      ProcedureCall meanAltitude = connection.getCall(flight, "getMeanAltitude");

      // Build the function on the server
      Expression.Bound e = Expression.bind(connection);
      Expression expr = e.greaterThan(e.call(meanAltitude), e.constantDouble(1000));

      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
        System.out.println("Altitude reached 1000m");
      }
    }
  }
}
