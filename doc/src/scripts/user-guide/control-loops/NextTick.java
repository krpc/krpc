import krpc.client.Connection;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

public class NextTick {
  static float controlLaw(float pitch) {
    return 0.05f * (45 - pitch);
  }

  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      KRPC krpc = KRPC.newInstance(connection);
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      try {
        while (true) {
          krpc.nextTick();
          float pitch = vessel.flight(null).getPitch();
          vessel.getControl().setPitch(controlLaw(pitch));
        }
      } finally {
        krpc.releaseTick();
      }
      // [/docs]
    }
  }
}
