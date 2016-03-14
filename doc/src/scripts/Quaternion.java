import java.io.IOException;
import org.javatuples.Quartet;
import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

public class Quaternion {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance();
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        Quartet<Double, Double, Double, Double> q = vessel.flight(null).getRotation();
        System.out.println(q.getValue0() + "," + q.getValue1() + "," + q.getValue2() + "," + q.getValue3());
    }
}
