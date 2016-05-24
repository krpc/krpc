using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.RemoteTech.Services
{
    /// <summary>
    /// This service provides functionality to interact with
    /// <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/75245-11-remotetech-v1610-2016-04-12/">RemoteTech</a>.
    /// </summary>
    [KRPCService (GameScene = GameScene.All)]
    public static class RemoteTech
    {
        static void CheckAPI ()
        {
            if (!API.IsAvailable)
                throw new InvalidOperationException ("RemoteTech is not available");
        }

        /// <summary>
        /// Get a communications object, representing the communication capability of a particular vessel.
        /// </summary>
        [KRPCProcedure]
        public static Comms Comms (KRPC.SpaceCenter.Services.Vessel vessel)
        {
            CheckAPI ();
            return new Comms (vessel);
        }

        /// <summary>
        /// Get the antenna object for a particular part.
        /// </summary>
        [KRPCProcedure]
        public static Antenna Antenna (KRPC.SpaceCenter.Services.Parts.Part part)
        {
            CheckAPI ();
            return new Antenna (part);
        }

        /// <summary>
        /// The names of the ground stations.
        /// </summary>
        [KRPCProperty]
        public static IList<string> GroundStations {
            get {
                CheckAPI ();
                return API.GetGroundStations ().ToList ();
            }
        }

        internal static IDictionary<Guid, CelestialBody> CelestialBodyIds {
            get { return FlightGlobals.Bodies.ToDictionary (API.GetCelestialBodyGuid, x => x); }
        }

        internal static IDictionary<Guid, string> GroundStationIds {
            get { return API.GetGroundStations ().ToDictionary (API.GetGroundStationGuid, x => x); }
        }
    }
}
