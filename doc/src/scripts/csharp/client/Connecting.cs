using System;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program {
    public static void Main() {
        using (var connection = new Connection(
                   name: "My Example Program",
                   address: IPAddress.Parse("192.168.0.10"),
                   port: 1000)) {
            var krpc = connection.KRPC();
            Console.WriteLine(krpc.GetStatus().Version);
        }
    }
}
