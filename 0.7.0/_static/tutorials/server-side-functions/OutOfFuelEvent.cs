using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var spaceCenter = connection.SpaceCenter ();
        var vessel = spaceCenter.ActiveVessel;
        // [docs]
        var evnt = connection.AddEvent (
            () => vessel.Parts.Engines.Any (engine => !engine.HasFuel));
        lock (evnt.Condition) {
            evnt.Wait ();
            Console.WriteLine ("An engine has run out of fuel");
        }
        // [/docs]
    }
}
