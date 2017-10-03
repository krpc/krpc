using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.UI;

class UserInterface
{
    public static void Main ()
    {
        var conn = new Connection ("User Interface Example");
        var canvas = conn.UI ().StockCanvas;

        // Get the size of the game window in pixels
        var screenSize = canvas.RectTransform.Size;

        // Add a panel to contain the UI elements
        var panel = canvas.AddPanel ();

        // Position the panel on the left of the screen
        var rect = panel.RectTransform;
        rect.Size = Tuple.Create (200.0, 100.0);
        rect.Position = Tuple.Create ((110-(screenSize.Item1)/2), 0.0);

        // Add a button to set the throttle to maximum
        var button = panel.AddButton ("Full Throttle");
        button.RectTransform.Position = Tuple.Create (0.0, 20.0);

        // Add some text displaying the total engine thrust
        var text = panel.AddText ("Thrust: 0 kN");
        text.RectTransform.Position = Tuple.Create (0.0, -20.0);
        text.Color = Tuple.Create (1.0, 1.0, 1.0);
        text.Size = 18;

        // Set up a stream to monitor the throttle button
        var buttonClicked = conn.AddStream (() => button.Clicked);

        var vessel = conn.SpaceCenter ().ActiveVessel;
        while (true) {
            // Handle the throttle button being clicked
            if (buttonClicked.Get ()) {
                vessel.Control.Throttle = 1;
                button.Clicked = false;
            }

            // Update the thrust text
            text.Content = "Thrust: " + (vessel.Thrust/1000) + " kN";

            System.Threading.Thread.Sleep (1000);
        }
    }
}
