using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();

        // Stream whether map mode shows both probes and ships, computed on the server
        var craft = MapFilterType.Probes | MapFilterType.Ships;
        var showsCraft = connection.AddStream(() => spaceCenter.MapFilter.HasFlag(craft));
        Console.WriteLine(showsCraft.Get());
    }
}
