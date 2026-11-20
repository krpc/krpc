using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class SpaceCenterExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("SpaceCenter Example")) {
            Console.WriteLine (connection.SpaceCenter ().ActiveVessel.Name);
        }
    }
}
