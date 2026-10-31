using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var control = connection.SpaceCenter().ActiveVessel.Control;
        var abort = connection.AddStream(() => control.Abort);

        abort.AddCallback(
            (bool x) => {
                Console.WriteLine("Abort 1 called with a value of " + x);
            });
        abort.AddCallback(
            (bool x) => {
                Console.WriteLine("Abort 2 called with a value of " + x);
            });

        // Keep the program running, while the callbacks are called
        while (true)
            Thread.Sleep(1000);
    }
}
