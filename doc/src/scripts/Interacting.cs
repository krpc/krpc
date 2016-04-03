using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main () {
        var connection = new Connection (name : "Vessel Name");
        var spaceCenter = connection.SpaceCenter ();
        var vessel = spaceCenter.ActiveVessel;
        System.Console.WriteLine (vessel.Name);
    }
}
