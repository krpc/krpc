import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;

import java.io.IOException;
import java.util.Arrays;

public class NotLoadedStream {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Flight flight = spaceCenter.getActiveVessel().flight(null);

      // [docs]
      // altitude = meanAltitude / 1000, or 0 if the vessel is not loaded
      Expression altitude = Expression.variable(connection, "altitude", Type.double_(connection));
      Expression altitudeKm = Expression.blockWithVariables(
          connection,
          Arrays.asList(altitude),
          Arrays.asList(
              Expression.tryCatch(connection,
                  Expression.assign(connection, altitude,
                      Expression.divide(connection,
                          Expression.call(connection, connection.getCall(flight, "getMeanAltitude")),
                          Expression.constantDouble(connection, 1000))),
                  "KRPC", "ObjectNotLoadedException", null,
                  Expression.assign(connection, altitude,
                      Expression.constantDouble(connection, 0))),
              altitude));

      Stream<Double> stream = connection.addStream(altitudeKm);
      // [/docs]
      System.out.println(stream.get());
    }
  }
}
