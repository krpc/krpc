using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class RaycastExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Raycast Example")) {
            var sc = connection.SpaceCenter ();
            var part = sc.ActiveVessel.Parts.Root;
            var frame = part.ReferenceFrame;

            // A fan of 8 rays below the part, 10 degrees apart
            var origin = Tuple.Create (0.0, 0.0, 0.0);
            var directions = new List<Tuple<double, double, double>> ();
            for (int i = 0; i < 8; i++) {
                var angle = (10 * i - 35) * Math.PI / 180;
                directions.Add (Tuple.Create (Math.Sin (angle), -Math.Cos (angle), 0.0));
            }

            // Cast every ray in the same physics tick
            var distances = connection.AddStream (
                () => directions.Select (d => sc.RaycastDistance (origin, d, frame)).ToList ());
            foreach (var distance in distances.Get ())
                Console.WriteLine (distance);
        }
    }
}
