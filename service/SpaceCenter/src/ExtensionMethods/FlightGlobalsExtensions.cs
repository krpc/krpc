using System;

namespace KRPC.SpaceCenter
{
    static class FlightGlobalsExtensions
    {
        public static Vessel GetVesselById (Guid id)
        {
            if (FlightGlobals.ActiveVessel.id == id)
                return FlightGlobals.ActiveVessel;
            foreach (var vessel in FlightGlobals.Vessels)
                if (vessel.id == id)
                    return vessel;
            throw new ArgumentException ("No such vessel " + id);
        }
    }
}
