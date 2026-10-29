import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

import java.io.IOException;

public class RemoteObjects {
  public static void main(String[] args)
      throws IOException, RPCException, InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      Flight flightInfo = vessel.flight(null);

      // Each read gets the current altitude from the game
      System.out.println(flightInfo.getMeanAltitude());
      Thread.sleep(1000);
      System.out.println(flightInfo.getMeanAltitude());

      // Two objects for the same vessel are equal
      System.out.println(vessel.equals(spaceCenter.getActiveVessel()));
    }
  }
}
