using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var resources = connection.SpaceCenter().ActiveVessel.Resources;

        // Compile the lambda once. Its remote procedure calls
        // are made again each time the function runs
        var fuelFraction = connection.Compile(
            () => resources.Amount("LiquidFuel") / resources.Max("LiquidFuel"));

        // Run the compiled function as often as needed
        while (true) {
            Console.WriteLine("Fuel: " + connection.Run<float>(fuelFraction));
            Thread.Sleep(1000);
        }
    }
}
