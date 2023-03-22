using System;
using KRPC;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
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
        /// Create an event stream, that calls a function when it updates to
        /// determine if the event is triggered.
        /// </summary>
        public Event (Func<Event, bool> func)
        {
            stream = new EventStream (() => func(this));
            client = CallContext.Client;
            streamId = Core.Instance.AddStream (client, stream);
            Message = new Messages.Event (new Messages.Stream (streamId));
        }


        /// <summary>
        /// Create an event stream, that calls a continuation when it updates to
        /// determine if the event is triggered.
        /// </summary>
        public Event (Func<bool> continuation)
        {
            stream = new EventStream (continuation);
            client = CallContext.Client;
            streamId = Core.Instance.AddStream (client, stream);
            Message = new Messages.Event (new Messages.Stream (streamId));
        }

        /// <summary>
        /// Trigger the event.
        /// </summary>
        public void Trigger ()
        {
            stream.Trigger();
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
