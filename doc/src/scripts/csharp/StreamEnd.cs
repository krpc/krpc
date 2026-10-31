using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var part = connection.SpaceCenter().ActiveVessel.Parts.Root;
        var temperature = connection.AddStream(() => part.Temperature);
        try {
            lock (temperature.Condition) {
                while (true) {
                    temperature.Wait();
                    Console.WriteLine("Temperature " + temperature.Get());
                }
            }
        } catch (ObjectDestroyedException) {
            Console.WriteLine("The part was destroyed");
        }
    }
}
