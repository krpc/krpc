import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Quartet;

public class Quaternion {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      Quartet<Double, Double, Double, Double> q =
          vessel.flight(null).getRotation();
      System.out.printf("(%.2f, %.2f, %.2f, %.2f)%n", q.getValue0(),
          q.getValue1(), q.getValue2(), q.getValue3());
    }
  }
}
