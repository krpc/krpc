import krpc.client.Connection;
import krpc.client.services.KRPC;


public class ConnectingLocal {
  public static void main(String[] args) {
    try (Connection connection = Connection.newLocalInstance("My Example Program")) {
      System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    }
  }
}
