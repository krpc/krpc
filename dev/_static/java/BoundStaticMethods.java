import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Orbit;
import krpc.client.services.SpaceCenter.ReferenceFrame;

import java.io.IOException;

public class BoundStaticMethods {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newInstance()) {
      ReferenceFrame frame =
          SpaceCenter.newInstance(connection).getActiveVessel().getReferenceFrame();

      // Bind the static methods to the connection once
      Orbit.Bound orbit = Orbit.bind(connection);
      System.out.println(orbit.referencePlaneNormal(frame));
      System.out.println(orbit.referencePlaneDirection(frame));
    }
  }
}
