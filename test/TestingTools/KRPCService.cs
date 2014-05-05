using System;
using KRPC;
using KRPC.Service.Attributes;

namespace TestingTools
{
    [KRPCService]
    public static class TestingTools
    {
        [KRPCProcedure]
        public static void LoadSave (string directory, string name)
        {
            HighLogic.SaveFolder = directory;
            var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
            if (game == null || game.flightState == null || !game.compatible)
                throw new ArgumentException ("Failed to load save '" + name + "'");
            if (game.flightState.protoVessels.Count == 0)
                throw new ArgumentException ("Failed to load vessel id 0 from save '" + name + "'");
            FlightDriver.StartAndFocusVessel (game, 0);
        }
    }
}
