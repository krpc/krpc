using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using KRPC.Server;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.KSA.Grpc
{
    /// <summary>
    /// A call waiting for the server core to run it.
    /// </summary>
    sealed class PendingCall
    {
        readonly TaskCompletionSource<byte[]> result =
            new TaskCompletionSource<byte[]> (TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingCall (ProcedureSignature procedure, byte[] message)
        {
            Procedure = procedure;
            Message = message;
            Arrived = Stopwatch.GetTimestamp ();
        }

        /// <summary>
        /// The <see cref="Stopwatch.GetTimestamp"/> at which the call arrived.
        /// </summary>
        public long Arrived { get; }

        public ProcedureSignature Procedure { get; }

        public byte[] Message { get; }

        /// <summary>
        /// The decoded request, set on the game thread.
        /// </summary>
        public Request Request { get; set; }

        public Task<byte[]> Result {
            get { return result.Task; }
        }

        public void Complete (byte[] response)
        {
            result.TrySetResult (response);
        }

        public void Fail (StatusCode code, string message, Metadata trailers = null)
        {
            result.TrySetException (new RpcException (new global::Grpc.Core.Status (code, message), trailers ?? new Metadata ()));
        }
    }

    /// <summary>
    /// The client for one HTTP/2 connection.
    /// </summary>
    sealed class GrpcClient : IClient<IMessageStream>
    {
        enum State { Pending, Allowed, Denied, Closed }

        volatile State state = State.Pending;
        readonly GrpcServer server;
        readonly GrpcStream stream;

        public GrpcClient (string name, string address, GrpcServer server)
        {
            Name = name;
            Address = address;
            Guid = Guid.NewGuid ();
            this.server = server;
            stream = new GrpcStream (this);
        }

        public string Name { get; }

        public Guid Guid { get; }

        public string Address { get; }

        public bool Connected {
            get { return state == State.Allowed; }
        }

        public IMessageStream Stream {
            get { return stream; }
        }

        internal GrpcStream GrpcStream {
            get { return stream; }
        }

        /// <summary>
        /// Queue a call. Called from a Kestrel thread.
        /// </summary>
        public void Enqueue (PendingCall call)
        {
            stream.Incoming.Enqueue (call);
            // The game thread may have drained the queue before the call reached it
            if (state == State.Denied || state == State.Closed)
                FailQueued ();
            else
                Arrivals.Set ();
        }

        public void Allow ()
        {
            state = State.Allowed;
        }

        public void Deny ()
        {
            state = State.Denied;
            FailQueued ();
        }

        /// <summary>
        /// Mark the connection as gone. Called from a Kestrel thread; <see cref="End"/>
        /// follows on the game thread.
        /// </summary>
        public void Disconnect ()
        {
            if (state != State.Denied)
                state = State.Closed;
        }

        /// <summary>
        /// End the session, from the server window or the server core. The server removes the
        /// client on its next update.
        /// </summary>
        public void Close ()
        {
            server.Remove (this);
        }

        /// <summary>
        /// Fail the client's calls, and any it makes later.
        /// </summary>
        internal void End ()
        {
            if (state != State.Denied)
                state = State.Closed;
            stream.FailInFlight ();
            FailQueued ();
        }

        void FailQueued ()
        {
            var (code, message) = state == State.Denied
                ? (StatusCode.PermissionDenied, "The server denied the connection")
                : (StatusCode.Unavailable, "The client disconnected");
            while (stream.Incoming.TryDequeue (out var call))
                call.Fail (code, message);
        }

        public bool Equals (IClient<IMessageStream> other)
        {
            return other != null && Guid == other.Guid;
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as IClient<IMessageStream>);
        }

        public override int GetHashCode ()
        {
            return Guid.GetHashCode ();
        }
    }

    /// <summary>
    /// The requests and responses of a client. The server core reads and writes it on the game
    /// thread, one request at a time, so responses arrive in request order.
    /// </summary>
    sealed class GrpcStream : IMessageStream
    {
        readonly GrpcClient client;
        readonly Queue<PendingCall> inFlight = new Queue<PendingCall> ();

        public GrpcStream (GrpcClient client)
        {
            this.client = client;
        }

        public ConcurrentQueue<PendingCall> Incoming { get; } = new ConcurrentQueue<PendingCall> ();

        /// <summary>
        /// Whether a call is ready to read. Decodes the next call, and fails any that do not
        /// decode, as the object store is only safe to use on the game thread.
        /// </summary>
        public bool DataAvailable {
            get {
                while (Incoming.TryPeek (out var call)) {
                    if (call.Request != null)
                        return true;
                    try {
                        call.Request = ProtoCodec.DecodeRequest (call.Procedure, call.Message);
                        return true;
                    } catch (System.Exception e) {
                        Incoming.TryDequeue (out _);
                        call.Fail (StatusCode.InvalidArgument, "Failed to decode the request: " + e.Message);
                    }
                }
                return false;
            }
        }

        public Request Read ()
        {
            if (!DataAvailable || !Incoming.TryDequeue (out var call))
                throw new InvalidOperationException ("No request to read");
            inFlight.Enqueue (call);
            BytesRead += (ulong)call.Message.Length;
            call.Request.Arrived = call.Arrived;
            return call.Request;
        }

        public void Write (Response value)
        {
            if (inFlight.Count == 0)
                return;
            var call = inFlight.Dequeue ();
            try {
                var message = ProcedureMethods.EncodeResponse (call.Procedure, value, out var status);
                if (status != null) {
                    call.Fail (status.Value.Code, status.Value.Message, status.Value.Trailers);
                } else {
                    BytesWritten += (ulong)message.Length;
                    call.Complete (message);
                }
            } catch (System.Exception e) {
                call.Fail (StatusCode.Internal, "Failed to encode the response: " + e.Message);
            }
        }

        public void FailInFlight ()
        {
            while (inFlight.Count > 0)
                inFlight.Dequeue ().Fail (StatusCode.Unavailable, "The client disconnected");
        }

        public void Close ()
        {
            client.Close ();
        }

        public ulong BytesRead { get; private set; }

        public ulong BytesWritten { get; private set; }

        public void ClearStats ()
        {
            BytesRead = 0;
            BytesWritten = 0;
        }

        // The gRPC methods leave out the KRPC service's streams, so no stream updates are sent
        public void Write (StreamUpdate value)
        {
        }

        public void StreamRemoved (ulong id)
        {
        }

        public void Disconnect (DisconnectReason reason, string message)
        {
            client.Close ();
        }
    }
}
