import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.CelestialBody;
import krpc.client.services.SpaceCenter.EditorVessel;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.SimulatedFlight;

import org.javatuples.Quartet;
import org.javatuples.Triplet;

public class EditorTorque {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      CelestialBody body = spaceCenter.getBodies().get("Kerbin");
      // [docs]
      EditorVessel vessel = spaceCenter.getEditor().getVessel();
      SimulatedFlight flight = vessel.flight(vessel.getReferenceFrame());
      // The vessel frame's z axis points out of the bottom, so up is its
      // -z axis
      double side = Math.sqrt(0.5);
      Triplet<Double, Double, Double> zero = new Triplet<>(0.0, 0.0, 0.0);
      flight.setSurfaceReferenceFrame(ReferenceFrame.createRelative(
          connection, vessel.getReferenceFrame(), zero,
          new Quartet<>(0.0, side, 0.0, side), zero, zero));
      Triplet<Double, Double, Double> torque = flight.buoyantTorqueAt(
          body.getOceanDensity(), body.getSurfaceGravity());
      // [/docs]
      System.out.printf("Torque = (%.0f, %.0f, %.0f) N m%n",
          torque.getValue0(), torque.getValue1(), torque.getValue2());
    }
  }
}
