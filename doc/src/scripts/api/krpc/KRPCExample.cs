using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class KRPCExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("KRPC Example")) {
            Console.WriteLine (connection.KRPC ().GetStatus ().Version);
        }
    }
}
