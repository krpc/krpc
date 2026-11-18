import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;
import org.javatuples.Triplet;

public class LimitAngularVelocity {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      AutoPilot ap = vessel.getAutoPilot();
      // limit the angular velocity by maxAngularVelocity only
      ap.setLimitAngularVelocity(false);
      ap.setMaxAngularVelocity(new Triplet<>(0.3, 0.5, 0.3));
      // [/docs]
    }
  }
}
