using System;
using KRPC.Service;
using KRPC.Schema.Flight;
using KSP;

namespace KRPCServices
{
    [KRPCService]
    public class Flight
    {
        [KRPCProcedure]
        public static FlightData GetFlightData() {
            var orbit = FlightGlobals.ActiveVessel.GetOrbit();
            return FlightData.CreateBuilder()
                .SetAltitude(orbit.altitude)
                .Build();
        }
    }
}
