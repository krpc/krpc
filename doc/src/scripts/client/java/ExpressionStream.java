import java.io.IOException;
import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

public class ExpressionStream {
  public static void main(String[] args)
      throws IOException, RPCException, StreamException, InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);

      // Create an expression on the server, that computes
      // the vessel's altitude in kilometers
      Expression expr = Expression.divide(
          connection,
          Expression.call(connection, connection.getCall(flight, "getMeanAltitude")),
          Expression.constantDouble(connection, 1000));

      // Stream the value of the expression
      Stream<Double> stream = connection.addStream(expr);
      while (true) {
        System.out.println("Altitude: " + stream.get() + " km");
        Thread.sleep(1000);
      }
    }
  }
}
