import krpc.client.Connection;
import krpc.client.Stream;
import krpc.client.services.Drawing;
import krpc.client.services.Drawing.Line;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.DockingPort;
import krpc.client.services.SpaceCenter.DockingPortState;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.client.services.StdLib;

import org.javatuples.Triplet;

public class Docking {
    public static void main(String[] args) throws InterruptedException {
        Connection connection = Connection.newInstance("Docking");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Drawing drawing = Drawing.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        DockingPort port =
            vessel.getParts().getControlling().getDockingPort();
        DockingPort target = spaceCenter.getTargetDockingPort();
        ReferenceFrame frame = target.getReferenceFrame();

        // Turn to face the target port
        System.out.println("Turning to face the target");
        vessel.getControl().setSAS(false);
        vessel.getControl().setRCS(true);
        vessel.getAutoPilot().setReferenceFrame(frame);
        vessel.getAutoPilot().setTargetDirection(
            new Triplet<Double, Double, Double>(0.0, -1.0, 0.0));
        vessel.getAutoPilot().setEngaged(true);
        vessel.getAutoPilot().wait_(-1);

        // Draw the docking axis, and a line from our port to the target
        Triplet<Double, Double, Double> origin =
            new Triplet<Double, Double, Double>(0.0, 0.0, 0.0);
        Line axis = drawing.addLine(
            origin, new Triplet<Double, Double, Double>(0.0, 50.0, 0.0),
            frame, true);
        axis.setColor(new StdLib.ColorRGB(0f, 1f, 0f));
        Line approach = drawing.addLine(origin, origin, frame, true);
        approach.setColor(new StdLib.ColorRGB(1f, 1f, 0f));

        // Approach the target port along its axis
        System.out.println("Approaching");
        Stream<Triplet<Double, Double, Double>> position =
            connection.addStream(port, "position", frame);
        Stream<Triplet<Double, Double, Double>> velocity =
            connection.addStream(vessel, "velocity", frame);
        while (port.getState() != DockingPortState.DOCKING &&
               port.getState() != DockingPortState.DOCKED) {
            Triplet<Double, Double, Double> p = position.get();
            approach.setStart(p);
            double x = p.getValue0(), y = p.getValue1(), z = p.getValue2();

            // Line up on the axis, then close in along it
            double offAxis = Math.hypot(x, z);
            double goalY = offAxis < 0.5 ? 0 : Math.max(y, 5);
            double ex = -x, ey = goalY - y, ez = -z;
            double distance = Math.sqrt(ex * ex + ey * ey + ez * ez);
            double speed = Math.min(1, Math.max(0.1, 0.2 * distance));
            double scale = speed / Math.max(distance, 0.01);

            // Thrust towards the velocity we want, in the vessel's frame
            Triplet<Double, Double, Double> v = velocity.get();
            Triplet<Double, Double, Double> thrust =
                new Triplet<Double, Double, Double>(
                    2 * (ex * scale - v.getValue0()),
                    2 * (ey * scale - v.getValue1()),
                    2 * (ez * scale - v.getValue2()));
            Triplet<Double, Double, Double> t =
                spaceCenter.transformDirection(
                    thrust, frame, vessel.getReferenceFrame());
            vessel.getControl().setRight(
                (float) Math.max(-1, Math.min(1, t.getValue0())));
            vessel.getControl().setForward(
                (float) Math.max(-1, Math.min(1, t.getValue1())));
            vessel.getControl().setUp(
                (float) Math.max(-1, Math.min(1, -t.getValue2())));
            Thread.sleep(50);
        }

        // Stop the thrusters, and remove the drawing
        vessel.getControl().setRight(0);
        vessel.getControl().setForward(0);
        vessel.getControl().setUp(0);
        vessel.getAutoPilot().setEngaged(false);
        drawing.clear(false);
        System.out.println("Docked");
        connection.close();
    }
}
