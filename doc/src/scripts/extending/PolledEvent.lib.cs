using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class Altitude {
        // [docs]
        [KRPCProcedure]
        public static KRPC.Service.Messages.Event OnAltitudeReached(uint altitude) {
            // Create the event
            var evnt = new KRPC.Service.Event ((KRPC.Service.Event e) => {
                bool result = FlightGlobals.ActiveVessel.terrainAltitude > altitude;
                if (result)
                    e.Remove();
                return result;
            });

            // Return the message describing the event to the client
            return evnt.Message;
        }
        // [/docs]
    }
}
