import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

import java.io.IOException;

public class Streaming2 {
    public static void main(String[] args) throws IOException, RPCException, StreamException {
        Connection connection = Connection.newInstance();
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        Vessel vessel = spaceCenter.getActiveVessel();
        ReferenceFrame refframe = vessel.getOrbit().getBody().getReferenceFrame();
        Stream<Triplet<Double,Double,Double>> vesselStream = connection.addStream(vessel, "position", refframe);
        while (true) {
            System.out.println(vesselStream.get());
        }
    }
}
