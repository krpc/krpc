import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

public class BuoyantForce {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      // [docs]
      CelestialBody body = vessel.getOrbit().getBody();
      Flight flight = vessel.flight(null);
      double displacement = flight.getDisplacement();
      double buoyancy = flight.buoyantForceAt(
          body.getOceanDensity(), body.getSurfaceGravity());
      // [/docs]
      System.out.printf("Displacement = %.1f m^3, buoyancy = %.0f N%n",
          displacement, buoyancy);
    }
  }
}
