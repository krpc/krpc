using KRPC.Service;
using KRPC.Schema.Flight;

namespace KRPCServices
{
    [KRPCService]
    static public class Flight
    {
        [KRPCProcedure]
        public static FlightData GetFlightData ()
        {
            var orbit = FlightGlobals.ActiveVessel.GetOrbit ();
            return FlightData.CreateBuilder ()
                .SetAltitude (orbit.altitude)
                .Build ();
        }
    }
}
