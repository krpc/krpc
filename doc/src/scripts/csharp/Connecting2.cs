using System;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program {
    public static void Main() {
        var connection = new Connection(
            name: "My Example Program",
            address: IPAddress.Parse("192.168.1.10"),
            port: 1000);
        Console.WriteLine(connection.KRPC().GetStatus().Version);
    }
}
