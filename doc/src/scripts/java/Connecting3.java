import krpc.client.Connection;
import krpc.client.services.KRPC;


public class Connecting3 {
  public static void main(String[] args) {
    Connection connection = Connection.newInstance("My Example Program");
    System.out.println(KRPC.newInstance(connection).getStatus().getVersion());
    connection.close();
  }
}
