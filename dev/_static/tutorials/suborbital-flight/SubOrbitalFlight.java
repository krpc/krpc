import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.VesselSituation;
import krpc.schema.KRPC.ProcedureCall;

import org.javatuples.Triplet;

public class SubOrbitalFlight {
  public static void main(String[] args) throws InterruptedException {
    Connection connection = Connection.newInstance("Sub-orbital flight");
    SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);

    SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

    vessel.getAutoPilot().targetPitchAndHeading(90, 90);
    vessel.getAutoPilot().setEngaged(true);
    vessel.getControl().setThrottle(0.5f);
    Thread.sleep(1000);

    System.out.println("Launch!");
    vessel.getControl().activateNextStage();

    Expression.Bound e = Expression.bind(connection);

    Flight flight = vessel.flight(null);
    {
      ProcedureCall meanAltitude =
        connection.getCall(flight, "getMeanAltitude");
      Expression expr = e.greaterThan(
        e.call(meanAltitude), e.constantDouble(500));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Pitching over");
    vessel.getAutoPilot().setTargetSmoothingTime(10);
    vessel.getAutoPilot().targetPitchAndHeading(70, 90);

    {
      ProcedureCall liquidFuel = connection.getCall(
        vessel.getResources(), "amount", "LiquidFuel");
      Expression expr = e.lessThan(
        e.call(liquidFuel), e.constantFloat(0.1f));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Flame out");
    vessel.getControl().setThrottle(0);
    vessel.getAutoPilot().setReferenceFrame(
      vessel.getSurfaceVelocityReferenceFrame());
    vessel.getAutoPilot().setTargetDirection(
      new Triplet<Double, Double, Double>(0.0, 1.0, 0.0));

    Flight bodyFlight =
      vessel.flight(vessel.getOrbit().getBody().getReferenceFrame());
    {
      ProcedureCall verticalSpeed =
        connection.getCall(bodyFlight, "getVerticalSpeed");
      Expression expr = e.lessThan(
        e.call(verticalSpeed), e.constantDouble(0));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Apoapsis reached, separating");
    vessel.getControl().activateNextStage();
    vessel.getAutoPilot().setTargetDirection(
      new Triplet<Double, Double, Double>(0.0, -1.0, 0.0));
    vessel.getAutoPilot().wait_(-1);
    vessel.getAutoPilot().setEngaged(false);

    {
      ProcedureCall srfAltitude =
        connection.getCall(flight, "getSurfaceAltitude");
      Expression expr = e.lessThan(
        e.call(srfAltitude), e.constantDouble(2000));
      Event event = connection.addEvent(expr);
      synchronized (event.getCondition()) {
        event.waitFor();
      }
    }

    System.out.println("Deploying the parachute");
    vessel.getParts().getParachutes().get(0).setDeployAltitude(550);
    vessel.getControl().activateNextStage();

    while (vessel.getSituation() != VesselSituation.LANDED &&
           vessel.getSituation() != VesselSituation.SPLASHED) {
      System.out.printf(
        "Altitude = %.1f meters\n", flight.getSurfaceAltitude());
      Thread.sleep(1000);
    }
    System.out.println("Landed!");
    connection.close();
  }
}
