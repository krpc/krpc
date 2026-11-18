import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

public class CenterOfBuoyancy {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      Flight flight = vessel.flight(vessel.getSurfaceReferenceFrame());
      Triplet<Double, Double, Double> centerOfBuoyancy =
          flight.getCenterOfBuoyancy();
      Triplet<Double, Double, Double> centerOfMass =
          flight.getCenterOfMass();
      // [/docs]
      if (centerOfBuoyancy != null) {
        System.out.printf("Trim north = %.2f m, east = %.2f m%n",
            centerOfBuoyancy.getValue1() - centerOfMass.getValue1(),
            centerOfBuoyancy.getValue2() - centerOfMass.getValue2());
      }
    }
  }
}
