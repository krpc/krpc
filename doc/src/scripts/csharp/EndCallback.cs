using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var part = connection.SpaceCenter().ActiveVessel.Parts.Root;
        var temperature = connection.AddStream(() => part.Temperature);

        temperature.AddCallback(
            (double value) => {
                Console.WriteLine("Temperature " + value);
            });
        temperature.AddEndCallback(
            (Exception error) => {
                Console.WriteLine("The stream ended: " + error.Message);
            });

        // Keep the program running, while the callbacks are called
        while (true)
            Thread.Sleep(1000);
    }
}
