// [docs]
import krpc.client.Connection;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

public class PitchHold {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance("pitch hold")) {
      KRPC krpc = KRPC.newInstance(connection);
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      float targetPitch = 45;

      try {
        for (int i = 0; i < 500; i++) {
          krpc.nextTick();
          Flight flight = vessel.flight(null);
          float error = targetPitch - flight.getPitch();
          vessel.getControl().setPitch(
              Math.max(-1, Math.min(1, 0.05f * error)));
        }
      } finally {
        krpc.releaseTick();
        vessel.getControl().setPitch(0);
      }
    }
  }
}
// [/docs]
