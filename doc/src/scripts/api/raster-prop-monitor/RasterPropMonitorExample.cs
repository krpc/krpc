using KRPC.Client;
using KRPC.Client.Services.RasterPropMonitor;
using KRPC.Client.Services.SpaceCenter;

class RasterPropMonitorExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Raster Prop Monitor Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var control = vessel.Control;
            var rpm = connection.RasterPropMonitor ();

            // The first screen in the IVA, and the content of its krpc page
            var monitor = rpm.Monitors (vessel) [0];
            var display = monitor.Display;

            // Raster Prop Monitor formats the SAS variable, and keeps it up to date
            display.Text = "SAS: {0:ON;OFF;OFF} $&$ SAS";

            // A press of the NAV button shows the page
            monitor.CaptureButton ("buttonR1", "krpc");

            // Each press of the ENTER button toggles SAS
            var enter = monitor.Buttons.IndexOf ("button_ENTER");
            var pressed = display.ButtonPressed (enter);
            lock (pressed.Condition) {
                while (true) {
                    pressed.Wait ();
                    control.SAS = !control.SAS;
                }
            }
        }
    }
}
