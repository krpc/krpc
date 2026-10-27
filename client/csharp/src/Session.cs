using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// The messages of one connection. One thread at a time reads the socket, the reader. A
    /// caller with no reader takes the role and reads until its response arrives, so a
    /// call costs no hand-off between threads. While streams are open, the reader thread reads
    /// when no call is waiting for a response, so their updates arrive between calls. The reader
    /// thread waits for data without the reader role, so a caller can take over at once. The
    /// reader never runs user code, and takes no lock user code can hold. The dispatcher thread
    /// runs callbacks, wakes stream waiters, and makes the writes the reader needs, in the order
    /// the reader queues them.
    /// </summary>
    sealed class Session
    {
        // A message that has started to arrive may go this long without a new byte, by default
        internal static readonly TimeSpan DefaultStallTimeout = TimeSpan.FromSeconds (30);
        // The wait for the server to close its side once ours is closed
        static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds (1);

        // A call waiting for its response. StreamType is the type a call that returns a stream
        // or an event returns. Changed under the state lock.
        sealed class Slot
        {
            public System.Type StreamType;
            public bool Abandoned;
            public volatile bool Done;
            public Response Response;
            public System.Exception Error;
        }

        // Thrown once the connection has ended, to stop the thread reading it
        sealed class EndedException : System.Exception
        {
        }

        readonly Socket socket;
        readonly NetworkStream networkStream;
        readonly CodedOutputStream output;
        readonly MessageReader reader;
        readonly Func<ulong, Request> removeStreamRequest;
        StreamManager manager;
        // Held for the whole of a write
        readonly object writeLock = new object ();
        // Guards the ids, the pending calls, the error, and which thread is reading. Pulsed when a
        // call completes, when the socket is free to read, and when the reader thread is to read.
        readonly object stateLock = new object ();
        ulong nextId = 1;
        readonly IDictionary<ulong, Slot> pending = new Dictionary<ulong, Slot> ();
        System.Exception error;
        // The error that failed the connection before it closed
        System.Exception failure;
        bool closing;
        bool ended;
        // Whether a thread is reading the socket, the number of calls waiting for their
        // response, and the number of those waiting for another thread to read it
        bool reading;
        int callers;
        int waiting;
        readonly ManualResetEvent halfClosed = new ManualResetEvent (false);
        readonly Queue<Action> queue = new Queue<Action> ();
        bool queueStopped;
        // Timeout.InfiniteTimeSpan waits forever
        readonly TimeSpan stallTimeout;
        readonly Thread readerThread;
        readonly Thread dispatcherThread;
        // Whether Enter caught an interrupt on this thread, to raise once the call returns
        [ThreadStatic]
        static bool deferredInterrupt;

        public Session (Socket connectedSocket, NetworkStream stream, MessageReader messageReader,
                        Func<ulong, Request> removeStream, TimeSpan stall)
        {
            stallTimeout = stall;
            socket = connectedSocket;
            networkStream = stream;
            output = new CodedOutputStream (networkStream, true);
            reader = messageReader;
            removeStreamRequest = removeStream;
            readerThread = new Thread (RunReader) { IsBackground = true, Name = "krpc-reader" };
            dispatcherThread = new Thread (RunDispatcher) { IsBackground = true, Name = "krpc-dispatcher" };
        }

        public void Start (StreamManager streamManager)
        {
            manager = streamManager;
            readerThread.Start ();
            dispatcherThread.Start ();
        }

        /// <summary>
        /// Whether this thread is the dispatcher thread, on which callbacks run.
        /// </summary>
        public bool OnDispatcherThread {
            get { return Thread.CurrentThread == dispatcherThread; }
        }

        internal Socket Socket {
            get { return socket; }
        }

        /// <summary>
        /// Queue work for the dispatcher thread.
        /// </summary>
        public void Dispatch (Action work)
        {
            Enter (queue);
            try {
                if (queueStopped)
                    return;
                queue.Enqueue (work);
                Monitor.Pulse (queue);
            } finally {
                Monitor.Exit (queue);
            }
        }

        /// <summary>
        /// Take a lock, deferring an interrupt until the call returns. The reader takes its locks
        /// this way, so a message it has read is handled in full.
        /// </summary>
        internal static void Enter (object obj)
        {
            while (true) {
                var taken = false;
                try {
                    Monitor.Enter (obj, ref taken);
                    return;
                } catch (ThreadInterruptedException) {
                    deferredInterrupt = true;
                    if (taken)
                        return;
                }
            }
        }

        /// <summary>
        /// Take an interrupt pending on this thread, to raise once a close returns, so the
        /// close waits and joins as any close does.
        /// </summary>
        internal static void DeferPendingInterrupt ()
        {
            try {
                Thread.Sleep (0);
            } catch (ThreadInterruptedException) {
                deferredInterrupt = true;
            }
        }

        internal static void RaiseDeferredInterrupt ()
        {
            if (deferredInterrupt) {
                deferredInterrupt = false;
                Thread.CurrentThread.Interrupt ();
            }
        }

        void StopDispatcher ()
        {
            lock (queue) {
                queueStopped = true;
                Monitor.Pulse (queue);
            }
        }

        void RunDispatcher ()
        {
            while (true) {
                Action work;
                lock (queue) {
                    while (queue.Count == 0) {
                        if (queueStopped)
                            return;
                        Monitor.Wait (queue);
                    }
                    work = queue.Dequeue ();
                }
                try {
                    work ();
                } catch (System.Exception exn) {
                    Console.Error.WriteLine ("Exception on the kRPC dispatcher thread: " + exn);
                }
            }
        }

        bool StreamsOpen {
            get { return manager != null && manager.HasStreams; }
        }

        /// <summary>
        /// Send a request and wait for its response. Throws the connection error if the
        /// connection has failed or fails before the response arrives.
        /// </summary>
        public Response Call (Request request, System.Type streamType)
        {
            var slot = new Slot { StreamType = streamType };
            bool isReader;
            // The slot is added before the request is written, so a fast response finds it. A
            // caller that finds no thread reading becomes the reader in the same hold of the lock.
            lock (stateLock) {
                if (error != null)
                    throw error;
                request.Id = nextId;
                nextId++;
                pending [request.Id] = slot;
                callers++;
                isReader = !reading;
                if (isReader)
                    reading = true;
            }
            var returned = false;
            try {
                WriteRequest (request);
                if (isReader)
                    returned = ReadUntil (slot, true);
                else
                    Await (slot);
            } catch (ThreadInterruptedException) {
                // A reader discards the response when it arrives
                Enter (stateLock);
                try {
                    slot.Abandoned = true;
                } finally {
                    Monitor.Exit (stateLock);
                }
                throw;
            } finally {
                if (!returned) {
                    Enter (stateLock);
                    try {
                        if (isReader)
                            reading = false;
                        Returned ();
                    } finally {
                        Monitor.Exit (stateLock);
                    }
                }
                RaiseDeferredInterrupt ();
            }
            if (slot.Error != null)
                throw slot.Error;
            return slot.Response;
        }

        /// <summary>
        /// Wake the reader thread for a stream added outside a call.
        /// </summary>
        internal void StreamAdded ()
        {
            lock (stateLock) {
                if (callers == 0)
                    Monitor.PulseAll (stateLock);
            }
        }

        /// <summary>
        /// Note a call has stopped waiting, and wake the thread to read next. Called with the
        /// state lock held.
        /// </summary>
        void Returned ()
        {
            callers--;
            if (waiting > 0 || (callers == 0 && StreamsOpen))
                Monitor.PulseAll (stateLock);
        }

        /// <summary>
        /// Read messages until a call's response arrives, as the reader, then handle the whole
        /// messages already read. The call's own response is handled here, which is the common
        /// case. If returning and no whole message is buffered, the call stops reading and stops
        /// waiting in the same hold of the lock, and this returns true.
        /// </summary>
        bool ReadUntil (Slot slot, bool returning)
        {
            try {
                while (!slot.Done) {
                    var response = ReceiveResponse ();
                    if (response == null)
                        continue;
                    Enter (stateLock);
                    try {
                        Slot found;
                        if (pending.TryGetValue (response.Id, out found) && found == slot && slot.StreamType == null) {
                            pending.Remove (response.Id);
                            slot.Response = response;
                            slot.Done = true;
                            if (returning && !reader.HasMessage) {
                                reading = false;
                                Returned ();
                                return true;
                            }
                            continue;
                        }
                    } finally {
                        Monitor.Exit (stateLock);
                    }
                    HandleResponse (response);
                }
                HandleBuffered ();
            } catch (EndedException) {
            }
            return false;
        }

        /// <summary>
        /// Handle the whole messages already read, as the reader, before it stops reading. The
        /// buffer then holds at most part of a message, and the rest of it wakes the reader
        /// thread's poll. Throws EndedException once the connection has ended.
        /// </summary>
        void HandleBuffered ()
        {
            while (reader.HasMessage) {
                // A failed connection has no pending calls to match responses to
                Enter (stateLock);
                try {
                    if (error != null)
                        return;
                } finally {
                    Monitor.Exit (stateLock);
                }
                var message = Receive ();
                if (message.ContentCase == Message.ContentOneofCase.Response)
                    HandleResponse (message.Response);
                else
                    Handle (message);
            }
        }

        /// <summary>
        /// Wait for a call's response, reading the socket while no other thread is.
        /// </summary>
        void Await (Slot slot)
        {
            lock (stateLock) {
                while (!slot.Done && reading) {
                    waiting++;
                    try {
                        Monitor.Wait (stateLock);
                    } finally {
                        waiting--;
                    }
                }
                if (slot.Done)
                    return;
                reading = true;
            }
            try {
                ReadUntil (slot, false);
            } finally {
                StopReading ();
            }
        }

        void StopReading ()
        {
            Enter (stateLock);
            try {
                reading = false;
                if (waiting > 0 || (callers == 0 && StreamsOpen))
                    Monitor.PulseAll (stateLock);
            } finally {
                Monitor.Exit (stateLock);
            }
        }

        /// <summary>
        /// Queue a request to remove a stream on the server, whose response is discarded.
        /// </summary>
        public void RemoveStream (ulong id)
        {
            Dispatch (() => {
                var slot = new Slot { Abandoned = true };
                var request = removeStreamRequest (id);
                lock (stateLock) {
                    if (error != null)
                        return;
                    request.Id = nextId;
                    nextId++;
                    pending [request.Id] = slot;
                }
                WriteRequest (request);
            });
        }

        /// <summary>
        /// Write a call's request. A failed write ends the connection.
        /// </summary>
        void WriteRequest (Request request)
        {
            try {
                lock (writeLock) {
                    WriteFault.Reach ();
                    // The Message is written around the request, which saves building one
                    var size = request.CalculateSize ();
                    output.WriteLength (1 + CodedOutputStream.ComputeLengthSize (size) + size);
                    output.WriteTag (Message.RequestFieldNumber, WireFormat.WireType.LengthDelimited);
                    output.WriteLength (size);
                    request.WriteTo (output);
                    output.Flush ();
                }
            } catch (System.Exception exn) when (exn is IOException || exn is SocketException || exn is ObjectDisposedException) {
                End (Failure (exn));
            }
        }

        void Write (Message message)
        {
            WriteFault.Reach ();
            output.WriteLength (message.CalculateSize ());
            message.WriteTo (output);
            output.Flush ();
        }

        /// <summary>
        /// Tell the server the connection is closing, then close it. Waits for the server to
        /// close its side, for up to a second, discarding what arrives meanwhile. Skips telling
        /// the server if the write lock is held for longer than that, such as by a caller blocked
        /// on a full send buffer.
        /// </summary>
        public void Close ()
        {
            bool failed;
            lock (stateLock) {
                closing = true;
                failed = error != null;
                failure = error;
                // A response that arrives after the disconnect does not reach its call
                Fail (new ConnectionException ("Connection closed"));
            }
            if (!failed && Monitor.TryEnter (writeLock, CloseTimeout)) {
                try {
                    Write (new Message { Disconnect = new Disconnect { Reason = Disconnect.Types.Reason.Closed } });
                    socket.Shutdown (SocketShutdown.Send);
                } catch (System.Exception) {
                    // The connection can already have failed
                } finally {
                    Monitor.Exit (writeLock);
                }
            }
            var deadline = DateTime.UtcNow + CloseTimeout;
            bool drain;
            // A thread already reading finds the server's close itself
            lock (stateLock) {
                while (reading && !ended) {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;
                    // Counted as waiting, so a caller that stops reading wakes this thread
                    waiting++;
                    try {
                        Monitor.Wait (stateLock, remaining);
                    } finally {
                        waiting--;
                    }
                }
                drain = !reading && !ended;
                if (drain)
                    reading = true;
            }
            if (drain)
                Drain (deadline);
            CloseSocket ();
            Fail (new ConnectionException ("Connection closed"));
            lock (stateLock) {
                if (drain)
                    reading = false;
                ended = true;
                Monitor.PulseAll (stateLock);
            }
        }

        /// <summary>
        /// Discard what arrives until the server closes the connection, or until the deadline.
        /// </summary>
        void Drain (DateTime deadline)
        {
            var discard = new byte [4096];
            try {
                while (true) {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        return;
                    if (!socket.Poll ((int)(remaining.TotalMilliseconds * 1000), SelectMode.SelectRead))
                        return;
                    if (socket.Receive (discard) == 0)
                        return;
                }
            } catch (System.Exception) {
            }
        }

        /// <summary>
        /// Stop the threads, and wait for them except the one this runs on.
        /// </summary>
        public void Join (bool joinDispatcher)
        {
            StopDispatcher ();
            if (Thread.CurrentThread != readerThread)
                readerThread.Join ();
            if (joinDispatcher && Thread.CurrentThread != dispatcherThread)
                dispatcherThread.Join ();
        }

        void CloseSocket ()
        {
            try {
                socket.Shutdown (SocketShutdown.Both);
            } catch (System.Exception) {
            }
            socket.Close ();
        }

        /// <summary>
        /// Fail the connection, and every call waiting for a response.
        /// </summary>
        void Fail (System.Exception exn)
        {
            Enter (stateLock);
            try {
                if (error == null)
                    error = exn;
                foreach (var slot in pending.Values) {
                    slot.Error = error;
                    slot.Done = true;
                }
                pending.Clear ();
                Monitor.PulseAll (stateLock);
            } finally {
                Monitor.Exit (stateLock);
            }
        }

        /// <summary>
        /// End the connection, once a read or write finds it has ended. Returns the
        /// EndedException a reader throws.
        /// </summary>
        /// <summary>
        /// The cause of the error that failed the connection before it closed, or null if it
        /// had not failed. The streams end with this as their ending's cause.
        /// </summary>
        public System.Exception FailureCause {
            get {
                lock (stateLock) {
                    return failure == null ? null : EndingCause (failure);
                }
            }
        }

        /// <summary>
        /// The cause of a stream ending, for the error that failed the connection.
        /// </summary>
        static System.Exception EndingCause (System.Exception failure)
        {
            return failure.InnerException ?? failure;
        }

        EndedException End (System.Exception cause)
        {
            if (cause == null)
                cause = new ConnectionException ("Connection closed");
            bool wasClosing;
            // Failed in the same hold, so a thread that finds the connection ended finds its call
            // failed
            Enter (stateLock);
            try {
                if (ended)
                    return new EndedException ();
                ended = true;
                wasClosing = closing;
                Fail (cause);
                // The first failure, which the calls throw
                cause = error;
            } finally {
                Monitor.Exit (stateLock);
            }
            // The closing thread ends the streams itself. Otherwise their ending's cause is what
            // ended the connection.
            if (!wasClosing && manager != null) {
                var endingCause = EndingCause (cause);
                Dispatch (() => manager.NotifyClosed (endingCause));
                StopDispatcher ();
            }
            CloseSocket ();
            Enter (stateLock);
            try {
                Monitor.PulseAll (stateLock);
            } finally {
                Monitor.Exit (stateLock);
            }
            return new EndedException ();
        }

        /// <summary>
        /// Read messages while streams are open and no call is waiting for a response.
        /// </summary>
        void RunReader ()
        {
            while (true) {
                lock (stateLock) {
                    while ((callers > 0 || reading || closing || !StreamsOpen) && !ended)
                        Monitor.Wait (stateLock);
                    if (ended)
                        return;
                }
                // Wait for data without being the reader, so a call can take over. The buffer
                // holds at most part of a message, as the reader handles the whole ones before
                // it stops reading. The wait is bounded by the stall clock, so that part is
                // timed.
                var wait = stallTimeout;
                if (wait != Timeout.InfiniteTimeSpan && reader.HasBuffered)
                    wait = reader.StallRemaining (wait);
                var readable = Readable (wait);
                lock (stateLock) {
                    if (ended)
                        return;
                    if (callers > 0 || reading || closing)
                        continue;
                    reading = true;
                }
                try {
                    // A call can read the data between the wait and taking the role
                    if (!reader.HasBuffered && !(readable && Readable (TimeSpan.Zero)))
                        continue;
                    var message = Receive ();
                    if (message.ContentCase == Message.ContentOneofCase.Response)
                        HandleResponse (message.Response);
                    else
                        Handle (message);
                    HandleBuffered ();
                } catch (EndedException) {
                    return;
                } finally {
                    StopReading ();
                }
            }
        }

        /// <summary>
        /// Whether the socket has data to read, or has closed, within the wait. True if the
        /// poll fails, so that the read reports the failure.
        /// </summary>
        bool Readable (TimeSpan wait)
        {
            try {
                var microseconds = wait == Timeout.InfiniteTimeSpan ? -1 :
                    (int)Math.Min (wait.TotalMilliseconds * 1000, int.MaxValue);
                return socket.Poll (microseconds, SelectMode.SelectRead);
            } catch (System.Exception) {
                return true;
            }
        }

        /// <summary>
        /// Read one message. Throws EndedException once the connection has ended.
        /// </summary>
        Message Receive ()
        {
            Read ();
            return Parse ();
        }

        /// <summary>
        /// Read the bytes of one message into the reader's buffer. Throws EndedException once
        /// the connection has ended.
        /// </summary>
        void Read ()
        {
            System.Exception cause;
            try {
                reader.Read (Timeout.InfiniteTimeSpan, stallTimeout);
                return;
            } catch (TimeoutException) {
                cause = ProtocolError (
                    Disconnect.Types.Reason.Timeout,
                    "A message from the server stalled for more than " +
                    stallTimeout.TotalSeconds.ToString (CultureInfo.InvariantCulture) + " seconds", null);
            } catch (InvalidDataException exn) {
                cause = ProtocolError (Disconnect.Types.Reason.MalformedMessage, exn.Message, exn);
            } catch (System.Exception exn) when (exn is IOException || exn is SocketException || exn is ObjectDisposedException) {
                cause = Failure (exn);
            }
            throw End (cause);
        }

        /// <summary>
        /// The cause of a failed read or write, or null if the connection is closing.
        /// </summary>
        System.Exception Failure (System.Exception exn)
        {
            Enter (stateLock);
            try {
                return closing ? null : new ConnectionException ("Connection failed: " + exn.Message, exn);
            } finally {
                Monitor.Exit (stateLock);
            }
        }

        /// <summary>
        /// Parse the message last read. Throws EndedException if it is malformed, which ends the
        /// connection.
        /// </summary>
        Message Parse ()
        {
            try {
                return Message.Parser.ParseFrom (reader.Buffer, reader.Offset, reader.Size);
            } catch (InvalidProtocolBufferException exn) {
                throw End (ProtocolError (
                    Disconnect.Types.Reason.MalformedMessage,
                    "Failed to parse a message from the server: " + exn.Message, exn));
            }
        }

        /// <summary>
        /// Read one message, and return the response it holds, or handle it and return null if
        /// it holds anything else. Throws EndedException once the connection has ended.
        /// </summary>
        Response ReceiveResponse ()
        {
            Read ();
            // A Message holding a response is the tag of field 2, then the response's size and
            // bytes, so the response is parsed where it lies
            var buffer = reader.Buffer;
            var offset = reader.Offset;
            var size = reader.Size;
            if (size >= 2 && buffer [offset] == 0x12 && buffer [offset + 1] < 0x80 && buffer [offset + 1] == size - 2) {
                try {
                    return Response.Parser.ParseFrom (buffer, offset + 2, size - 2);
                } catch (InvalidProtocolBufferException exn) {
                    throw End (ProtocolError (
                        Disconnect.Types.Reason.MalformedMessage,
                        "Failed to parse a message from the server: " + exn.Message, exn));
                }
            }
            var message = Parse ();
            if (message.ContentCase == Message.ContentOneofCase.Response)
                return message.Response;
            Handle (message);
            return null;
        }

        /// <summary>
        /// Handle a message other than a response. Throws EndedException once the connection has
        /// ended.
        /// </summary>
        void Handle (Message message)
        {
            switch (message.ContentCase) {
            case Message.ContentOneofCase.StreamUpdate:
                manager.Update (message.StreamUpdate.Results);
                break;
            case Message.ContentOneofCase.Disconnect:
                throw End (Disconnected (message.Disconnect));
            case Message.ContentOneofCase.None:
                break;
            default:
                throw End (ProtocolError (
                    Disconnect.Types.Reason.ProtocolError,
                    "Unexpected " + message.ContentCase + " message from the server", null));
            }
        }

        /// <summary>
        /// Handle a response. Throws EndedException if its id is unknown, which ends the
        /// connection.
        /// </summary>
        void HandleResponse (Response response)
        {
            Slot slot;
            bool abandoned;
            bool failed;
            Enter (stateLock);
            try {
                if (pending.TryGetValue (response.Id, out slot))
                    pending.Remove (response.Id);
                abandoned = slot != null && slot.Abandoned;
                failed = error != null;
            } finally {
                Monitor.Exit (stateLock);
            }
            // A failure on another thread clears the pending calls
            if (slot == null && failed)
                throw new EndedException ();
            if (slot == null)
                throw End (ProtocolError (
                    Disconnect.Types.Reason.ProtocolError,
                    "Response with an unknown id " + response.Id + " from the server", null));
            ulong id;
            if (StreamId (slot, response, out id)) {
                if (abandoned)
                    // Removed on the server, as no handle owns it
                    RemoveStream (id);
                else
                    // Registered before the next message is read, which the stream's first
                    // update follows
                    manager.Register (id);
            }
            if (!abandoned) {
                Enter (stateLock);
                try {
                    slot.Response = response;
                    slot.Done = true;
                    if (waiting > 0)
                        Monitor.PulseAll (stateLock);
                } finally {
                    Monitor.Exit (stateLock);
                }
            }
        }

        /// <summary>
        /// The id of the stream a call that returns a stream or an event added.
        /// </summary>
        static bool StreamId (Slot slot, Response response, out ulong id)
        {
            id = 0;
            if (slot.StreamType == null || response.Error != null || response.Results.Count == 0)
                return false;
            var result = response.Results [0];
            if (result.Error != null || result.IsNull)
                return false;
            try {
                var stream = slot.StreamType == typeof (Schema.KRPC.Stream)
                    ? Schema.KRPC.Stream.Parser.ParseFrom (result.Value)
                    : Schema.KRPC.Event.Parser.ParseFrom (result.Value).Stream;
                if (stream == null)
                    return false;
                id = stream.Id;
                return true;
            } catch (InvalidProtocolBufferException) {
                // The call has succeeded, and its caller decodes the value
                return false;
            }
        }

        static System.Exception Disconnected (Disconnect disconnect)
        {
            var text = "The server ended the connection (" + disconnect.Reason + ")";
            if (disconnect.Message.Length > 0)
                text += ": " + disconnect.Message;
            return new ConnectionException (text) { Reason = disconnect.Reason };
        }

        /// <summary>
        /// Fail the connection, have the dispatcher tell the server why, and drain the socket
        /// until the server closes it, for up to a second after the half-close.
        /// </summary>
        System.Exception ProtocolError (Disconnect.Types.Reason reason, string text, System.Exception inner)
        {
            var failure = inner == null ? new ConnectionException (text) : new ConnectionException (text, inner);
            Fail (failure);
            Action disconnect = () => {
                try {
                    lock (writeLock) {
                        Write (new Message { Disconnect = new Disconnect { Reason = reason, Message = text } });
                        socket.Shutdown (SocketShutdown.Send);
                    }
                } catch (System.Exception) {
                } finally {
                    halfClosed.Set ();
                }
            };
            // A call from a callback reads on the dispatcher thread, so we write the disconnect here
            if (OnDispatcherThread) {
                disconnect ();
            } else {
                Dispatch (disconnect);
                try {
                    halfClosed.WaitOne (CloseTimeout);
                } catch (ThreadInterruptedException) {
                    deferredInterrupt = true;
                }
            }
            Drain (DateTime.UtcNow + CloseTimeout);
            return failure;
        }
    }
}
