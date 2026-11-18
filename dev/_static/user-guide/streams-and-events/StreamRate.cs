using System;
using System.Threading;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class StreamRate {
    public static void Main () {
        using (var connection = new Connection ()) {
            var flight = connection.SpaceCenter ().ActiveVessel.Flight ();
            // [docs]
            var altitude = connection.AddStream (() => flight.MeanAltitude);
            altitude.Rate = 10;
            while (true) {
                Console.WriteLine (altitude.Get ());
                Thread.Sleep (100);
            }
            // [/docs]
        }
    }
}
