import krpc.client.Connection;
import krpc.client.services.SpaceCenter;


public class Errors {
  public static void main(String[] args) {
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
