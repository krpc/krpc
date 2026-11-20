import krpc.client.Connection;
import krpc.client.RemoteObject;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Control;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.client.services.StdLib;
import krpc.schema.KRPC.ProcedureCall;

import java.util.HashMap;
import java.util.Map;

public class CompiledStep {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      Flight flight = vessel.flight(null);
      Control control = vessel.getControl();
      double targetPitch = 45;
      // [docs]
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);

      // control.setPitch(
      //   clamp(0.05 * (targetPitch - flight.getPitch()), -1, 1));
      ProcedureCall pitch = connection.getCall(flight, "getPitch");
      Expression error =
          e.subtract(e.constantDouble(targetPitch), e.call(pitch));
      Map<Integer, Expression> clampArgs = new HashMap<>();
      clampArgs.put(0, e.multiply(e.constantDouble(0.05), error));
      clampArgs.put(1, e.constantDouble(-1));
      clampArgs.put(2, e.constantDouble(1));
      Expression command = e.callWithArguments(
          connection.getCallTemplate(StdLib.class, "clamp"), clampArgs);

      Map<Integer, Expression> setterArgs = new HashMap<>();
      setterArgs.put(
          0, e.constantObject(RemoteObject.getObjectId(control)));
      setterArgs.put(1, e.cast(command, t.float_()));
      Expression step = connection.compile(e.callWithArguments(
          connection.getCallTemplate(Control.class, "setPitch"),
          setterArgs));

      while (true) {
        connection.run(step);
      }
      // [/docs]
    }
  }
}
