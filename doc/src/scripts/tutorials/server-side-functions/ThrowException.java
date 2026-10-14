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

public class ThrowException {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      // [docs]
      ProcedureCall amount = connection.getCall(
          vessel.getResources(), "amount", "LiquidFuel");
      Expression expr = Expression.ifThen(connection,
          Expression.lessThan(connection,
              Expression.call(connection, amount),
              Expression.constantDouble(connection, 100)),
          Expression.throw_(connection, "KRPC", "InvalidOperationException",
              Expression.constantString(connection, "not enough fuel")));
      connection.run(expr);
      // [/docs]
    }
  }
}
