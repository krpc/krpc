using System;
using KRPC.Schema.Orbit;
using Google.ProtocolBuffers;
using UnityEngine;

namespace KRPC.Service
{
    public class Orbit
    {
        [KRPCMethod]
        public static IMessage Get() {
            var orbit = FlightGlobals.ActiveVessel.GetOrbit();
            return OrbitData.CreateBuilder()
                .SetApoapsis(orbit.ApR)
                .SetPeriapsis(orbit.PeR)
                .SetEccentricity(orbit.eccentricity)
                .SetInclination(orbit.inclination)
                .SetLongitudeOfAscendingNode(orbit.LAN)
                .SetArgumentOfPeriapsis(orbit.argumentOfPeriapsis)
                .SetMeanAnomalyAtEpoch(orbit.meanAnomalyAtEpoch)
                .SetBody(orbit.referenceBody.name)
                .BuildPartial();
        }
    }
}
