import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

public class Vector3 {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      Triplet<Double, Double, Double> v = vessel.flight(null).getPrograde();
      System.out.printf("(%.2f, %.2f, %.2f)%n",
          v.getValue0(), v.getValue1(), v.getValue2());
    }
  }
}
