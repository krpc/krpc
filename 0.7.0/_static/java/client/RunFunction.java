import java.io.IOException;
import java.util.Arrays;
import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;
import org.javatuples.Pair;

public class RunFunction {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      Flight flight = vessel.flight(vessel.getOrbit().getBody().getReferenceFrame());

      // Get the remote procedure calls as message objects,
      // so they can be passed to the server
      ProcedureCall meanAltitude = connection.getCall(flight, "getMeanAltitude");
      ProcedureCall speed = connection.getCall(flight, "getSpeed");

      // Build a function that reads the altitude and the speed
      Expression expr = Expression.createTuple(connection, Arrays.asList(
          Expression.call(connection, meanAltitude),
          Expression.call(connection, speed)));

      // Run the function once, so both values come from the same physics tick
      Pair<Double, Double> result = connection.run(expr);
      System.out.println(
          "Altitude: " + result.getValue0() + " m, speed: " + result.getValue1() + " m/s");
    }
  }
}
