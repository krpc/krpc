using System;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    static class FAR
    {
        public static void Load ()
        {
            IsAvailable = APILoader.Load (typeof(FAR), "FerramAerospaceResearch", "FerramAerospaceResearch.FARAPI", new Version (0, 15));
        }

        public static bool IsAvailable { get; private set; }

        public static Func<Vessel, double> VesselDynPres { get; internal set; }

        public static Func<Vessel, double> VesselLiftCoeff { get; internal set; }

        public static Func<Vessel, double> VesselDragCoeff { get; internal set; }

        public static Func<Vessel, double> VesselRefArea { get; internal set; }

        public static Func<Vessel, double> VesselTermVelEst { get; internal set; }

        public static Func<Vessel, double> VesselBallisticCoeff { get; internal set; }

        public static Func<Vessel, double> VesselAoA { get; internal set; }

        public static Func<Vessel, double> VesselSideslip { get; internal set; }

        public static Func<Vessel, double> VesselTSFC { get; internal set; }

        public static Func<Vessel, double> VesselStallFrac { get; internal set; }

        public static double VesselMachNumber (Vessel vessel)
        {
            var aero = vessel.GetComponent ("FARVesselAero");
            return (double)aero.GetType ().GetProperty ("MachNumber").GetValue (aero, null);
        }

        public static double VesselReynoldsNumber (Vessel vessel)
        {
            var aero = vessel.GetComponent ("FARVesselAero");
            return (double)aero.GetType ().GetProperty ("ReynoldsNumber").GetValue (aero, null);
        }
    }
}
