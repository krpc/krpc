using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        using (var connection = new Connection()) {
            var vessel = connection.SpaceCenter().ActiveVessel;

            // Call a static method, passing the connection first
            Console.WriteLine(Orbit.ReferencePlaneDirection(connection, vessel.ReferenceFrame));

            // Stream it
            var direction = connection.AddStream(
                () => Orbit.ReferencePlaneDirection(connection, vessel.ReferenceFrame));
            Console.WriteLine(direction.Get());
        }
    }
}
