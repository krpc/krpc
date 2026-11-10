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
      Expression.Bound e = Expression.bind(connection);
      // altitude = meanAltitude / 1000, or 0 if the vessel is not loaded
      Expression altitude = e.variable("altitude", Type.bind(connection).double_());
      Expression km = e.divide(
          e.call(connection.getCall(flight, "getMeanAltitude")), e.constantDouble(1000));
      Expression altitudeKm = e.blockWithVariables(
          Arrays.asList(altitude),
          Arrays.asList(
              e.tryCatch(
                  e.assign(altitude, km),
                  "KRPC", "ObjectNotLoadedException", null,
                  e.assign(altitude, e.constantDouble(0))),
              altitude));

      Stream<Double> stream = connection.addStream(altitudeKm);
      // [/docs]
      System.out.println(stream.get());
    }
  }
}
