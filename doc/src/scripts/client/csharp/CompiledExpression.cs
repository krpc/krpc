using System;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var flight = vessel.Flight ();

        // Create an event from a lambda. The condition is compiled into a
        // server side expression, and checked on the server each physics tick.
        var evnt = connection.AddEvent (() => flight.MeanAltitude > 1000);
        lock (evnt.Condition) {
            evnt.Wait ();
            Console.WriteLine ("Altitude reached 1000m");
        }

        // Remote procedure calls made by the lambda are re-invoked on each
        // evaluation, including calls made on each element of a collection.
        // This event fires when any engine on the vessel runs out of fuel:
        var engines = vessel.Parts.Engines;
        evnt = connection.AddEvent (() => engines.Any (engine => !engine.HasFuel));
        lock (evnt.Condition) {
            evnt.Wait ();
            Console.WriteLine ("An engine has run out of fuel");
        }

        // Compound expressions passed to AddStream are compiled and computed
        // on the server. This streams the vessel's altitude in kilometers:
        var stream = connection.AddStream (() => flight.MeanAltitude / 1000);
        Console.WriteLine ("Altitude: " + stream.Get () + " km");
    }
}
