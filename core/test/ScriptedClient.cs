using System;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Service.Messages;

namespace KRPC.Test
{
    // A client whose requests are handed over one at a time, and only once the server has
    // polled for them often enough. Polling stands in for the time a real client spends
    // deciding what to call next, so that what is measured is how many updates the calls
    // are spread over rather than how long anything took.
    sealed class ScriptedClient : IClient<Request,Response>
    {
        sealed class ScriptedStream : IStream<Request,Response>
        {
            readonly Queue<Request> pending;
            readonly int pollsBeforeReady;
            readonly Action onPollsBeforeGone;
            readonly int pollsBeforeGone;
            int polls;

            public ScriptedStream (IEnumerable<Request> requests, int pollsBeforeReady,
                                   int pollsBeforeGone, Action onPollsBeforeGone)
            {
                pending = new Queue<Request> (requests);
                this.pollsBeforeReady = pollsBeforeReady;
                this.pollsBeforeGone = pollsBeforeGone;
                this.onPollsBeforeGone = onPollsBeforeGone;
            }

            public IList<Response> Written { get; } = new List<Response> ();

            public bool DataAvailable {
                get {
                    // Counted here, since polling for a request is what the server does
                    // while it waits for one.
                    polls++;
                    if (pollsBeforeGone > 0 && polls > pollsBeforeGone)
                        onPollsBeforeGone ();
                    if (pending.Count == 0)
                        return false;
                    return polls > pollsBeforeReady;
                }
            }

            public Request Read ()
            {
                polls = 0;
                return pending.Dequeue ();
            }

            public void Write (Response value)
            {
                Written.Add (value);
            }

            public ulong BytesRead { get { return 0; } }

            public ulong BytesWritten { get { return 0; } }

            public void ClearStats ()
            {
            }

            public void Close ()
            {
            }

            public int Read (Request[] buffer, int offset)
            {
                throw new NotSupportedException ();
            }

            public int Read (Request[] buffer, int offset, int size)
            {
                throw new NotSupportedException ();
            }

            public void Write (Response[] buffer)
            {
                throw new NotSupportedException ();
            }

            public void Write (Response[] buffer, int offset, int size)
            {
                throw new NotSupportedException ();
            }
        }

        readonly ScriptedStream stream;

        public ScriptedClient (int pollsBeforeReady, int pollsBeforeGone,
                               params string[] procedures)
            : this (pollsBeforeReady, pollsBeforeGone, OnePerRequest (procedures))
        {
        }

        ScriptedClient (int pollsBeforeReady, int pollsBeforeGone, IEnumerable<Request> requests)
        {
            stream = new ScriptedStream (requests, pollsBeforeReady, pollsBeforeGone,
                                         () => Connected = false);
            Guid = Guid.NewGuid ();
            Connected = true;
        }

        /// <summary>
        /// A client whose script is grouped into requests, each inner array being the calls of
        /// one request. Calls sent together are executed together, in one continuation.
        /// </summary>
        public static ScriptedClient Batched (int pollsBeforeReady, int pollsBeforeGone,
                                              params string[][] requests)
        {
            var scripted = new List<Request> ();
            foreach (var procedures in requests) {
                var request = new Request ();
                foreach (var procedure in procedures)
                    request.Calls.Add (new ProcedureCall ("KRPC", procedure));
                scripted.Add (request);
            }
            return new ScriptedClient (pollsBeforeReady, pollsBeforeGone, scripted);
        }

        static IEnumerable<Request> OnePerRequest (IEnumerable<string> procedures)
        {
            var requests = new List<Request> ();
            foreach (var procedure in procedures) {
                var request = new Request ();
                request.Calls.Add (new ProcedureCall ("KRPC", procedure));
                requests.Add (request);
            }
            return requests;
        }

        public IList<Response> Written { get { return stream.Written; } }

        public string Name { get { return "ScriptedClient"; } }

        public Guid Guid { get; private set; }

        public string Address { get { return "scripted"; } }

        public bool Connected { get; set; }

        public IStream<Request,Response> Stream { get { return stream; } }

        public void Close ()
        {
            Connected = false;
        }

        // The round robin scheduler and the set of clients already being served both
        // compare clients, so identity has to come from the identifier.
        public bool Equals (IClient<Request,Response> other)
        {
            return other != null && Guid == other.Guid;
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as IClient<Request,Response>);
        }

        public override int GetHashCode ()
        {
            return Guid.GetHashCode ();
        }
    }
}
