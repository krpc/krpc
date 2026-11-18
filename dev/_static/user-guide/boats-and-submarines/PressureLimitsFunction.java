import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;

import java.util.Arrays;
import java.util.Collections;
import java.util.List;

public class PressureLimitsFunction {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);
      Expression part =
          e.parameter("part", t.classType("SpaceCenter", "Part"));

      Expression over = e.greaterThan(
          e.add(property(connection, e, part, "getStaticPressure"),
                property(connection, e, part, "getDynamicPressure")),
          property(connection, e, part, "getMaxPressure"));
      Expression titles = e.select(
          e.where(
              e.call(connection.getCall(vessel.getParts(), "getAll")),
              e.lambda(Arrays.asList(part), over)),
          e.lambda(Arrays.asList(part),
                   property(connection, e, part, "getTitle")));
      List<String> overLimit = connection.run(e.toList(titles));
      // [/docs]
      for (String title : overLimit) {
        System.out.println(title);
      }
    }
  }

  // A property of the part parameter
  static Expression property(Connection connection, Expression.Bound e,
                             Expression part, String name) {
    ProcedureCall call = connection.getCallTemplate(Part.class, name);
    return e.callWithArguments(call, Collections.singletonMap(0, part));
  }
}
