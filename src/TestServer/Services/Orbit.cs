using System;
using KRPC.Service;
using KRPC.Schema.Orbit;

namespace TestServer
{
    [KRPCService]
    public class Orbit
    {
        [KRPCProcedure]
        public static OrbitData GetOrbitData() {
            return OrbitData.CreateBuilder()
                .SetApoapsis(600000)
                .SetPeriapsis(600000)
                .SetEccentricity(0)
                .SetInclination(0)
                .SetLongitudeOfAscendingNode(0)
                .SetArgumentOfPeriapsis(0)
                .SetMeanAnomalyAtEpoch(0)
                .SetReferenceBody("Kerbin")
                .Build();
        }
    }
}
