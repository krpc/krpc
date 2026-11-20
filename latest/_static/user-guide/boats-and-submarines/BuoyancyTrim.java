import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;

import org.javatuples.Triplet;

public class BuoyancyTrim {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance("Buoyancy Trim")) {
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      CelestialBody body = vessel.getOrbit().getBody();

      if (!body.getHasOcean()) {
        throw new IllegalStateException(body.getName() + " has no ocean");
      }

      Flight flight = vessel.flight(vessel.getSurfaceReferenceFrame());

      // The fully submerged figures, which do not depend on where the
      // vessel is
      double density = body.getOceanDensity();
      double gravity = body.getSurfaceGravity();
      double buoyancy = flight.buoyantForceAt(density, gravity);
      Triplet<Double, Double, Double> torque =
          flight.buoyantTorqueAt(density, gravity);
      double weight = vessel.getMass() * gravity;
      double torqueSize = Math.sqrt(
          torque.getValue0() * torque.getValue0()
          + torque.getValue1() * torque.getValue1()
          + torque.getValue2() * torque.getValue2());

      System.out.printf("Displacement:  %.1f m^3%n",
                        flight.getDisplacement());
      System.out.printf("Buoyancy:      %.0f N fully submerged%n",
                        buoyancy);
      System.out.printf("Weight:        %.0f N%n", weight);
      System.out.printf("Floats:        %s%n", buoyancy > weight);
      System.out.printf("Torque:        %.0f N m fully submerged%n",
                        torqueSize);

      // The trim, read in the surface reference frame. Its x axis points
      // up, so the y and z axes span the horizontal. A horizontal offset
      // between the two centers tips the hull until they line up again.
      Triplet<Double, Double, Double> centerOfBuoyancy =
          flight.getCenterOfBuoyancy();

      if (centerOfBuoyancy == null) {
        System.out.println("Trim:          vessel is out of the water");
      } else {
        Triplet<Double, Double, Double> centerOfMass =
            flight.getCenterOfMass();
        double north = centerOfBuoyancy.getValue1()
            - centerOfMass.getValue1();
        double east = centerOfBuoyancy.getValue2()
            - centerOfMass.getValue2();
        System.out.printf("Submerged:     %.0f%%%n",
                          100 * flight.getSubmergedPortion());
        System.out.printf("Trim:          %.2f m horizontally%n",
                          Math.hypot(north, east));
        System.out.printf("Trim north:    %+.2f m%n", north);
        System.out.printf("Trim east:     %+.2f m%n", east);
      }
    }
  }
}
