import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;

import java.io.IOException;

public class Errors {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      try {
        spaceCenter.load("missing");
      } catch (IllegalArgumentException exn) {
        System.out.println("Failed to load the save: " + exn.getMessage());
      }
    }
  }
}
