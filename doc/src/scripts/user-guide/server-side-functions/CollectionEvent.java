import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.schema.KRPC.ProcedureCall;

import java.util.Arrays;
import java.util.Collections;

public class CollectionEvent {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);

      // A function taking an engine, returning true if it is out of fuel
      Expression engine = e.parameter("engine", t.classType("SpaceCenter", "Engine"));
      ProcedureCall hasFuel = connection.getCall(
        vessel.getParts().getEngines().get(0), "getHasFuel");
      Expression outOfFuel = e.lambda(Arrays.asList(engine),
        e.not(e.callWithArguments(hasFuel, Collections.singletonMap(0, engine))));

      // Whether any engine satisfies it
      ProcedureCall engines = connection.getCall(vessel.getParts(), "getEngines");
      Expression expr = e.any(e.call(engines), outOfFuel);

      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
        System.out.println("An engine has run out of fuel");
      }
    }
  }
}
