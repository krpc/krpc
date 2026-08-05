using System;
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
        // How often the read thread checks the port for received data. Reads are only issued for
        // data the port already holds, so that they return promptly without engaging the port's
        // timeout handling: a read left waiting for data that arrives just as the read gives up
        // can lose that data, depending on the port implementation.
        const int pollInterval = 10;
        // How long a read is allowed to take before it fails with TimeoutException. Reads are only
        // issued for data the port already holds, so this is a guard against a port that
        // misreports how much it has, which would otherwise leave the read thread stuck in a read
        // that never returns.
        const int readTimeout = 100;
        // How long Close waits for each background thread to finish before abandoning it.
        const int shutdownTimeout = 500;
        // Number of bytes transferred to or from the port at a time.
        const int chunkSize = 4096;
        // Amount of unread received data at which reading from the port pauses, so that a client
        // sending faster than the game reads cannot grow the buffer without limit. The data stays
        // in the port's own buffer until the game catches up.
        const int receiveLimit = 1024 * 1024;
        // Amount of unsent written data at which the port is failed. A caller that writes faster
        // than the configured baud rate for long enough would otherwise grow the buffer without
        // limit, while the data waiting in it grows ever staler. Unlike reading, which can pause
        // and leave data with the port, written data has nowhere else to live, so the connection
        // is dropped instead.
        const int sendLimit = 1024 * 1024;

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
            // Wait for the write thread first, so that it gets the chance to send what is left in
            // the send buffer. A thread that does not finish in time is left to the port being
            // closed under it, which fails its transfer and ends it.
            JoinThread (writeThread);
            JoinThread (readThread);
            writeThread = null;
            readThread = null;
            lock (receivedLock)
                received.Clear ();
            lock (sendingLock)
                sending.Clear ();
            port.Close ();
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
                // Reading may have taken the buffer back below the limit at which the read thread
                // pauses, so let it know there is room again.
                if (read > 0)
                    Monitor.PulseAll (receivedLock);
                return read;
            }
        }

        /// <summary>
        /// Queue size bytes from buffer, starting at offset, to be sent over the port. Fails the
        /// port when the amount of queued data reaches the send limit, which means data is being
        /// written faster than the port can carry it.
        /// </summary>
        public void Write (byte[] buffer, int offset, int size)
        {
            CheckFailed ();
            lock (sendingLock) {
                if (sending.Length + size > sendLimit) {
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
            // The threads only move data for a server that the game owns, so they must never be
            // what keeps the process alive.
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
                    if (port.BytesToRead == 0) {
                        Thread.Sleep (pollInterval);
                        continue;
                    }
                    size = port.Read (buffer, 0, buffer.Length);
                } catch (TimeoutException) {
                    // The port did not hand over the data it reported holding
                    continue;
                } catch (System.Exception exn) {
                    Fail (exn);
                    return;
                }
                if (size <= 0) {
                    // The port reports itself readable but has nothing to give, which means the
                    // other end has gone. Wait rather than ask again immediately.
                    Thread.Sleep (readTimeout);
                    continue;
                }
                lock (receivedLock)
                    received.Append (buffer, 0, size);
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
