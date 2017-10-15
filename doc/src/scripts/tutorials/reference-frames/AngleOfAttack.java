import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;

import org.javatuples.Triplet;

import java.io.IOException;
import java.lang.Math;

public class AngleOfAttack {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException {
        Connection connection = Connection.newInstance("Angle of attack");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        SpaceCenter.Vessel vessel = spaceCenter.getActiveVessel();

        while (true) {
            Triplet<Double,Double,Double> d =
                vessel.direction(vessel.getOrbit().getBody().getReferenceFrame());
            Triplet<Double,Double,Double> v =
                vessel.velocity(vessel.getOrbit().getBody().getReferenceFrame());

            // Compute the dot product of d and v
            double dotProd =
                  d.getValue0() * v.getValue0()
                + d.getValue1() * v.getValue1()
                + d.getValue2() * v.getValue2();

            // Compute the magnitude of v
            double vMag = Math.sqrt(
                  v.getValue0() * v.getValue0()
                + v.getValue1() * v.getValue1()
                + v.getValue2() * v.getValue2()
            );
            // Note: don't need to magnitude of d as it is a unit vector

            // Compute the angle between the vectors
            double angle = 0;
            if (dotProd > 0) {
                angle = Math.abs(Math.acos(dotProd / vMag) * (180.0 / Math.PI));
            }

            System.out.printf("Angle of attack = %.1f degrees\n", angle);

            Thread.sleep(1000);
        }
    }
}
