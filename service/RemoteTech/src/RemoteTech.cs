using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using API = KRPC.SpaceCenter.ExternalAPI.RemoteTech;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// Provides functionality to interact with RemoteTech.
    /// </summary>
    [KRPCService (Id = ServiceInfo.Id, GameScene = GameScene.All)]
    [RequiresMod (typeof (API))]
    public static class RemoteTech
    {
        /// <summary>
        /// Whether RemoteTech is installed.
        /// </summary>
        [KRPCProperty]
        public static bool Available {
            get { return ModAPI.Get<API> ().IsAvailable; }
        }

        /// <summary>
        /// Get a communications object, representing the communication capability of a particular vessel.
        /// </summary>
        [KRPCProcedure]
        public static Comms Comms (SpaceCenter.Services.Vessel vessel)
        {
            return new Comms (vessel);
        }

        /// <summary>
        /// Get the antenna object for a particular part.
        /// </summary>
        [KRPCProcedure]
        public static Antenna Antenna (SpaceCenter.Services.Parts.Part part)
        {
            return new Antenna (part);
        }

        /// <summary>
        /// The names of the ground stations.
        /// </summary>
        [KRPCProperty]
        public static IList<string> GroundStations {
            get { return API.GetGroundStations ().ToList (); }
        }

        internal static IDictionary<Guid, CelestialBody> CelestialBodyIds {
            get { return FlightGlobals.Bodies.ToDictionary (API.GetCelestialBodyGuid, x => x); }
        }

        internal static IDictionary<Guid, string> GroundStationIds {
            get { return API.GetGroundStations ().ToDictionary (API.GetGroundStationGuid, x => x); }
        }
    }
}
