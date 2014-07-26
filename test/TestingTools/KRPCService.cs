using System;
using System.Linq;
using UnityEngine;
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
        public static void RemoveAllVessels ()
        {
            // FIXME: this removes the vessels from the save, but doesn't update the current game state
            var vessels = FlightGlobals.Vessels.Where (v => v != FlightGlobals.ActiveVessel).ToList ();
            foreach (var vessel in vessels)
                HighLogic.CurrentGame.DestroyVessel (vessel);
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

        [KRPCProcedure]
        public static void ClearRotation ()
        {
            var vessel = FlightGlobals.ActiveVessel;
            var right = vessel.GetWorldPos3D () - vessel.mainBody.position;
            var northPole = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - (vessel.GetWorldPos3D ());
            northPole.Normalize ();
            var up = Vector3.Exclude (right, northPole);
            var forward = Vector3.Cross (right, northPole);
            Vector3.OrthoNormalize (ref forward, ref up);
            var rotation = Quaternion.LookRotation (forward, up);
            rotation = Quaternion.AngleAxis (90, new Vector3 (0, -1, 0)) * rotation;
            vessel.SetRotation (rotation);
        }
    }
}
