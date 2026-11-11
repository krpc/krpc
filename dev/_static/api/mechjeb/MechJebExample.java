import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.RPCException;
import krpc.client.StreamException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.MechJeb;
import krpc.client.services.MechJeb.AscentAutopilot;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.schema.KRPC.ProcedureCall;

import java.io.IOException;

public class MechJebExample {
    public static void main(String[] args) throws IOException, RPCException, StreamException {
        Connection connection = Connection.newInstance("MechJeb Example");
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        AscentAutopilot ascent = MechJeb.newInstance(connection).getAscentAutopilot();

        ascent.setDesiredOrbitAltitude(100000);
        ascent.setDesiredInclination(6);
        ascent.setAutostage(true);
        ascent.setEnabled(true);

        vessel.getControl().activateNextStage();

        ProcedureCall enabled = connection.getCall(ascent, "getEnabled");
        Expression.Bound e = Expression.bind(connection);
        Expression expr = e.equal(e.call(enabled), e.constantBool(false));
        Event event = connection.addEvent(expr);
        synchronized (event.getCondition()) {
            event.waitFor();
        }

        System.out.println("Launch complete");
        connection.close();
    }
}
