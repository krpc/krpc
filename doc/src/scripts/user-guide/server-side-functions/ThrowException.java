import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;


public class ThrowException {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      // [docs]
      Expression.Bound e = Expression.bind(connection);
      ProcedureCall amount = connection.getCall(
          vessel.getResources(), "amount", "LiquidFuel");
      Expression expr = e.ifThen(
          e.lessThan(e.call(amount), e.constantDouble(100)),
          e.throw_("KRPC", "InvalidOperationException", e.constantString("not enough fuel")));
      connection.run(expr);
      // [/docs]
    }
  }
}
