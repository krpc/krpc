using System;

namespace KRPCSpaceCenter.ExternalAPI
{
    static class FAR
    {
        public static void Load ()
        {
            IsAvailable = Loader.LoadAPI (typeof(FAR), "FerramAerospaceResearch", "FerramAerospaceResearch.FARAPI", new Version (0, 15));
        }

        public static bool IsAvailable { get; private set; }

        public static Func<global::Vessel, bool> ActiveControlSysIsOnVessel { get; internal set; }

        public static Func<global::Vessel, double> VesselDynPres { get; internal set; }

        public static Func<global::Vessel, double> VesselLiftCoeff { get; internal set; }

        public static Func<global::Vessel, double> VesselDragCoeff { get; internal set; }

        public static Func<global::Vessel, double> VesselRefArea { get; internal set; }

        public static Func<global::Vessel, double> VesselTermVelEst { get; internal set; }

        public static Func<global::Vessel, double> VesselBallisticCoeff { get; internal set; }

        public static Func<global::Vessel, double> VesselAoA { get; internal set; }

        public static Func<global::Vessel, double> VesselSideslip { get; internal set; }

        public static Func<global::Vessel, double> VesselTSFC { get; internal set; }

        public static Func<global::Vessel, double> VesselStallFrac { get; internal set; }
    }
}
