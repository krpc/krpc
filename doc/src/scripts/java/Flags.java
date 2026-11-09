import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.MapFilterType;

import java.io.IOException;

public class Flags {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);

      // Show only probes and ships in map mode
      spaceCenter.setMapFilter(MapFilterType.PROBES.or(MapFilterType.SHIPS));

      // Check whether map mode shows debris
      MapFilterType filter = spaceCenter.getMapFilter();
      System.out.println(filter.contains(MapFilterType.DEBRIS));

      // Show debris as well
      spaceCenter.setMapFilter(filter.or(MapFilterType.DEBRIS));
    }
  }
}
