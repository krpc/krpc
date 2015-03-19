using System;

namespace KRPCSpaceCenter.ExternalAPI
{
    internal static class RemoteTech
    {
        public static void Load ()
        {
            IsAvailable = Loader.LoadAPI (typeof(RemoteTech), "RemoteTech", "RemoteTech.API.API", new Version (1, 6));
        }

        public static bool IsAvailable { get; private set; }

        public static Func<Guid, bool> HasFlightComputer { get; internal set; }

        public static Action<Guid, Action<FlightCtrlState>> AddSanctionedPilot { get; internal set; }

        public static Action<Guid, Action<FlightCtrlState>> RemoveSanctionedPilot { get; internal set; }

        public static Func<Guid, bool> HasAnyConnection { get; internal set; }

        public static Func<Guid, bool> HasConnectionToKSC { get; internal set; }

        public static Func<Guid, double> GetShortestSignalDelay { get; internal set; }

        public static Func<Guid, double> GetSignalDelayToKSC { get; internal set; }

        public static Func<Guid, Guid, double> GetSignalDelayToSatellite { get; internal set; }
    }
}
