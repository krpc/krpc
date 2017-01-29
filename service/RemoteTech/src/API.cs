using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.RemoteTech
{
    static class API
    {
        public static bool IsAvailable { get; private set; }

        public static void Load ()
        {
            IsAvailable = APILoader.Load (typeof(API), "RemoteTech", "RemoteTech.API.API", new Version (1, 8));
        }

        public static Func<Guid, bool> HasLocalControl { get; internal set; }

        public static Func<Guid, bool> HasFlightComputer { get; internal set; }

        public static Action<Guid, Action<FlightCtrlState>> AddSanctionedPilot { get; internal set; }

        public static Action<Guid, Action<FlightCtrlState>> RemoveSanctionedPilot { get; internal set; }

        public static Func<Guid, bool> HasAnyConnection { get; internal set; }

        public static Func<Guid, bool> HasConnectionToKSC { get; internal set; }

        public static Func<Part,bool> AntennaHasConnection { get; internal set; }

        public static Func<Part,Guid> GetAntennaTarget { get; internal set; }

        public static Action<Part,Guid> SetAntennaTarget { get; internal set; }

        public static Func<IEnumerable<string>> GetGroundStations { get; internal set; }

        public static Func<string,Guid> GetGroundStationGuid { get; internal set; }

        public static Func<CelestialBody,Guid> GetCelestialBodyGuid { get; internal set; }

        public static Func<Guid> GetNoTargetGuid { get; internal set; }

        public static Func<Guid> GetActiveVesselGuid { get; internal set; }

        public static Func<Guid, double> GetShortestSignalDelay { get; internal set; }

        public static Func<Guid, double> GetSignalDelayToKSC { get; internal set; }

        public static Func<Guid, Guid, double> GetSignalDelayToSatellite { get; internal set; }
    }
}
