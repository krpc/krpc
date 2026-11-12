import krpc.client.Connection;
import krpc.client.services.KRPC;


public class Connecting1 {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    }
  }
}
