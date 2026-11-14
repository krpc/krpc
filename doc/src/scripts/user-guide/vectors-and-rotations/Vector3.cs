using System;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class VectorExample
{
    public static void Main ()
    {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            Tuple<double,double,double> v = vessel.Flight ().Prograde;
            Console.WriteLine (v.Item1 + ", " + v.Item2 + ", " + v.Item3);
        }
    }
}
