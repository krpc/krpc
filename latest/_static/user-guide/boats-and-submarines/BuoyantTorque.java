import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

public class BuoyantTorque {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      CelestialBody body = vessel.getOrbit().getBody();
      Flight flight = vessel.flight(vessel.getSurfaceReferenceFrame());
      // [docs]
      Triplet<Double, Double, Double> torque = flight.buoyantTorqueAt(
          body.getOceanDensity(), body.getSurfaceGravity());
      // [/docs]
      System.out.printf("Torque = (%.0f, %.0f, %.0f) N m%n",
          torque.getValue0(), torque.getValue1(), torque.getValue2());
    }
  }
}
