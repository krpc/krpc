using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program
{
    public static void Main ()
    {
        using (var connection = new Connection (name : "Example")) {
            var krpc = connection.KRPC ();
            System.Console.WriteLine (krpc.GetStatus ().Version);
        }
    }
}
