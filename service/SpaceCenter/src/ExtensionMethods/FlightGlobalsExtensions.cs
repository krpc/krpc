using System;

namespace KRPC.SpaceCenter
{
    static class FlightGlobalsExtensions
    {
        /// <summary>
        /// The vessel with the given id, loaded or not,
        /// or null if the game has no such vessel.
        /// </summary>
        public static Vessel FindVesselById (Guid id)
        {
            var active = FlightGlobals.ActiveVessel;
            if (active != null && active.id == id)
                return active;
            foreach (var vessel in FlightGlobals.Vessels)
                if (vessel != null && vessel.id == id)
                    return vessel;
            return null;
        }

        /// <summary>
        /// The vessel with the given id, loaded or not.
        /// Throws if the game has no such vessel.
        /// </summary>
        public static Vessel GetVesselById (Guid id)
        {
            var vessel = FindVesselById (id);
            if (vessel != null)
                return vessel;
            throw new ArgumentException ("No such vessel " + id);
        }
    }
}
