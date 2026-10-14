using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.StdLib;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var spaceCenter = connection.SpaceCenter ();
        var vessel = spaceCenter.ActiveVessel;
        // [docs]
        var stdlib = connection.StdLib ();
        var frame = vessel.Orbit.Body.ReferenceFrame;
        var stream = connection.AddStream (
            () => stdlib.RadiansToDegrees (stdlib.VectorAngle (
                vessel.Direction (frame), vessel.Velocity (frame))));
        // [/docs]
    }
}
