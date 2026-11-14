import krpc.client.Connection;
import krpc.client.services.Drawing;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;
import org.javatuples.Triplet;


public class DrawingExample {
    public static void main(String[] args) throws InterruptedException {
        Connection connection = Connection.newInstance("Drawing Example");
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        Drawing.newInstance(connection).addDirectionFromCom(
            new Triplet<Double, Double, Double>(0.0, 1.0, 0.0),
            vessel.getReferenceFrame(), 10, true);
        Thread.sleep(10000);
        connection.close();
    }
}
