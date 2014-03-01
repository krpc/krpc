using KRPC.Service.Attributes;

namespace KRPCServices
{
    [KRPCService]
    static public class Flight
    {
        [KRPCProperty]
        public static double Altitude {
            get { return FlightGlobals.ActiveVessel.GetOrbit ().altitude; }
        }
    }
}
