using System;
using KRPC;
using KRPC.Service.Attributes;
using System.Linq;

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

        [KRPCProcedure]
        public static void LaunchVesselFromVAB (string name)
        {
            var craft = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/VAB/" + name + ".craft";
            var crew = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel (ConfigNode.Load (craft), true);
            FlightDriver.StartWithNewLaunch (craft, EditorLogic.FlagURL, "LaunchPad", crew);
        }

        [KRPCProcedure]
        public static void LaunchVesselFromSPH (string name)
        {
            var craft = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/SPH/" + name + ".craft";
            var crew = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel (ConfigNode.Load (craft), true);
            FlightDriver.StartWithNewLaunch (craft, EditorLogic.FlagURL, "Runway", crew);
        }

        [KRPCProcedure]
        public static void SetCircularOrbit (string body, double altitude)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            var semiMajorAxis = celestialBody.Radius + altitude;
            OrbitTools.OrbitDriver.orbit.SetOrbit (OrbitTools.CreateOrbit (celestialBody, semiMajorAxis, 0, 0, 0, 0, 0, 0));
        }

        [KRPCProcedure]
        public static void SetOrbit (string body, double semiMajorAxis, double eccentricity, double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            OrbitTools.OrbitDriver.orbit.SetOrbit (OrbitTools.CreateOrbit (celestialBody, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch));
        }
    }
}
