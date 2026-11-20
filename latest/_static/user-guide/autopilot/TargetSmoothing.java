import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;

public class TargetSmoothing {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      AutoPilot ap = vessel.getAutoPilot();
      ap.setReferenceFrame(vessel.getSurfaceReferenceFrame());
      ap.targetPitchAndHeading(90, 90); // start pointing straight up
      ap.setEngaged(true);
      ap.wait_(-1);

      // ease a 10 degree pitch-over, 90 to 80, in over 20 seconds
      ap.setTargetSmoothingTime(20);
      ap.targetPitchAndHeading(80, 90);
      ap.wait_(-1); // returns once the 80 degree target is reached
      // [/docs]
    }
  }
}
