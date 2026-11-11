import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KerbalEngineerRedux;
import krpc.client.services.KerbalEngineerRedux.BurnTime;
import krpc.client.services.KerbalEngineerRedux.Stage;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;

import java.io.IOException;
import java.util.List;

public class KerbalEngineerReduxExample {
    public static void main(String[] args) throws IOException, RPCException {
        Connection connection = Connection.newInstance("Kerbal Engineer Redux Example");
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        KerbalEngineerRedux ker = KerbalEngineerRedux.newInstance(connection);

        List<Stage> stages = ker.stages(vessel);
        for (int i = stages.size() - 1; i >= 0; i--) {
            Stage stage = stages.get(i);
            System.out.printf("Stage %d: %.0f m/s, TWR %.2f\n",
                stage.getNumber(), stage.getDeltaV(), stage.getAvailableTWR());
        }

        BurnTime burn = ker.burnTime(vessel, 500);
        if (burn == null) {
            System.out.println("Not enough delta-v for the burn");
        } else {
            System.out.printf("A 500 m/s burn takes %.1f seconds\n", burn.getDuration());
        }
        connection.close();
    }
}
