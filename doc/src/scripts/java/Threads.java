import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;


public class Threads {
  public static void main(String[] args) throws InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      Flight flightInfo = vessel.flight(vessel.getOrbit().getBody().getReferenceFrame());

      // Both threads make calls over the same connection
      Thread altitude = new Thread(() -> {
        for (int i = 0; i < 10; i++) {
          System.out.println("Altitude: " + flightInfo.getMeanAltitude());
        }
      });
      Thread speed = new Thread(() -> {
        for (int i = 0; i < 10; i++) {
          System.out.println("Speed: " + flightInfo.getSpeed());
        }
      });
      altitude.start();
      speed.start();
      altitude.join();
      speed.join();
    }
  }
}
