import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.Stream;
import krpc.client.StreamException;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Vessel;
import krpc.client.services.UI;
import krpc.client.services.UI.Button;
import krpc.client.services.UI.Canvas;
import krpc.client.services.UI.Layout;
import krpc.client.services.UI.Panel;
import krpc.client.services.UI.RectTransform;
import krpc.client.services.UI.Slider;
import krpc.client.services.UI.Text;

import org.javatuples.Pair;
import org.javatuples.Quartet;

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

        // Add a panel to contain the UI elements. The user can drag it around
        // the screen with the mouse.
        Panel panel = canvas.addPanel(true);
        panel.setDraggable(true);

        // Position the panel on the left of the screen
        RectTransform rect = panel.getRectTransform();
        rect.setSize(new Pair<Double,Double>(200.0, 120.0));
        rect.setPosition(
          new Pair<Double,Double>((110-(screenSize.getValue0())/2), 0.0));

        // Lay the contents of the panel out in a column
        Layout layout = panel.addVerticalLayout();
        layout.setPadding(new Quartet<Integer,Integer,Integer,Integer>(10, 10, 10, 10));
        layout.setSpacing(6);

        // Add a button to set the throttle to maximum, with a tooltip shown
        // while the mouse rests on it
        Button button = panel.addButton("Full Throttle", true);
        button.setTooltip("Set the throttle to maximum");
        button.getLayoutElement().setPreferredSize(new Pair<Double,Double>(-1.0, 30.0));

        // Add a slider to set the throttle by hand
        Slider slider = panel.addSlider(false, true);
        slider.getLayoutElement().setPreferredSize(new Pair<Double,Double>(-1.0, 20.0));

        // Add some text displaying the total engine thrust
        Text text = panel.addText("Thrust: 0 kN", true);
        text.setColor(new Quartet<Double,Double,Double,Double>(1.0, 1.0, 1.0, 1.0));
        text.setSize(18);

        // Set up streams to monitor the button and the slider
        Stream<Boolean> buttonClicked = connection.addStream(button, "getClicked");
        Stream<Boolean> sliderChanged = connection.addStream(slider, "getChanged");

        Vessel vessel = spaceCenter.getActiveVessel();
        while (true) {
            // Handle the throttle button being clicked
            if (buttonClicked.get ()) {
                vessel.getControl().setThrottle(1);
                slider.setValue(1);
                button.setClicked(false);
            }

            // Handle the user moving the throttle slider
            if (sliderChanged.get ()) {
                vessel.getControl().setThrottle(slider.getValue());
                slider.setChanged(false);
            }

            // Update the thrust text
            text.setContent(String.format("Thrust: %.0f kN", (vessel.getThrust()/1000)));

            Thread.sleep(1000);
        }
    }
}
