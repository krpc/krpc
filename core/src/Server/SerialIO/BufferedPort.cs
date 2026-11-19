using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using KRPC.Utils;

namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// A serial port that is read and written by background threads, presenting the data to the
    /// caller through in-memory buffers. Received data is buffered until it is read, and written
    /// data is buffered until the port has sent it, so neither Read nor Write waits on the port.
    ///
    /// A serial port carries one byte at a time at the configured baud rate, which at the default
    /// 9600 baud is about a millisecond per byte, and a write does not finish until the port has
    /// sent every byte of it. The server calls into this class from the thread that runs the game,
    /// so waiting for the port there would hold up the game for the duration of the transfer.
    /// </summary>
    sealed class BufferedPort
    {
        // Time the read thread waits in a read for data before it gives up and checks whether
        // the port is closing
        const int readTimeout = 100;
        // Time Close waits for each background thread to finish before abandoning it.
        const int shutdownTimeout = 500;
        // Number of bytes transferred to or from the port at a time.
        const int chunkSize = 4096;
        // Amount of unread received data at which reading from the port pauses, so that a
        // client sending faster than the game reads cannot grow the buffer without limit
        const int receiveLimit = 1024 * 1024;
        // Amount of unsent data at which a further write fails the port, as the caller is
        // writing faster than the port sends. A write into a buffer under the limit is accepted
        // in full, so one large message still goes out
        internal const int SendLimit = 16 * 1024 * 1024;

        readonly IPort port;
        readonly ByteBuffer received = new ByteBuffer ();
        readonly ByteBuffer sending = new ByteBuffer ();
        readonly object receivedLock = new object ();
        readonly object sendingLock = new object ();
        Thread readThread;
        Thread writeThread;
        bool opened;
        volatile bool closing;
        volatile bool failed;
        long lastArrival;
        volatile Action receivedHandler;

        public BufferedPort (IPort innerPort)
        {
            if (innerPort == null)
                throw new ArgumentNullException (nameof (innerPort));
            port = innerPort;
        }

        public string PortName {
            get { return port.PortName; }
        }

        /// <summary>
        /// Whether the port is open and usable. False once it has been closed, or once one of the
        /// background threads has failed and left the port in an unusable state.
        /// </summary>
        public bool IsOpen {
            get { return !failed && port.IsOpen; }
        }

        /// <summary>
        /// The <see cref="Stopwatch.GetTimestamp"/> at which bytes last arrived.
        /// </summary>
        public long LastArrival {
            get { return Interlocked.Read (ref lastArrival); }
        }

        /// <summary>
        /// Run on the read thread each time bytes arrive, before the server's wake event is set.
        /// </summary>
        public Action Received {
            set { receivedHandler = value; }
        }

        /// <summary>
        /// Number of bytes that have been received from the port and not yet read.
        /// </summary>
        public int BytesAvailable {
            get {
                CheckFailed ();
                lock (receivedLock)
                    return received.Length;
            }
        }

        /// <summary>
        /// Open the port and start transferring data to and from it. A port can only be opened
        /// once: a background thread that outlives a Close, because it was stuck in a transfer,
        /// could otherwise fail the reopened port. Create a new instance to open the port again.
        /// </summary>
        public void Open ()
        {
            if (opened)
                throw new InvalidOperationException (
                    "The serial port " + port.PortName + " cannot be opened again");
            opened = true;
            port.ReadTimeout = readTimeout;
            port.Open ();
            // Discard stale data from the port
            port.DiscardInBuffer ();
            readThread = StartThread (ReadLoop, "read");
            writeThread = StartThread (WriteLoop, "write");
        }

        /// <summary>
        /// Stop transferring data and close the port. Data already given to Write is sent first,
        /// unless the port takes too long about it.
        /// </summary>
        public void Close ()
        {
            closing = true;
            lock (receivedLock)
                Monitor.PulseAll (receivedLock);
            lock (sendingLock)
                Monitor.PulseAll (sendingLock);
            // Wait for the write thread first, so that it can send what is left in the send buffer
            JoinThread (writeThread);
            // A write still waiting on the port, such as one held by flow control, is released.
            // The rest of the send buffer is dropped, the port's output is discarded, and flow
            // control is turned off so that the write completes. Closing the port does not end
            // a native write blocked on it, and the game cannot exit while one is blocked
            var writeStuck = writeThread != null && writeThread.IsAlive;
            lock (sendingLock)
                sending.Clear ();
            if (writeStuck) {
                try {
                    port.DiscardOutBuffer ();
                    port.DisableFlowControl ();
                } catch (Exception exn) when (exn is IOException || exn is InvalidOperationException) {
                    // The close goes on, so that the port and the read thread are still closed
                    Logger.WriteLine (
                        "SerialIO: failed to release a held write on " + port.PortName + "; " + exn.Message,
                        Logger.Severity.Debug);
                }
                JoinThread (writeThread);
            }
            // A read that waits with a timeout ends at it. The port closes only after that, as
            // a read still waiting on a closed port could take the bytes of another file that
            // reuses its descriptor. Closing the port ends a read that waits with no timeout.
            if (readThread != null)
                readThread.Join (2 * readTimeout);
            port.Close ();
            JoinThread (readThread);
            writeThread = null;
            readThread = null;
            lock (receivedLock)
                received.Clear ();
        }

        /// <summary>
        /// Read up to size bytes of received data into buffer, starting at offset. Returns the
        /// number of bytes read, which is zero when nothing has been received.
        /// </summary>
        public int Read (byte[] buffer, int offset, int size)
        {
            CheckFailed ();
            lock (receivedLock) {
                var read = received.Take (buffer, offset, size);
                // Reading may have taken the buffer back below the limit at which the read
                // thread pauses, so signal that there is room again
                if (read > 0)
                    Monitor.PulseAll (receivedLock);
                return read;
            }
        }

        /// <summary>
        /// Queue size bytes from buffer, starting at offset, to be sent over the port. Fails the
        /// port when the data already queued has reached the send limit, which means data is
        /// being written faster than the port can carry it.
        /// </summary>
        public void Write (byte[] buffer, int offset, int size)
        {
            CheckFailed ();
            lock (sendingLock) {
                if (sending.Length >= SendLimit) {
                    var exn = new IOException (
                        "Data is being written to the serial port " + port.PortName +
                        " faster than the port can send it");
                    Fail (exn);
                    throw exn;
                }
                sending.Append (buffer, offset, size);
                Monitor.PulseAll (sendingLock);
            }
        }

        Thread StartThread (ThreadStart body, string role)
        {
            var thread = new Thread (body);
            thread.Name = "kRPC SerialIO " + role + " " + port.PortName;
            // The threads only move data for a server that the game owns, so they must not
            // keep the process alive
            thread.IsBackground = true;
            thread.Start ();
            return thread;
        }

        static void JoinThread (Thread thread)
        {
            if (thread != null)
                thread.Join (shutdownTimeout);
        }

        void ReadLoop ()
        {
            var buffer = new byte [chunkSize];
            while (!closing && !failed) {
                lock (receivedLock) {
                    while (!closing && !failed && received.Length >= receiveLimit)
                        Monitor.Wait (receivedLock, readTimeout);
                }
                if (closing || failed)
                    return;
                int size;
                try {
                    size = port.Read (buffer, 0, buffer.Length);
                } catch (TimeoutException) {
                    // Nothing arrived
                    continue;
                } catch (System.Exception exn) {
                    // A read ended by closing the port is not a failure
                    if (!closing)
                        Fail (exn);
                    return;
                }
                if (size <= 0) {
                    // A read that returns nothing means the other end has gone. Wait before
                    // reading again
                    Thread.Sleep (readTimeout);
                    continue;
                }
                Interlocked.Exchange (ref lastArrival, Stopwatch.GetTimestamp ());
                lock (receivedLock)
                    received.Append (buffer, 0, size);
                var handler = receivedHandler;
                if (handler != null)
                    handler ();
                Arrivals.Set ();
            }
        }

        void WriteLoop ()
        {
            var buffer = new byte [chunkSize];
            while (!failed) {
                int size;
                lock (sendingLock) {
                    while (sending.Length == 0) {
                        if (closing || failed)
                            return;
                        Monitor.Wait (sendingLock);
                    }
                    size = sending.Take (buffer, 0, buffer.Length);
                }
                try {
                    port.Write (buffer, 0, size);
                } catch (System.Exception exn) {
                    // A write ended by closing the port is not a failure
                    if (!closing)
                        Fail (exn);
                    return;
                }
            }
        }

        /// <summary>
        /// Record that the port can no longer be used, and wake anything waiting on it.
        /// </summary>
        void Fail (System.Exception exn)
        {
            failed = true;
            Logger.WriteLine (
                "SerialIO: transfer on " + port.PortName + " failed; " + exn, Logger.Severity.Error);
            lock (receivedLock)
                Monitor.PulseAll (receivedLock);
            lock (sendingLock)
                Monitor.PulseAll (sendingLock);
        }

        void CheckFailed ()
        {
            if (failed)
                throw new IOException ("The serial port " + port.PortName + " is no longer usable");
        }
    }
}
