using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Vector3 {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            Tuple<double,double,double> v = vessel.Flight ().Prograde;
            Console.WriteLine ("({0:F2}, {1:F2}, {2:F2})",
                               v.Item1, v.Item2, v.Item3);
        }
    }
}
