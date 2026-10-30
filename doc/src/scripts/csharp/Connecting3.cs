using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program {
    public static void Main() {
        using (var connection = new Connection(name: "My Example Program")) {
            Console.WriteLine(connection.KRPC().GetStatus().Version);
        }
    }
}
