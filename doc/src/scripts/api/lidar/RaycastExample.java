import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.ReferenceFrame;

import org.javatuples.Triplet;

import java.io.IOException;

public class RaycastExample {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Raycast Example");
        SpaceCenter sc = SpaceCenter.newInstance(connection);
        Part part = sc.getActiveVessel().getParts().getRoot();
        ReferenceFrame frame = part.getReferenceFrame();

        // A fan of 8 rays below the part, 10 degrees apart
        Triplet<Double, Double, Double> origin = new Triplet<Double, Double, Double>(0.0, 0.0, 0.0);
        for (int i = 0; i < 8; i++) {
            double angle = Math.toRadians(10 * i - 35);
            Triplet<Double, Double, Double> direction =
                new Triplet<Double, Double, Double>(Math.sin(angle), -Math.cos(angle), 0.0);
            System.out.println(sc.raycastDistance(origin, direction, frame));
        }
        connection.close();
    }
}
