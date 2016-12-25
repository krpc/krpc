using System;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class QuaternionExample
{
    public static void Main ()
    {
        var connection = new Connection ();
        var spaceCenter = connection.SpaceCenter ();
        var vessel = spaceCenter.ActiveVessel;
        Tuple<double,double,double,double> q = vessel.Flight ().Rotation;
        Console.WriteLine (q.Item1 + "," + q.Item2 + "," + q.Item3 + "," + q.Item4);
    }
}
