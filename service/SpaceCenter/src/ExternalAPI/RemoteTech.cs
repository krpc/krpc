using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// RemoteTech's API, which the autopilot and the RemoteTech service both use. SpaceCenter
    /// supports no RemoteTech parts, so its registry entry does not name the mod.
    /// </summary>
    sealed class RemoteTech : ModAPI
    {
        public RemoteTech () : base ("RemoteTech", "RemoteTech")
        {
        }

        protected override void Bind (ModBinder mod)
        {
            mod.Statics (typeof (RemoteTech), "RemoteTech.API.API");
            // The autopilot calls these every physics frame, so a missing one fails the load
            var api = mod.Type ("RemoteTech.API.API").Require;
            HasFlightComputer = api.Method<Func<Guid, bool>> ("HasFlightComputer");
            AddSanctionedPilot = api.Method<Action<Guid, Action<FlightCtrlState>>> ("AddSanctionedPilot");
            RemoveSanctionedPilot = api.Method<Action<Guid, Action<FlightCtrlState>>> ("RemoveSanctionedPilot");
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

        public static Func<string, Guid> GetGroundStationGuid { get; internal set; }

        public static Func<CelestialBody,Guid> GetCelestialBodyGuid { get; internal set; }

        public static Func<Guid> GetNoTargetGuid { get; internal set; }

        public static Func<Guid> GetActiveVesselGuid { get; internal set; }

        public static Func<Guid, double> GetShortestSignalDelay { get; internal set; }

        public static Func<Guid, double> GetSignalDelayToKSC { get; internal set; }

        public static Func<Guid, Guid, double> GetSignalDelayToSatellite { get; internal set; }
    }
}
