import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;

import java.util.Arrays;
import java.util.Collections;

public class DeployParachutesFunction {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);

      // for (Parachute parachute : vessel.getParts().getParachutes())
      //   parachute.deploy();
      Expression parachute = e.variable(
          "parachute", t.classType("SpaceCenter", "Parachute"));
      ProcedureCall deploy = connection.getCallTemplate(
          SpaceCenter.Parachute.class, "deploy");
      ProcedureCall parachutes =
          connection.getCall(vessel.getParts(), "getParachutes");
      Expression program = e.blockWithVariables(
          Arrays.asList(parachute),
          Arrays.asList(e.forEach(
              parachute, e.call(parachutes),
              e.callWithArguments(
                  deploy, Collections.singletonMap(0, parachute)))));

      connection.run(program);
      // [/docs]
    }
  }
}
