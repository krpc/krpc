import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.RateFilterMode;
import krpc.client.services.SpaceCenter.Vessel;

public class RateFilter {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      AutoPilot ap = vessel.getAutoPilot();
      // notch a known 1.4 Hz pitch and yaw bending mode from the start
      ap.setPitchYawRateFilterMode(RateFilterMode.NOTCH);
      ap.setPitchYawOscillationFrequency(1.4);
      // [/docs]
    }
  }
}
