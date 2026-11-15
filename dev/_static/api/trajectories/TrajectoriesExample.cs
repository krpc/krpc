using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;
using KRPC.Client.Services.Trajectories;

class TrajectoriesExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Trajectories Example")) {
            var body = connection.SpaceCenter ().ActiveVessel.Orbit.Body;
            Console.WriteLine (connection.Trajectories ()
                .ImpactPosition (body.ReferenceFrame));
        }
    }
}
