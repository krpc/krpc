import krpc.client.Connection;
import krpc.client.services.KRPC;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.AutoPilotUpdateMode;
import krpc.client.services.SpaceCenter.Vessel;

public class ManualUpdate {
  static float controlLaw(float pitch) {
    return pitch + 0.05f * (45 - pitch);
  }

  static void log(float error, float pitch) {
    System.out.println(error + " " + pitch);
  }

  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      KRPC krpc = KRPC.newInstance(connection);
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      AutoPilot ap = vessel.getAutoPilot();
      ap.setEngaged(true);
      // [docs]
      ap.setUpdateMode(AutoPilotUpdateMode.MANUAL);
      while (true) {
        krpc.holdTick();
        try {
          ap.targetPitchAndHeading(
              controlLaw(vessel.flight(null).getPitch()), 90);
          ap.update();
          log(ap.getError(), vessel.getControl().getPitch());
        } finally {
          krpc.releaseTick();
        }
      }
      // [/docs]
    }
  }
}
