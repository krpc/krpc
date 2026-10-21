using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var vessel = connection.SpaceCenter().ActiveVessel;
        var flight = vessel.Flight(vessel.Orbit.Body.ReferenceFrame);

        // Run the lambda on the server, so both values come from the same physics tick
        var result = connection.Run(() => Tuple.Create(flight.MeanAltitude, flight.Speed));
        Console.WriteLine("Altitude: " + result.Item1 + " m, speed: " + result.Item2 + " m/s");
    }
}
