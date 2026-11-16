import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.Stream;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Node;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Resources;
import krpc.schema.KRPC.ProcedureCall;

import org.javatuples.Triplet;

import java.lang.Math;

public class LaunchIntoOrbit {
    public static void main(String[] args) throws InterruptedException {
        Connection connection = Connection.newInstance("Launch into orbit");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

        float turnStartAltitude = 250;
        float turnEndAltitude = 45000;
        float targetAltitude = 150000;

        // Set up streams for telemetry
        spaceCenter.getUT();
        Stream<Double> ut =
            connection.addStream(SpaceCenter.class, "getUT");
        ReferenceFrame refFrame = vessel.getSurfaceReferenceFrame();
        Flight flight = vessel.flight(refFrame);
        Stream<Double> altitude =
            connection.addStream(flight, "getMeanAltitude");
        Stream<Double> apoapsis =
            connection.addStream(vessel.getOrbit(), "getApoapsisAltitude");
        Resources stage5Resources =
            vessel.decoupleStageAt(5).resources(false);
        Resources stage3Resources =
            vessel.decoupleStageAt(3).resources(false);
        Stream<Float> firstStageFuel =
            connection.addStream(stage5Resources, "amount", "SolidFuel");
        Stream<Float> secondStageFuel =
            connection.addStream(stage3Resources, "amount", "LiquidFuel");

        // Pre-launch setup
        vessel.getControl().setSAS(false);
        vessel.getControl().setRCS(false);
        vessel.getControl().setThrottle(1);

        // Activate the first stage
        vessel.getAutoPilot().setEngaged(true);
        vessel.getAutoPilot().targetPitchAndHeading(90, 90);
        vessel.getControl().activateNextStage();

        // Main ascent loop
        boolean firstStageSeparated = false;
        boolean secondStageSeparated = false;
        while (true) {
            // Gravity turn
            if (altitude.get() > turnStartAltitude &&
                altitude.get() < turnEndAltitude) {
                double frac = (altitude.get() - turnStartAltitude)
                              / (turnEndAltitude - turnStartAltitude);
                double turnAngle = frac * 90.0;
                vessel.getAutoPilot().targetPitchAndHeading(
                    (float)(90 - turnAngle), 90);
            }

            if (!firstStageSeparated) {
                if (firstStageFuel.get() < 0.1) {
                    vessel.getControl().activateNextStage();
                    vessel.getControl().activateNextStage();
                    firstStageSeparated = true;
                    System.out.println("First stage separated");
                }
            } else if (!secondStageSeparated) {
                if (secondStageFuel.get() < 0.1) {
                    vessel.getControl().activateNextStage();
                    vessel.getControl().activateNextStage();
                    secondStageSeparated = true;
                    System.out.println("Second stage separated");
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
        Expression.Bound e = Expression.bind(connection);
        {
          ProcedureCall apoapsisAltitude =
            connection.getCall(vessel.getOrbit(), "getApoapsisAltitude");
          Expression expr = e.greaterThanOrEqual(
            e.call(apoapsisAltitude), e.constantDouble(targetAltitude));
          Event event = connection.addEvent(expr);
          synchronized (event.getCondition()) {
            event.waitFor();
          }
        }
        System.out.println("Target apoapsis reached");
        vessel.getControl().setThrottle(0);

        // Wait until out of atmosphere
        System.out.println("Coasting out of atmosphere");
        {
          ProcedureCall meanAltitude =
            connection.getCall(vessel.flight(null), "getMeanAltitude");
          Expression expr = e.greaterThanOrEqual(
            e.call(meanAltitude), e.constantDouble(70500));
          Event event = connection.addEvent(expr);
          synchronized (event.getCondition()) {
            event.waitFor();
          }
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
          ut.get() + vessel.getOrbit().getTimeToApoapsis(),
          (float)deltaV, 0, 0);

        // Calculate burn time (using rocket equation)
        double force = vessel.getAvailableThrust();
        double isp = vessel.getSpecificImpulse() * 9.82;
        double m0 = vessel.getMass();
        double m1 = m0 / Math.exp(deltaV / isp);
        double flowRate = force / isp;
        double burnTime = (m0 - m1) / flowRate;

        // Orientate ship along the burn
        System.out.println("Orientating ship for circularization burn");
        ReferenceFrame frame =
            vessel.getOrbit().getBody().getNonRotatingReferenceFrame();
        vessel.getAutoPilot().setReferenceFrame(frame);
        vessel.getAutoPilot().setTargetDirection(
          node.remainingBurnVector(frame));
        vessel.getAutoPilot().wait_(-1);

        // Wait until burn
        System.out.println("Waiting until circularization burn");
        double burnUt = ut.get() + vessel.getOrbit().getTimeToApoapsis()
          - (burnTime / 2.0);
        double leadTime = 5;
        spaceCenter.warpTo(burnUt - leadTime, 100000, 2);

        // Execute burn
        System.out.println("Ready to execute burn");
        {
          ProcedureCall timeToApoapsis =
            connection.getCall(vessel.getOrbit(), "getTimeToApoapsis");
          Expression expr = e.lessThanOrEqual(
            e.call(timeToApoapsis), e.constantDouble(burnTime / 2.0));
          Event event = connection.addEvent(expr);
          synchronized (event.getCondition()) {
            event.waitFor();
          }
        }
        System.out.println("Executing burn");
        Stream<Triplet<Double, Double, Double>> remainingBurn =
            connection.addStream(node, "remainingBurnVector", frame);
        Stream<Double> remainingDeltaV =
            connection.addStream(node, "getRemainingDeltaV");
        double lowestDeltaV = remainingDeltaV.get();
        while (remainingDeltaV.get() > 0.1) {
            // Steer along the remaining burn, which turns as the vessel
            // moves around its orbit
            vessel.getAutoPilot().setTargetDirection(remainingBurn.get());
            // Full throttle, then throttle down for the last few m/s
            vessel.getControl().setThrottle(
                remainingDeltaV.get() > 5 ? 1.0f : 0.05f);
            // Stop if the burn overshoots, as the remaining delta-v then
            // grows
            if (remainingDeltaV.get() > lowestDeltaV + 0.1) {
                break;
            }
            lowestDeltaV = Math.min(lowestDeltaV, remainingDeltaV.get());
            Thread.sleep(50);
        }
        vessel.getControl().setThrottle(0);
        node.remove();

        System.out.println("Launch complete");
        connection.close();
    }
}
