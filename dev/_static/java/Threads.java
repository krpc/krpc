import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

import java.io.IOException;

public class Threads {
  public static void main(String[] args)
      throws IOException, RPCException, InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      Flight flightInfo = vessel.flight(vessel.getOrbit().getBody().getReferenceFrame());

      // Both threads make calls over the same connection
      Thread altitude = new Thread(() -> {
        try {
          for (int i = 0; i < 10; i++) {
            System.out.println("Altitude: " + flightInfo.getMeanAltitude());
          }
        } catch (RPCException exn) {
          exn.printStackTrace();
        }
      });
      Thread speed = new Thread(() -> {
        try {
          for (int i = 0; i < 10; i++) {
            System.out.println("Speed: " + flightInfo.getSpeed());
          }
        } catch (RPCException exn) {
          exn.printStackTrace();
        }
      });
      altitude.start();
      speed.start();
      altitude.join();
      speed.join();
    }
  }
}
