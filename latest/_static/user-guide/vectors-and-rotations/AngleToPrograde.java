import krpc.client.Connection;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.ReferenceFrame;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.client.services.StdLib;

import org.javatuples.Triplet;

public class AngleToPrograde {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      StdLib stdLib = StdLib.newInstance(connection);
      Vessel vessel =
          SpaceCenter.newInstance(connection).getActiveVessel();
      ReferenceFrame frame =
          vessel.getOrbit().getBody().getNonRotatingReferenceFrame();

      // [docs]
      Triplet<Double, Double, Double> direction = vessel.direction(frame);
      Triplet<Double, Double, Double> prograde =
          vessel.flight(frame).getPrograde();
      double angle = stdLib.vectorAngle(direction, prograde);
      System.out.println(stdLib.radiansToDegrees(angle));
      // [/docs]
    }
  }
}
