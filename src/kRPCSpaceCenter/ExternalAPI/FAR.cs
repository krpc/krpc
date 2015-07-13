using System;

namespace KRPCSpaceCenter.ExternalAPI
{
    internal static class FAR
    {
        public static void Load ()
        {
            IsAvailable = Loader.LoadAPI (typeof(FAR), "FerramAerospaceResearch", "ferram4.FARAPI", new Version (0, 14));
        }

        public static bool IsAvailable { get; private set; }

        public static Func<global::Vessel, bool> ActiveControlSysIsOnVessel { get; internal set; }

        public static Func<double> GetActiveControlSys_AirDensity { get; internal set; }

        public static Func<double> GetActiveControlSys_Q { get; internal set; }

        public static Func<double> GetActiveControlSys_Cl { get; internal set; }

        public static Func<double> GetActiveControlSys_Cd { get; internal set; }

        public static Func<double> GetActiveControlSys_Cm { get; internal set; }

        public static Func<double> GetActiveControlSys_RefArea { get; internal set; }

        public static Func<double> GetActiveControlSys_MachNumber { get; internal set; }

        public static Func<double> GetActiveControlSys_TermVel { get; internal set; }

        public static Func<double> GetActiveControlSys_BallisticCoeff { get; internal set; }

        public static Func<double> GetActiveControlSys_AoA { get; internal set; }

        public static Func<double> GetActiveControlSys_Sideslip { get; internal set; }

        public static Func<double> GetActiveControlSys_TSFC { get; internal set; }

        public static Func<double> GetActiveControlSys_StallFrac { get; internal set; }

        public static Func<string> GetActiveControlSys_StatusMessage { get; internal set; }
    }
}
