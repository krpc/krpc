import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.schema.KRPC.ProcedureCall;


public class Launch {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance("Launch")) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();
      SpaceCenter.Flight flight = vessel.flight(null);

      vessel.getAutoPilot().targetPitchAndHeading(90, 90);
      vessel.getAutoPilot().setEngaged(true);
      vessel.getControl().setThrottle(1);
      vessel.getControl().activateNextStage();
      System.out.println(vessel.getName() + " has launched");

      ProcedureCall meanAltitude = connection.getCall(flight, "getMeanAltitude");
      Expression.Bound e = Expression.bind(connection);
      Expression expr = e.greaterThan(e.call(meanAltitude), e.constantDouble(10000));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }

      vessel.getControl().setThrottle(0);
      vessel.getAutoPilot().setEngaged(false);
      System.out.println(vessel.getName() + " reached 10 km");
    }
  }
}
