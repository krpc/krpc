using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        using (var connection = new Connection()) {
            var vessel = connection.SpaceCenter().ActiveVessel;
            var frame = vessel.Orbit.Body.ReferenceFrame;

            // Call a method on an object
            Console.WriteLine(vessel.Position(frame));

            // Call a static method, passing the connection first
            Console.WriteLine(Orbit.ReferencePlaneDirection(connection, frame));

            // Stream it
            var direction = connection.AddStream(
                () => Orbit.ReferencePlaneDirection(connection, frame));
            Console.WriteLine(direction.Get());
        }
    }
}
