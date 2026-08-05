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

        // Add a panel to contain the UI elements. The user can drag it around
        // the screen with the mouse.
        var panel = canvas.AddPanel ();
        panel.Draggable = true;

        // Position the panel on the left of the screen
        var rect = panel.RectTransform;
        rect.Size = Tuple.Create (200.0, 120.0);
        rect.Position = Tuple.Create ((110-(screenSize.Item1)/2), 0.0);

        // Lay the contents of the panel out in a column
        var layout = panel.AddVerticalLayout ();
        layout.Padding = Tuple.Create (10, 10, 10, 10);
        layout.Spacing = 6;

        // Add a button to set the throttle to maximum, with a tooltip shown
        // while the mouse rests on it
        var button = panel.AddButton ("Full Throttle");
        button.Tooltip = "Set the throttle to maximum";
        button.LayoutElement.PreferredSize = Tuple.Create (-1.0, 30.0);

        // Add a slider to set the throttle by hand
        var slider = panel.AddSlider ();
        slider.LayoutElement.PreferredSize = Tuple.Create (-1.0, 20.0);

        // Add some text displaying the total engine thrust
        var text = panel.AddText ("Thrust: 0 kN");
        text.Color = Tuple.Create (1.0, 1.0, 1.0, 1.0);
        text.Size = 18;

        // Set up streams to monitor the button and the slider
        var buttonClicked = conn.AddStream (() => button.Clicked);
        var sliderChanged = conn.AddStream (() => slider.Changed);

        var vessel = conn.SpaceCenter ().ActiveVessel;
        while (true) {
            // Handle the throttle button being clicked
            if (buttonClicked.Get ()) {
                vessel.Control.Throttle = 1;
                slider.Value = 1;
                button.Clicked = false;
            }

            // Handle the user moving the throttle slider
            if (sliderChanged.Get ()) {
                vessel.Control.Throttle = slider.Value;
                slider.Changed = false;
            }

            // Update the thrust text
            text.Content = "Thrust: " + (vessel.Thrust/1000) + " kN";

            System.Threading.Thread.Sleep (1000);
        }
    }
}
