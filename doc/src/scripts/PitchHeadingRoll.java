import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;

import org.javatuples.Triplet;

import java.io.IOException;
import java.lang.Math;

public class PitchHeadingRoll {

    static Triplet<Double,Double,Double> crossProduct(Triplet<Double,Double,Double> u, Triplet<Double,Double,Double> v) {
        return new Triplet<Double,Double,Double>(
            u.getValue1() * v.getValue2() - u.getValue2() * v.getValue1(),
            u.getValue2() * v.getValue0() - u.getValue0() * v.getValue2(),
            u.getValue0() * v.getValue1() - u.getValue1() * v.getValue0()
        );
    }

    static double dotProduct(Triplet<Double,Double,Double> u, Triplet<Double,Double,Double> v) {
        return u.getValue0() * v.getValue0() + u.getValue1() * v.getValue1() + u.getValue2() * v.getValue2();
    }

    static double magnitude(Triplet<Double,Double,Double> v) {
        return Math.sqrt(dotProduct(v, v));
    }

    // Compute the angle between vector x and y
    static double angleBetweenVectors(Triplet<Double,Double,Double> u, Triplet<Double,Double,Double> v) {
        double dp = dotProduct(u, v);
        if (dp == 0) {
            return 0;
        }
        double um = magnitude(u);
        double vm = magnitude(v);
        return Math.acos(dp / (um * vm)) * (180f / Math.PI);
    }

    public static void main(String[] args) throws IOException, RPCException, InterruptedException {
        Connection connection = Connection.newInstance();
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

        while (true) {
            Triplet<Double,Double,Double> vesselDirection = vessel.direction(vessel.getSurfaceReferenceFrame());

            // Get the direction of the vessel in the horizon plane
            Triplet<Double,Double,Double> horizonDirection =
                new Triplet<Double,Double,Double>(0.0, vesselDirection.getValue1(), vesselDirection.getValue2());

            // Compute the pitch - the angle between the vessels direction and the direction in the horizon plane
            double pitch = angleBetweenVectors(vesselDirection, horizonDirection);
            if (vesselDirection.getValue0() < 0) {
                pitch = -pitch;
            }

            // Compute the heading - the angle between north and the direction in the horizon plane
            Triplet<Double,Double,Double> north = new Triplet<Double,Double,Double>(0.0,1.0,0.0);
            double heading = angleBetweenVectors(north, horizonDirection);
            if (horizonDirection.getValue2() < 0) {
                heading = 360 - heading;
            }

            // Compute the roll
            // Compute the plane running through the vessels direction and the upwards direction
            Triplet<Double,Double,Double> up = new Triplet<Double,Double,Double>(1.0,0.0,0.0);
            Triplet<Double,Double,Double> planeNormal = crossProduct(vesselDirection, up);
            // Compute the upwards direction of the vessel
            Triplet<Double,Double,Double> vesselUp = spaceCenter.transformDirection(
                new Triplet<Double,Double,Double>(0.0,0.0,-1.0),
                vessel.getReferenceFrame(), vessel.getSurfaceReferenceFrame());
            // Compute the angle between the upwards direction of the vessel and the plane normal
            double roll = angleBetweenVectors(vesselUp, planeNormal);
            // Adjust so that the angle is between -180 and 180 and
            // rolling right is +ve and left is -ve
            if (vesselUp.getValue0() > 0) {
                roll *= -1;
            } else if (roll < 0) {
                roll += 180;
            } else {
                roll -= 180;
            }

            System.out.printf("pitch = " + pitch + ", heading = " + heading + ", roll = " + roll);

            Thread.sleep(1000);
        }
    }
}
