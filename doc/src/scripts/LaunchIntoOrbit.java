import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Node;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Resources;

import org.javatuples.Triplet;

import java.io.IOException;
import java.lang.Math;

public class LaunchIntoOrbit {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException, StreamException {
        Connection connection = Connection.newInstance("Launch into orbit");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

        float turnStartAltitude = 250;
        float turnEndAltitude = 45000;
        float targetAltitude = 150000;

        // Set up streams for telemetry
        spaceCenter.getUT();
        Stream<Double> ut = connection.addStream(SpaceCenter.class, "getUT");
        ReferenceFrame refFrame = vessel.getSurfaceReferenceFrame();
        Flight flight = vessel.flight(refFrame);
        Stream<Double> altitude = connection.addStream(flight, "getMeanAltitude");
        Stream<Double> apoapsis =
            connection.addStream(vessel.getOrbit(), "getApoapsisAltitude");
        Resources stage3Resources = vessel.resourcesInDecoupleStage(3, false);
        Stream<Float> srbFuel =
            connection.addStream(stage3Resources, "amount", "SolidFuel");

        // Pre-launch setup
        vessel.getControl().setSAS(false);
        vessel.getControl().setRCS(false);
        vessel.getControl().setThrottle(1);

        // Countdown...
        System.out.println("3...");
        Thread.sleep(1000);
        System.out.println("2...");
        Thread.sleep(1000);
        System.out.println("1...");
        Thread.sleep(1000);
        System.out.println("Launch!");

        // Activate the first stage
        vessel.getControl().activateNextStage();
        vessel.getAutoPilot().engage();
        vessel.getAutoPilot().targetPitchAndHeading(90, 90);

        // Main ascent loop
        boolean srbsSeparated = false;
        double turnAngle = 0;
        while (true) {

            // Gravity turn
            if (altitude.get() > turnStartAltitude &&
                altitude.get() < turnEndAltitude) {
                double frac = (altitude.get() - turnStartAltitude)
                              / (turnEndAltitude - turnStartAltitude);
                double newTurnAngle = frac * 90.0;
                if (Math.abs(newTurnAngle - turnAngle) > 0.5) {
                    turnAngle = newTurnAngle;
                    vessel.getAutoPilot().targetPitchAndHeading(
                        (float)(90 - turnAngle), 90);
                }
            }

            // Separate SRBs when finished
            if (!srbsSeparated) {
              if (srbFuel.get() < 0.1) {
                    vessel.getControl().activateNextStage();
                    srbsSeparated = true;
                    System.out.println("SRBs separated");
                }
            }

            // Decrease throttle when approaching target apoapsis
            if (apoapsis.get() > targetAltitude * 0.9) {
                System.out.println("Approaching target apoapsis");
                break;
            }
        }

        // Disable engines when target apoapsis is reached
        vessel.getControl().setThrottle(0.25f);
        while (apoapsis.get() < targetAltitude) {
        }
        System.out.println("Target apoapsis reached");
        vessel.getControl().setThrottle(0);

        // Wait until out of atmosphere
        System.out.println("Coasting out of atmosphere");
        while (altitude.get() < 70500) {
        }

        // Plan circularization burn (using vis-viva equation)
        System.out.println("Planning circularization burn");
        double mu = vessel.getOrbit().getBody().getGravitationalParameter();
        double r = vessel.getOrbit().getApoapsis();
        double a1 = vessel.getOrbit().getSemiMajorAxis();
        double a2 = r;
        double v1 = Math.sqrt(mu * ((2.0 / r) - (1.0 / a1)));
        double v2 = Math.sqrt(mu * ((2.0 / r) - (1.0 / a2)));
        double deltaV = v2 - v1;
        Node node = vessel.getControl().addNode(
          ut.get() + vessel.getOrbit().getTimeToApoapsis(), (float)deltaV, 0, 0);

        // Calculate burn time (using rocket equation)
        double force = vessel.getAvailableThrust();
        double isp = vessel.getSpecificImpulse() * 9.82;
        double m0 = vessel.getMass();
        double m1 = m0 / Math.exp(deltaV / isp);
        double flowRate = force / isp;
        double burnTime = (m0 - m1) / flowRate;

        // Orientate ship
        System.out.println("Orientating ship for circularization burn");
        vessel.getAutoPilot().setReferenceFrame(node.getReferenceFrame());
        vessel.getAutoPilot().setTargetDirection(
          new Triplet<Double,Double,Double>(0.0, 1.0, 0.0));
        vessel.getAutoPilot().wait_();

        // Wait until burn
        System.out.println("Waiting until circularization burn");
        double burnUt =
          ut.get() + vessel.getOrbit().getTimeToApoapsis() - (burnTime / 2.0);
        double leadTime = 5;
        spaceCenter.warpTo(burnUt - leadTime, 100000, 2);

        // Execute burn
        System.out.println("Ready to execute burn");
        Stream<Double> timeToApoapsis =
          connection.addStream(vessel.getOrbit(), "getTimeToApoapsis");
        while (timeToApoapsis.get() - (burnTime / 2.0) > 0) {
        }
        System.out.println("Executing burn");
        vessel.getControl().setThrottle(1);
        Thread.sleep((int)((burnTime - 0.1) * 1000));
        System.out.println("Fine tuning");
        vessel.getControl().setThrottle(0.05f);
        Stream<Triplet<Double,Double,Double>> remainingBurn =
          connection.addStream(
            node, "remainingBurnVector", node.getReferenceFrame());
        while (remainingBurn.get().getValue1() > 0) {
        }
        vessel.getControl().setThrottle(0);
        node.remove();

        System.out.println("Launch complete");
        connection.close();
    }
}
