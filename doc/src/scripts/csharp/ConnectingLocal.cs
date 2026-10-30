using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program {
    public static void Main() {
        var connection = Connection.ConnectLocal(name: "My Example Program");
        Console.WriteLine(connection.KRPC().GetStatus().Version);
    }
}
