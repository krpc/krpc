import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;

import java.io.IOException;

public class TruncateAltitude {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      Flight flight = vessel.flight(null);
      ProcedureCall meanAltitude = connection.getCall(flight, "getMeanAltitude");
      // [docs]
      // Truncate the altitude to a whole number of meters
      Expression expr = Expression.cast(
          connection,
          Expression.call(connection, meanAltitude),
          Type.int_(connection));
      // [/docs]
    }
  }
}
