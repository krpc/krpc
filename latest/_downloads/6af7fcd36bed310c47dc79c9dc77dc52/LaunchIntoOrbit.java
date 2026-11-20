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

public class LaunchIntoOrbit {
    public static void main(String[] args) throws InterruptedException {
        Connection connection = Connection.newInstance("Launch into orbit");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

        float turnStartAltitude = 250;
        float turnEndAltitude = 45000;
        float targetAltitude = 150000;

        // Set up streams for telemetry
        Stream<Double> ut =
            connection.addStream(SpaceCenter.class, "getUT");
        Flight flight = vessel.flight(null);
        Stream<Double> altitude =
            connection.addStream(flight, "getMeanAltitude");
        Stream<Double> apoapsis =
            connection.addStream(vessel.getOrbit(), "getApoapsisAltitude");
        Resources stage5Resources =
            vessel.decoupleStageAt(5).resources(false);
        Resources stage3Resources =
            vessel.decoupleStageAt(3).resources(false);

        // Pre-launch setup
        vessel.getControl().setSAS(false);
        vessel.getControl().setRCS(false);
        vessel.getControl().setThrottle(1);

        // Activate the first stage
        vessel.getAutoPilot().setEngaged(true);
        vessel.getAutoPilot().targetPitchAndHeading(90, 90);
        vessel.getControl().activateNextStage();

        // Separate each stage when it runs out of fuel
        Expression.Bound e = Expression.bind(connection);
        Resources[] stages = {stage5Resources, stage3Resources};
        String[] fuels = {"SolidFuel", "LiquidFuel"};
        String[] names = {"First", "Second"};
        for (int i = 0; i < stages.length; i++) {
            String name = names[i];
            ProcedureCall amount =
                connection.getCall(stages[i], "amount", fuels[i]);
            Event event = connection.addEvent(
                e.lessThan(e.call(amount), e.constantFloat(0.1f)));
            event.addCallback(() -> {
                event.remove();
                vessel.getControl().activateNextStage();
                vessel.getControl().activateNextStage();
                System.out.println(name + " stage separated");
            });
        }

        // Main ascent loop
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

            // Stop the ascent near the target apoapsis
            if (apoapsis.get() > targetAltitude * 0.9) {
                System.out.println("Approaching target apoapsis");
                break;
            }

            Thread.sleep(50);
        }

        // Fine tune the apoapsis at 25% thrust
        vessel.getControl().setThrottle(0.25f);
        {
            ProcedureCall apoapsisAltitude = connection.getCall(
                vessel.getOrbit(), "getApoapsisAltitude");
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
