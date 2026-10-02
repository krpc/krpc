import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Node;
import krpc.client.services.SpaceCenter.Resources;

import org.javatuples.Triplet;

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

    while (vessel.getResources().amount("SolidFuel") > 0.1) {
      Thread.sleep(1000);
    }

    System.out.println("Booster separation");
    vessel.getControl().activateNextStage();

    while (vessel.flight(null).getMeanAltitude() < 10000) {
      Thread.sleep(1000);
    }

    System.out.println("Gravity turn");
    vessel.getAutoPilot().targetPitchAndHeading(60, 90);

    while (vessel.getOrbit().getApoapsisAltitude() < 100000) {
      Thread.sleep(1000);
    }

    System.out.println("Launch stage separation");
    vessel.getControl().setThrottle(0);
    Thread.sleep(1000);
    vessel.getControl().activateNextStage();
    vessel.getAutoPilot().setEngaged(false);

    while (vessel.flight(null).getSurfaceAltitude() > 1000) {
      Thread.sleep(1000);
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
