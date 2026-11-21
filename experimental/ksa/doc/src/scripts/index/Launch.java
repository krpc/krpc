import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KSA;
import krpc.schema.KRPC.ProcedureCall;

import java.io.IOException;

public class Launch {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance("Launch")) {
      KSA ksa = KSA.newInstance(connection);
      KSA.Vehicle vehicle = ksa.launchVehicle("Rocket", "CCSFS LC-39A");
      KSA.Flight flight = vehicle.getFlight();

      vehicle.getAutoPilot().targetPitchAndHeading(90, 90);
      vehicle.getAutoPilot().engage();
      vehicle.getControl().setThrottle(1);
      vehicle.getControl().activateNextSequence();
      vehicle.getControl().setEngineOn(true);
      System.out.println(vehicle.getName() + " has launched");

      ProcedureCall meanAltitude = connection.getCall(flight, "getMeanAltitude");
      Expression expr = Expression.greaterThan(
        connection,
        Expression.call(connection, meanAltitude),
        Expression.constantDouble(connection, 10000));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }

      vehicle.getControl().setEngineOn(false);
      vehicle.getAutoPilot().disengage();
      System.out.println(vehicle.getName() + " reached 10 km");
    }
  }
}
