using KRPC.Client;
using KRPC.Client.Services.KRPC;

class Program {
    public static void Main () {
        var connection = new Connection (name : "Example");
        var krpc = connection.KRPC ();
        System.Console.WriteLine (krpc.GetStatus ().Version);
    }
}
