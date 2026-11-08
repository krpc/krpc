using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class Program {
    public static void Main() {
        var connection = new Connection();
        var spaceCenter = connection.SpaceCenter();

        // Show only probes and ships in map mode
        spaceCenter.MapFilter = MapFilterType.Probes | MapFilterType.Ships;

        // Check whether map mode shows debris
        Console.WriteLine(spaceCenter.MapFilter.HasFlag(MapFilterType.Debris));

        // Show debris as well
        spaceCenter.MapFilter |= MapFilterType.Debris;
    }
}
