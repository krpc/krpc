using System;
using System.Collections.Generic;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using Type = KRPC.Client.Services.KRPC.Type;

class Program {
    public static void Main () {
        var connection = new Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        var flight = vessel.Flight ();
        double targetPitch = 45;
        // [docs]
        var e = Expression.Bind (connection);
        var t = Type.Bind (connection);
        var command = connection.Compile (() => Math.Max (
            -1.0, Math.Min (1.0, 0.05 * (targetPitch - flight.Pitch))));
        // A C# lambda cannot assign to a property, so the expression API
        // builds the assignment
        var setPitch = Connection.GetSetCallTemplate<Control> (
            nameof (Control.Pitch));
        var control = RemoteObject.GetObjectId (vessel.Control);
        var step = connection.Compile (e.CallWithArguments (setPitch,
            new Dictionary<int, Expression> {
                { 0, e.ConstantObject (control) },
                { 1, e.Cast (command, t.Float ()) }
            }));

        while (true)
            connection.Run (step);
        // [/docs]
    }
}
