import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Engine;
import krpc.client.services.SpaceCenter.Vessel;

import java.io.IOException;
import java.util.LinkedList;
import java.util.List;

public class CombinedIsp {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance();
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();

        List<Engine> engines = vessel.getParts().getEngines();
        List<Engine> activeEngines = new LinkedList<Engine>();
        for (Engine engine : engines) {
            if (engine.getActive() && engine.getHasFuel()) {
                activeEngines.add(engine);
            }
        }

        System.out.println("Active engines:");
        for (Engine engine : activeEngines) {
            System.out.println("   " + engine.getPart().getTitle() +
                               " in stage " + engine.getPart().getStage());
        }

        double thrust = 0;
        double fuelConsumption = 0;
        for (Engine engine : activeEngines) {
            thrust += engine.getThrust();
            fuelConsumption += engine.getThrust() / engine.getSpecificImpulse();
        }
        double isp = thrust / fuelConsumption;
        System.out.printf("Combined vacuum Isp = %.0f\n", isp);
        connection.close();
    }
}
