using System;
using KRPC;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using KRPC.Server;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// Stream for an event.
    /// </summary>
    public class Event
    {
        EventStream stream;
        IClient client;
        ulong streamId;

        /// <summary>
        /// Create an event stream.
        /// </summary>
        public Event ()
        {
            stream = new EventStream ();
            client = CallContext.Client;
            streamId = Core.Instance.AddStream (client, stream);
            Message = new Messages.Event (new Messages.Stream (streamId));
        }

        /// <summary>
        /// Trigger the event.
        /// </summary>
        public void Trigger ()
        {
            stream.Result.Value = true;
            stream.Changed = true;
        }

        /// <summary>
        /// Remove the event.
        /// </summary>
        public void Remove ()
        {
            stream.Remove ();
        }

        /// <summary>
        /// Event message for this event stream.
        /// </summary>
        public Messages.Event Message { get; private set; }
    }
}
