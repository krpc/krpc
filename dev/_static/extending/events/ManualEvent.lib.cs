using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Extending
{
    [KRPCService]
    public static class Timers {
        // [docs]
        [KRPCProcedure]
        public static KRPC.Service.Messages.Event OnTimer(uint milliseconds) {
            // Create the event
            var evnt = new KRPC.Service.Event ();

            // Set up a timer that will trigger the event
            var timer = new System.Timers.Timer (milliseconds);
            timer.Elapsed += (s, e) => {
                evnt.Trigger ();
                evnt.Remove ();
                timer.Enabled = false;
            };
            timer.Start();

            // Return the message describing the event to the client
            return evnt.Message;
        }
        // [/docs]
    }
}
