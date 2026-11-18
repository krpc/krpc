import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;

public class TargetPitchAndHeading {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      AutoPilot ap = vessel.getAutoPilot();
      ap.setReferenceFrame(vessel.getSurfaceReferenceFrame());
      // 80 degrees above the horizon, facing east
      ap.targetPitchAndHeading(80, 90);
      ap.setEngaged(true);
      ap.wait_(-1); // blocks until the vessel has settled on the target
      ap.setEngaged(false);
      // [/docs]
    }
  }
}
