import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Flight;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;

import java.io.IOException;

public class StructureField {
  public static void main(String[] args) throws IOException, RPCException, StreamException {
    try (Connection connection = Connection.newInstance()) {
      SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
      Vessel vessel = spaceCenter.getActiveVessel();
      ProcedureCall siteInfo = connection.getCall(vessel.getComms().getControlPath().get(0), "getStart");
      // [docs]
      // The Name field of a structure a call returns
      Expression expr = Expression.getField(
          connection, Expression.call(connection, siteInfo), "Name");
      // [/docs]
    }
  }
}
