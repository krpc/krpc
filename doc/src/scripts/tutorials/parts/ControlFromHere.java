import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Part;
import krpc.client.services.SpaceCenter.Vessel;


public class ControlFromHere {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance();
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        Part part = vessel.getParts().withTitle("Clamp-O-Tron Docking Port").get(0);
        vessel.getParts().setControlling(part);
        connection.close();
    }
}
