using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program {
    public static void Main() {
        var connection = new Connection();
        Console.WriteLine(connection.KRPC().GetStatus().Version);
    }
}
