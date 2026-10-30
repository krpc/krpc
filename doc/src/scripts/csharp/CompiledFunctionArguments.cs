using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        // [docs]
        var flight = connection.SpaceCenter().ActiveVessel.Flight();
        var altitudeAbove = connection.Compile((double height) => flight.MeanAltitude - height);
        Console.WriteLine(connection.Run<double>(altitudeAbove, 70000));
        // [/docs]
    }
}
