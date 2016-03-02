using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using System;
using System.Net;

class QuaternionExample
{
    public static void Main ()
    {
        var connection = new KRPC.Client.Connection ();
        var vessel = connection.SpaceCenter ().ActiveVessel;
        Tuple<double,double,double,double> q = vessel.Flight ().Rotation;
        Console.WriteLine (q.Item1 + "," + q.Item2 + "," + q.Item3 + "," + q.Item4);
    }
}
