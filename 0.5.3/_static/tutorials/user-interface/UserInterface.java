import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.client.services.UI;
import krpc.client.services.UI.Button;
import krpc.client.services.UI.Canvas;
import krpc.client.services.UI.Panel;
import krpc.client.services.UI.RectTransform;
import krpc.client.services.UI.Text;

import org.javatuples.Pair;
import org.javatuples.Triplet;

import java.io.IOException;

public class UserInterface {
    public static void main(String[] args)
        throws IOException, RPCException, InterruptedException, StreamException {
        Connection connection = Connection.newInstance("User Interface Example");
        SpaceCenter spaceCenter = SpaceCenter.newInstance(connection);
        UI ui = UI.newInstance(connection);
        Canvas canvas = ui.getStockCanvas();

        // Get the size of the game window in pixels
        Pair<Double, Double> screenSize = canvas.getRectTransform().getSize();

        // Add a panel to contain the UI elements
        Panel panel = canvas.addPanel(true);

        // Position the panel on the left of the screen
        RectTransform rect = panel.getRectTransform();
        rect.setSize(new Pair<Double,Double>(200.0, 100.0));
        rect.setPosition(
          new Pair<Double,Double>((110-(screenSize.getValue0())/2), 0.0));

        // Add a button to set the throttle to maximum
        Button button = panel.addButton("Full Throttle", true);
        button.getRectTransform().setPosition(new Pair<Double,Double>(0.0, 20.0));

        // Add some text displaying the total engine thrust
        Text text = panel.addText("Thrust: 0 kN", true);
        text.getRectTransform().setPosition(new Pair<Double,Double>(0.0, -20.0));
        text.setColor(new Triplet<Double,Double,Double>(1.0, 1.0, 1.0));
        text.setSize(18);

        // Set up a stream to monitor the throttle button
        Stream<Boolean> buttonClicked = connection.addStream(button, "getClicked");

        Vessel vessel = spaceCenter.getActiveVessel();
        while (true) {
            // Handle the throttle button being clicked
            if (buttonClicked.get ()) {
                vessel.getControl().setThrottle(1);
                button.setClicked(false);
            }

            // Update the thrust text
            text.setContent(String.format("Thrust: %.0f kN", (vessel.getThrust()/1000)));

            Thread.sleep(1000);
        }
    }
}
