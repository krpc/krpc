import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

public class InfoUi {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      vessel.getAutoPilot().setShowInfoUI(true);
      // [/docs]
    }
  }
}
