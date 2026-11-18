import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.AutoPilot;
import krpc.client.services.SpaceCenter.Vessel;
import org.javatuples.Triplet;

public class DirectionAndUp {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      AutoPilot ap = vessel.getAutoPilot();
      ap.setReferenceFrame(vessel.getSurfaceReferenceFrame());
      // north, in the surface frame, off the flight path
      Triplet<Double, Double, Double> up = new Triplet<>(0.0, 1.0, 0.0);

      // straight up at the pad, roof toward north
      ap.setDirectionAndUp(new Triplet<>(1.0, 0.0, 0.0), up, 0);
      ap.setEngaged(true);
      ap.wait_(-1);

      // ease over to the eastern horizon; roll stays defined through vertical
      for (int i = 1; i <= 90; i++) {
        double pitch = Math.toRadians(90 - i); // 89 down to 0 degrees
        Triplet<Double, Double, Double> direction =
            new Triplet<>(Math.sin(pitch), 0.0, Math.cos(pitch));
        ap.setDirectionAndUp(direction, up, 0);
        ap.wait_(-1);
      }
      // [/docs]
    }
  }
}
