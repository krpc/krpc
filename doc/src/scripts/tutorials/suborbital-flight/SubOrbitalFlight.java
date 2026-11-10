import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.schema.KRPC.ProcedureCall;

import java.io.IOException;

public class SubOrbitalFlight {
  public static void main(String[] args)
    throws IOException, RPCException, StreamException, InterruptedException {
    Connection connection = Connection.newInstance("Sub-orbital flight");
    SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);

    SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

    vessel.getAutoPilot().targetPitchAndHeading(90, 90);
    vessel.getAutoPilot().setEngaged(true);
    vessel.getControl().setThrottle(1);
    Thread.sleep(1000);

    System.out.println("Launch!");
    vessel.getControl().activateNextStage();

    Expression.Bound e = Expression.bind(connection);
    {
      ProcedureCall solidFuel = connection.getCall(vessel.getResources(), "amount", "SolidFuel");
      Expression expr = e.lessThan(e.call(solidFuel), e.constantFloat(0.1f));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }
    System.out.println("Booster separation");
    vessel.getControl().activateNextStage();

    Flight flight = vessel.flight(null);
    {
      ProcedureCall meanAltitude = connection.getCall(flight, "getMeanAltitude");
      Expression expr = e.greaterThan(e.call(meanAltitude), e.constantDouble(10000));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Gravity turn");
    vessel.getAutoPilot().targetPitchAndHeading(60, 90);

    {
      ProcedureCall apoapsisAltitude = connection.getCall(
        vessel.getOrbit(), "getApoapsisAltitude");
      Expression expr = e.greaterThan(e.call(apoapsisAltitude), e.constantDouble(100000));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Launch stage separation");
    vessel.getControl().setThrottle(0);
    Thread.sleep(1000);
    vessel.getControl().activateNextStage();
    vessel.getAutoPilot().setEngaged(false);

    {
      ProcedureCall srfAltitude = connection.getCall(flight, "getSurfaceAltitude");
      Expression expr = e.lessThan(e.call(srfAltitude), e.constantDouble(1000));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    vessel.getControl().activateNextStage();

    while (vessel.flight(vessel.getOrbit().getBody().getReferenceFrame()).getVerticalSpeed() < -0.1) {
      System.out.printf("Altitude = %.1f meters\n", flight.getSurfaceAltitude());
      Thread.sleep(1000);
    }
    System.out.println("Landed!");
    connection.close();
  }
}
