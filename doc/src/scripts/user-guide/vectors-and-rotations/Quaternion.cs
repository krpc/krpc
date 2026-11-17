using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Quaternion {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            Tuple<double,double,double,double> q =
                vessel.Flight ().Rotation;
            Console.WriteLine ("({0:F2}, {1:F2}, {2:F2}, {3:F2})",
                               q.Item1, q.Item2, q.Item3, q.Item4);
        }
    }
}
