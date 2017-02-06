using System.Net;
using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program
{
    public static void Main ()
    {
        using (var connection = new Connection (
            name : "Example", address: IPAddress.Parse ("10.0.2.2"),
            rpcPort: 1000, streamPort: 1001)) {
            var krpc = connection.KRPC ();
            System.Console.WriteLine (krpc.GetStatus ().Version);
        }
    }
}
