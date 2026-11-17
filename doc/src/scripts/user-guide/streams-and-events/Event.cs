using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Event {
    public static void Main () {
        using (var connection = new Connection ()) {
            var flight = connection.SpaceCenter ().ActiveVessel.Flight ();
            // [docs]
            var evnt = connection.AddEvent (() => flight.MeanAltitude > 10000);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
            Console.WriteLine ("Above 10 km");
            // [/docs]
        }
    }
}
