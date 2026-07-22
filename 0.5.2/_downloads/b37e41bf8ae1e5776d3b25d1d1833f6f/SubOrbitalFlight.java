import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Node;
import krpc.client.services.SpaceCenter.Resources;
import krpc.schema.KRPC.ProcedureCall;

import org.javatuples.Triplet;

import java.io.IOException;

public class SubOrbitalFlight {
  public static void main(String[] args)
    throws IOException, RPCException, StreamException, InterruptedException {
    Connection connection = Connection.newInstance("Sub-orbital flight");
    KRPC krpc = KRPC.newInstance(connection);
    SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);

    SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

    vessel.getAutoPilot().targetPitchAndHeading(90, 90);
    vessel.getAutoPilot().engage();
    vessel.getControl().setThrottle(1);
    Thread.sleep(1000);

    System.out.println("Launch!");
    vessel.getControl().activateNextStage();

    {
      ProcedureCall solidFuel = connection.getCall(vessel.getResources(), "amount", "SolidFuel");
      Expression expr = Expression.lessThan(
        connection,
        Expression.call(connection, solidFuel),
        Expression.constantFloat(connection, 0.1f));
      Event event = krpc.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Booster separation");
    vessel.getControl().activateNextStage();

    {
      ProcedureCall meanAltitude = connection.getCall(vessel.flight(null), "getMeanAltitude");
      Expression expr = Expression.greaterThan(
        connection,
        Expression.call(connection, meanAltitude),
        Expression.constantDouble(connection, 10000));
      Event event = krpc.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Gravity turn");
    vessel.getAutoPilot().targetPitchAndHeading(60, 90);

    {
      ProcedureCall apoapsisAltitude = connection.getCall(
        vessel.getOrbit(), "getApoapsisAltitude");
      Expression expr = Expression.greaterThan(
        connection,
        Expression.call(connection, apoapsisAltitude),
        Expression.constantDouble(connection, 100000));
      Event event = krpc.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Launch stage separation");
    vessel.getControl().setThrottle(0);
    Thread.sleep(1000);
    vessel.getControl().activateNextStage();
    vessel.getAutoPilot().disengage();

    {
      ProcedureCall srfAltitude = connection.getCall(
        vessel.flight(null), "getSurfaceAltitude");
      Expression expr = Expression.lessThan(
        connection,
        Expression.call(connection, srfAltitude),
        Expression.constantDouble(connection, 1000));
      Event event = krpc.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    vessel.getControl().activateNextStage();

    while (vessel.flight(vessel.getOrbit().getBody().getReferenceFrame()).getVerticalSpeed() < -0.1) {
      System.out.printf("Altitude = %.1f meters\n", vessel.flight(null).getSurfaceAltitude());
      Thread.sleep(1000);
    }
    System.out.println("Landed!");
    connection.close();
  }
}
