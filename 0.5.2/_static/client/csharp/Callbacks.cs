using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();
        var control = spaceCenter.ActiveVessel.Control;
        var abort = connection.AddStream(() => control.Abort);

        abort.AddCallback(
            (bool x) => {
                Console.WriteLine("Abort 1 called with a value of " + x);
            });
        abort.AddCallback(
            (bool x) => {
                Console.WriteLine("Abort 2 called with a value of " + x);
            });
        abort.Start();

        // Keep the program running...
        while (true) {
        }
    }
}
