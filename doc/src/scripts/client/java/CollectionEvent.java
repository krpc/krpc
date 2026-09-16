import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.schema.KRPC.ProcedureCall;

import java.io.IOException;
import java.util.Arrays;
import java.util.Collections;

public class CollectionEvent {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      KRPC krpc = KRPC.newInstance(connection);
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

      // A function taking an engine, returning true if it is out of fuel
      Expression engine = Expression.parameter(
        connection, "engine",
        Type.classType(connection, "SpaceCenter", "Engine"));
      ProcedureCall hasFuel = connection.getCall(
        vessel.getParts().getEngines().get(0), "getHasFuel");
      Expression outOfFuel = Expression.lambda(
        connection, Arrays.asList(engine),
        Expression.not(connection, Expression.callWithArguments(
          connection, hasFuel, Collections.singletonMap(0, engine))));

      // Whether any engine satisfies it
      ProcedureCall engines = connection.getCall(vessel.getParts(), "getEngines");
      Expression expr = Expression.any(
        connection, Expression.call(connection, engines), outOfFuel);

      Event event = krpc.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
        System.out.println("An engine has run out of fuel");
      }
    }
  }
}
