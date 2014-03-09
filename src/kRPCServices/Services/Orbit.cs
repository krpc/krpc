using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    [KRPCService]
    static public class Orbit
    {
        [KRPCProperty]
        public static string Body {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().referenceBody.name; }
        }

        [KRPCProperty]
        public static double Apoapsis {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().ApR; }
        }

        [KRPCProperty]
        public static double Periapsis {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().PeR; }
        }

        [KRPCProperty]
        public static double ApoapsisAltitude {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().ApA; }
        }

        [KRPCProperty]
        public static double PeriapsisAltitude {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().PeA; }
        }

        [KRPCProperty]
        public static double Eccentricity {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().eccentricity; }
        }

        [KRPCProperty]
        public static double Inclination {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().inclination; }
        }

        [KRPCProperty]
        public static double LongitudeOfAscendingNode {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().LAN; }
        }

        [KRPCProperty]
        public static double ArgumentOfPeriapsis {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().argumentOfPeriapsis; }
        }

        [KRPCProperty]
        public static double MeanAnomalyAtEpoch {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().meanAnomalyAtEpoch; }
        }
    }
}
