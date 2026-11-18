import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Engine;
import krpc.client.services.SpaceCenter.Vessel;

import java.util.Arrays;
import java.util.Collections;
import java.util.function.Function;

public class CombinedIspFunction {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);
      Expression engine =
          e.parameter("engine", t.classType("SpaceCenter", "Engine"));
      // A property of the engine parameter
      Function<String, Expression> property = name -> e.callWithArguments(
          connection.getCallTemplate(Engine.class, name),
          Collections.singletonMap(0, engine));

      // The active engines with fuel
      Expression fueled = e.lambda(Arrays.asList(engine),
          e.conditionalAnd(
              property.apply("getActive"), property.apply("getHasFuel")));
      Expression engines = e.where(
          e.call(connection.getCall(vessel.getParts(), "getEngines")),
          fueled);

      Expression thrust = property.apply("getAvailableThrust");
      Expression isp = property.apply("getSpecificImpulse");
      Expression totalThrust = e.sum(e.select(
          engines, e.lambda(Arrays.asList(engine), thrust)));
      Expression fuelConsumption = e.sum(e.select(
          engines,
          e.lambda(Arrays.asList(engine), e.divide(thrust, isp))));

      float combinedIsp =
          connection.run(e.divide(totalThrust, fuelConsumption));
      // [/docs]
      System.out.printf("Combined Isp = %.0f seconds\n", combinedIsp);
    }
  }
}
