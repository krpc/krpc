using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        using (var connection = new Connection()) {
            var frame = connection.SpaceCenter().ActiveVessel.ReferenceFrame;

            // Bind the static methods to the connection once
            var orbit = Orbit.Bind(connection);
            Console.WriteLine(orbit.ReferencePlaneNormal(frame));
            Console.WriteLine(orbit.ReferencePlaneDirection(frame));
        }
    }
}
