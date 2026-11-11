using System;
using KRPC.Client;
using KRPC.Client.Services.KerbalEngineerRedux;
using KRPC.Client.Services.SpaceCenter;

class KerbalEngineerReduxExample
{
    public static void Main ()
    {
        using (var connection = new Connection ("Kerbal Engineer Redux Example")) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            var ker = connection.KerbalEngineerRedux ();

            var stages = ker.Stages (vessel);
            for (int i = stages.Count - 1; i >= 0; i--) {
                var stage = stages [i];
                Console.WriteLine ("Stage {0}: {1:F0} m/s, TWR {2:F2}", stage.Number, stage.DeltaV, stage.AvailableTWR);
            }

            var burn = ker.BurnTime (vessel, 500);
            if (burn == null)
                Console.WriteLine ("Not enough delta-v for the burn");
            else
                Console.WriteLine ("A 500 m/s burn takes {0:F1} seconds", burn.Value.Duration);
        }
    }
}
