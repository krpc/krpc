import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.Stream;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.MechJeb;
import krpc.client.services.MechJeb.AscentGuidance;
import krpc.client.services.MechJeb.AscentSettings;
import krpc.client.services.MechJeb.AscentType;
import krpc.client.services.MechJeb.LandingGuidance;
import krpc.client.services.MechJeb.ManeuverPlanner;
import krpc.client.services.MechJeb.OperationCircularize;
import krpc.client.services.MechJeb.OperationCourseCorrection;
import krpc.client.services.MechJeb.OperationPlane;
import krpc.client.services.MechJeb.OperationTransfer;
import krpc.client.services.MechJeb.TargetController;
import krpc.client.services.MechJeb.TimeReference;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.Engine;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Orbit;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.Vessel;

public class LaunchToMinmus {
    static Connection connection;
    static Vessel vessel;

    /** Wait until the condition is true. */
    static void waitFor(Expression condition) {
        Event event = connection.addEvent(condition);
        synchronized (event.getCondition()) {
            event.waitFor();
        }
    }

    /** Print each step of the autopilot, from a stream of its status. */
    static void printSteps(Stream<String> status) {
        String[] last = {""};
        status.addCallback(value -> {
            // The step, without the figures that change on each update
            String line = value.split("\n", -1)[0];
            String step = line.split("[0-9∞]", -1)[0];
            step = step.replaceAll("[ :=]+$", "");
            if (!step.isEmpty() && !step.equals(last[0])) {
                last[0] = step;
                System.out.println(step);
            }
        });
    }

    /** Execute the vessel's maneuver nodes, and wait for the burns. */
    static void executeNodes(ManeuverPlanner planner) {
        planner.executeAllNodes();
        Expression.Bound e = Expression.bind(connection);
        waitFor(e.not(
            e.call(connection.getCall(planner, "getExecutingNode"))));
    }

    static void printOrbit() {
        Orbit orbit = vessel.getOrbit();
        System.out.printf(
            "Orbit of %s, %.0f km by %.0f km%n", orbit.getBody().getName(),
            orbit.getApoapsisAltitude() / 1000,
            orbit.getPeriapsisAltitude() / 1000);
    }

    public static void main(String[] args) {
        connection = Connection.newInstance("Launch to Minmus");
        SpaceCenter sc = SpaceCenter.newInstance(connection);
        vessel = sc.getActiveVessel();
        MechJeb mj = MechJeb.newInstance(connection);
        Expression.Bound e = Expression.bind(connection);

        // The target orbit, set as in the Ascent Guidance window
        AscentGuidance ascent = mj.getAscentGuidance();
        ascent.setAscentType(AscentType.CLASSIC);
        ascent.setDesiredOrbitAltitude(100000);
        ascent.setDesiredInclination(0);

        // The Ascent Settings window
        AscentSettings settings = ascent.getSettings();
        settings.setAutostage(true);
        settings.setLimitAngleOfAttack(true);
        settings.setMaxAngleOfAttack(5);
        settings.setAutowarp(true);

        // Print each step of the autopilot
        Stream<String> status = connection.addStream(ascent, "getStatus");
        printSteps(status);

        ascent.setEnabled(true);
        vessel.getControl().activateNextStage();

        // Wait for the autopilot to disengage, once the orbit is circular
        waitFor(e.not(e.call(connection.getCall(ascent, "getEnabled"))));
        status.remove();
        printOrbit();

        ManeuverPlanner planner = mj.getManeuverPlanner();
        planner.setAutowarp(true);

        // Stage once a stage burns out, during the burns too
        mj.getUtilities().getStaging().setEnabled(true);

        // Target Minmus, and wait for MechJeb to see the target
        CelestialBody minmus = sc.getBodies().get("Minmus");
        sc.setTargetBody(minmus);
        TargetController targetController = mj.getTargetController();
        waitFor(e.call(
            connection.getCall(targetController, "getNormalTargetExists")));

        // Match planes with Minmus, at the nearest relative node
        OperationPlane plane = planner.getOperationPlane();
        plane.getTimeSelector().setTimeReference(
            TimeReference.REL_NEAREST_AD);
        plane.makeNodes();
        executeNodes(planner);

        // The Hohmann transfer, to an encounter with no capture burn
        OperationTransfer transfer = planner.getOperationTransfer();
        transfer.getTimeSelector().setTimeReference(TimeReference.COMPUTED);
        transfer.setCapture(false);
        transfer.makeNodes();
        executeNodes(planner);

        // Fine tune the encounter, for a periapsis 20 km above Minmus
        OperationCourseCorrection correction =
            planner.getOperationCourseCorrection();
        correction.setFinalPeriapsisAltitude(20000);
        correction.makeNodes();
        executeNodes(planner);

        // Warp into Minmus' sphere of influence
        sc.warpTo(
            sc.getUT() + vessel.getOrbit().getTimeToSOIChange() + 60,
            100000, 2);

        // Circularize at the periapsis
        OperationCircularize circularize =
            planner.getOperationCircularize();
        circularize.getTimeSelector().setTimeReference(
            TimeReference.PERIAPSIS);
        circularize.makeNodes();
        executeNodes(planner);

        sc.clearTarget();
        printOrbit();

        // Stage until the next stage would drop the landing legs. This
        // drops the stages below the lander, and activates the lander's
        // engine.
        mj.getUtilities().getStaging().setEnabled(false);
        Part legs = vessel.getParts().getLegs().get(0).getPart();
        while (vessel.getControl().getCurrentStage()
               > legs.getDecoupleStage() + 1) {
            vessel.getControl().activateNextStage();
        }

        // Limit the lander's thrust, so its lowest throttle leaves it
        // falling
        for (Engine engine : vessel.getParts().getEngines()) {
            engine.setThrustLimit(0.3f);
        }

        // Land on Minmus' Great Flats, and wait for MechJeb to see the
        // target
        LandingGuidance landing = mj.getLandingGuidance();
        landing.setTarget(minmus, 0, -6);
        waitFor(e.call(connection.getCall(
            targetController, "getPositionTargetExists")));

        // The settings of the Landing Guidance window
        landing.setTouchdownSpeed(1);
        landing.setDeployGears(true);
        landing.setAutowarp(true);

        // Print each step of the autopilot
        status = connection.addStream(landing, "getStatus");
        printSteps(status);

        landing.landAtTarget();

        // Wait for the autopilot to disengage, once the vessel has landed
        waitFor(e.not(e.call(connection.getCall(landing, "getEnabled"))));
        status.remove();

        sc.clearTarget();
        Flight flight = vessel.flight(null);
        System.out.printf(
            "Landed at latitude %.2f, longitude %.2f%n",
            flight.getLatitude(), flight.getLongitude());
        connection.close();
    }
}
