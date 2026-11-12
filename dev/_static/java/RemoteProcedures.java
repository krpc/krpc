import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;


public class RemoteProcedures {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();

      vessel.setName("My Vessel");

      Flight flightInfo = vessel.flight(null);
      System.out.println(flightInfo.getMeanAltitude());
    }
  }
}
