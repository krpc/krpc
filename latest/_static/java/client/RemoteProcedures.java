import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

import java.io.IOException;

public class RemoteProcedures {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newInstance("Vessel Name")) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      System.out.println(vessel.getName());
    }
  }
}
