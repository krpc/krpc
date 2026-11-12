import krpc.client.Connection;
import krpc.client.Event;
import krpc.client.services.RasterPropMonitor;
import krpc.client.services.RasterPropMonitor.Display;
import krpc.client.services.RasterPropMonitor.Monitor;
import krpc.client.services.SpaceCenter;
import krpc.client.services.SpaceCenter.Control;
import krpc.client.services.SpaceCenter.Vessel;

public class RasterPropMonitorExample {
    public static void main(String[] args) {
        Connection connection = Connection.newInstance("Raster Prop Monitor Example");
        Vessel vessel = SpaceCenter.newInstance(connection).getActiveVessel();
        Control control = vessel.getControl();
        RasterPropMonitor rpm = RasterPropMonitor.newInstance(connection);

        // The first screen in the IVA, and the content of its krpc page
        Monitor monitor = rpm.monitors(vessel).get(0);
        Display display = monitor.getDisplay();

        // Raster Prop Monitor formats the SAS variable, and keeps it up to date
        display.setText("SAS: {0:ON;OFF;OFF} $&$ SAS");

        // A press of the NAV button shows the page
        monitor.captureButton("buttonR1", "krpc");

        // Each press of the ENTER button toggles SAS
        int enter = monitor.getButtons().indexOf("button_ENTER");
        Event pressed = display.buttonPressed(enter);
        synchronized (pressed.getCondition()) {
            while (true) {
                pressed.waitFor();
                control.setSAS(!control.getSAS());
            }
        }
    }
}
