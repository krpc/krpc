using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace TestServer
{
    /// <summary>
    /// Server state the conformance tests control, so a test chooses when a stream updates, an
    /// event fires or a connection ends. State is keyed by name, and shared by every client.
    /// </summary>
    [KRPCService (Id = 9996)]
    public static class ConformanceService
    {
        static readonly IDictionary<string, int> values = new Dictionary<string, int> ();
        static readonly IDictionary<string, string> failures = new Dictionary<string, string> ();
        static readonly IDictionary<string, double?> doubles = new Dictionary<string, double?> ();
        static readonly IDictionary<string, EventState> events = new Dictionary<string, EventState> ();

        /// <summary>
        /// Sets a named value, and clears a failure set by <see cref="FailValue"/>.
        /// </summary>
        [KRPCProcedure]
        public static void SetValue (string name, int value)
        {
            values [name] = value;
            failures.Remove (name);
        }

        /// <summary>
        /// A named value, zero until set.
        /// </summary>
        [KRPCProcedure]
        public static int Value (string name)
        {
            string message;
            if (failures.TryGetValue (name, out message))
                throw new InvalidOperationException (message);
            int value;
            return values.TryGetValue (name, out value) ? value : 0;
        }

        /// <summary>
        /// Makes <see cref="Value"/> throw an <see cref="InvalidOperationException"/> with the
        /// given message.
        /// </summary>
        [KRPCProcedure]
        public static void FailValue (string name, string message)
        {
            failures [name] = message;
        }

        /// <summary>
        /// Sets a named floating point value.
        /// </summary>
        [KRPCProcedure]
        public static void SetDouble (string name, double? value)
        {
            doubles [name] = value;
        }

        /// <summary>
        /// A named floating point value, null until set.
        /// </summary>
        [KRPCProcedure]
        public static double? Double (string name)
        {
            double? value;
            return doubles.TryGetValue (name, out value) ? value : null;
        }

        sealed class EventState
        {
            public int Triggered;
            public int Fired;
            public bool Removed;
        }

        /// <summary>
        /// An event that fires once per <see cref="TriggerEvent"/>, and ends on
        /// <see cref="RemoveEvent"/> once its firings are sent.
        /// </summary>
        [KRPCProcedure]
        public static KRPC.Service.Messages.Event CreateEvent (string name)
        {
            var state = new EventState ();
            events [name] = state;
            var evnt = new Event (
                (Event e) => {
                    if (state.Fired < state.Triggered) {
                        state.Fired++;
                        return true;
                    }
                    if (state.Removed)
                        e.Remove ();
                    return false;
                });
            return evnt.Message;
        }

        /// <summary>
        /// Fires the named event on the next update.
        /// </summary>
        [KRPCProcedure]
        public static void TriggerEvent (string name)
        {
            events [name].Triggered++;
        }

        /// <summary>
        /// The number of times the named event has fired. The server writes each firing in the
        /// update pass that counts it.
        /// </summary>
        [KRPCProcedure]
        public static int EventFirings (string name)
        {
            return events [name].Fired;
        }

        /// <summary>
        /// Ends the named event, after any firing not yet sent.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveEvent (string name)
        {
            events [name].Removed = true;
        }

        /// <summary>
        /// Returns after the given time, in milliseconds, without holding up other calls.
        /// </summary>
        [KRPCProcedure]
        public static int Block (int milliseconds)
        {
            var timer = Stopwatch.StartNew ();
            return BlockUntil (timer, milliseconds);
        }

        static int BlockUntil (Stopwatch timer, int milliseconds)
        {
            if (timer.ElapsedMilliseconds >= milliseconds)
                return milliseconds;
            throw new YieldException<Func<int>> (() => BlockUntil (timer, milliseconds));
        }

        /// <summary>
        /// Ends the connection of every client with the given name, with a disconnect message
        /// carrying the given reason, such as <c>ServerStopped</c>.
        /// </summary>
        [KRPCProcedure]
        public static void DisconnectClient (string name, string reason, string message)
        {
            var disconnectReason = (DisconnectReason)Enum.Parse (typeof (DisconnectReason), reason);
            var clients = KRPC.Core.Instance.Clients.Where (x => x.Name == name).ToList ();
            if (clients.Count == 0)
                throw new ArgumentException ("No client is named " + name);
            foreach (var client in clients) {
                var stream = ((IClient<KRPC.Service.Messages.Request, KRPC.Service.Messages.Response>)client).Stream as IMessageStream;
                if (stream == null)
                    throw new InvalidOperationException ("The client has no message stream");
                stream.Disconnect (disconnectReason, message);
            }
        }

        /// <summary>
        /// A new probe.
        /// </summary>
        [KRPCProcedure]
        public static Probe CreateProbe ()
        {
            return new Probe ();
        }

        /// <summary>
        /// An object whose members report the client that called them.
        /// </summary>
        [KRPCClass]
        public sealed class Probe : Equatable<Probe>
        {
            readonly Guid id = Guid.NewGuid ();

            /// <summary>
            /// The name of the client that called this method.
            /// </summary>
            [KRPCMethod]
            public string ClientName ()
            {
                return CallContext.Client.Name;
            }

            /// <summary>
            /// The name of the client that called this method.
            /// </summary>
            [KRPCMethod]
            public static string StaticClientName ()
            {
                return CallContext.Client.Name;
            }

            public override bool Equals (Probe other)
            {
                return !ReferenceEquals (other, null) && id == other.id;
            }

            public override int GetHashCode ()
            {
                return id.GetHashCode ();
            }
        }
    }
}
