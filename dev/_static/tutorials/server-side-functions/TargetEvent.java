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

public class TargetEvent {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel someVessel = spaceCenter.getVessels().get(0);
      // [docs]
      ProcedureCall target = connection.getCall(SpaceCenter.class, "getTargetVessel");
      Expression expr = Expression.equal(connection,
          Expression.call(connection, target),
          Expression.constantObject(connection, someVessel.id));
      Event event = connection.addEvent(expr);
      // [/docs]
    }
  }
}
