using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace KRPC.Server
{
    /// <summary>
    /// A socket's byte stream, read and written by threads of its own. The reader thread
    /// blocks on the socket and queues what arrives, and the writer thread sends what the
    /// game thread queues. The game thread writes to the socket itself only a small write the
    /// socket reports room for, so a client that stops reading cannot stall the game.
    /// </summary>
    sealed class SocketStream : IByteStream, IHalfClosableStream, IReaderThreadStream
    {
        const int ChunkSize = 64 * 1024;

        /// <summary>
        /// The most bytes queued for a client before a write fails and the connection ends.
        /// </summary>
        internal const long MaxUnsentBytes = 16 * 1024 * 1024;

        /// <summary>
        /// The most received bytes left unread before the reader thread stops reading the
        /// socket, so the transport's flow control holds a client that sends faster than the
        /// game reads.
        /// </summary>
        internal const long MaxUnreadBytes = 1024 * 1024;

        /// <summary>
        /// The most bytes the game thread sends itself, when nothing is queued and the socket
        /// has room. Far below the free space a socket needs to report itself writable, so the
        /// send does not block.
        /// </summary>
        const int MaxDirectBytes = 4096;

        readonly NetworkStream stream;
        readonly Socket socket;

        // Received bytes the game thread has not read, in arrival order. The first chunk has
        // been read up to receivedOffset.
        readonly object receivedLock = new object ();
        readonly Queue<byte[]> received = new Queue<byte[]> ();
        int receivedOffset;
        long receivedBytes;
        bool readEnded;

        // Bytes queued to send, and whether the write side closes once they are sent
        readonly object sendLock = new object ();
        readonly Queue<byte[]> unsent = new Queue<byte[]> ();
        long unsentBytes;
        bool halfCloseQueued;
        bool closed;
        ServerException writeError;

        long bytesRead;
        long bytesWritten;
        volatile Action receivedHandler;

        public SocketStream (NetworkStream innerStream, Socket innerSocket)
        {
            stream = innerStream;
            socket = innerSocket;
            Start (ReadLoop, "read");
            Start (WriteLoop, "write");
        }

        static void Start (ThreadStart body, string role)
        {
            var thread = new Thread (body);
            thread.Name = "kRPC client " + role;
            thread.IsBackground = true;
            thread.Start ();
        }

        /// <summary>
        /// Whether the peer may still send bytes, or sent bytes not yet read. False once the
        /// stream is closed.
        /// </summary>
        public bool Open {
            get {
                lock (receivedLock) {
                    if (closed)
                        return false;
                    return !readEnded || receivedBytes > 0;
                }
            }
        }

        public bool DataAvailable {
            get {
                lock (receivedLock)
                    return receivedBytes > 0;
            }
        }

        /// <summary>
        /// Read the bytes that have arrived, up to <paramref name="size"/>. Returns zero if none
        /// have, and never blocks.
        /// </summary>
        public int Read (byte[] buffer, int offset, int size)
        {
            var read = 0;
            lock (receivedLock) {
                while (read < size && received.Count > 0) {
                    var chunk = received.Peek ();
                    var count = Math.Min (size - read, chunk.Length - receivedOffset);
                    Array.Copy (chunk, receivedOffset, buffer, offset + read, count);
                    read += count;
                    receivedOffset += count;
                    if (receivedOffset == chunk.Length) {
                        received.Dequeue ();
                        receivedOffset = 0;
                    }
                }
                receivedBytes -= read;
                if (read > 0)
                    Monitor.PulseAll (receivedLock);
            }
            return read;
        }

        public void Write (byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException (nameof (buffer));
            Write (buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Queue bytes to send. Throws an exception if the stream has failed or closed, or if
        /// the client has left too many bytes unread.
        /// </summary>
        public void Write (byte[] buffer, int offset, int size)
        {
            if (size == 0)
                return;
            lock (sendLock) {
                if (writeError != null)
                    throw writeError;
                if (closed || halfCloseQueued)
                    throw new ClientDisconnectedException ();
                // Sending here saves waking the writer thread, the common case of a response
                // to a client that reads
                if (unsentBytes == 0 && size <= MaxDirectBytes && Writable ()) {
                    try {
                        stream.Write (buffer, offset, size);
                    } catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException) {
                        writeError = new ServerException (e.Message);
                        throw writeError;
                    }
                    Interlocked.Add (ref bytesWritten, size);
                    return;
                }
            }
            var chunk = new byte [size];
            Array.Copy (buffer, offset, chunk, 0, size);
            lock (sendLock) {
                if (writeError != null)
                    throw writeError;
                if (closed || halfCloseQueued)
                    throw new ClientDisconnectedException ();
                if (unsentBytes + size > MaxUnsentBytes)
                    throw new ServerException ("The client left more than " + MaxUnsentBytes + " bytes unread");
                unsent.Enqueue (chunk);
                unsentBytes += size;
                Monitor.Pulse (sendLock);
            }
        }

        public Action Received {
            set { receivedHandler = value; }
        }

        public ulong BytesRead {
            get { return (ulong)Interlocked.Read (ref bytesRead); }
        }

        public ulong BytesWritten {
            get { return (ulong)Interlocked.Read (ref bytesWritten); }
        }

        public void ClearStats ()
        {
            Interlocked.Exchange (ref bytesRead, 0);
            Interlocked.Exchange (ref bytesWritten, 0);
        }

        /// <summary>
        /// Close the write side once the queued bytes are sent, leaving the read side open.
        /// </summary>
        public void HalfClose ()
        {
            lock (sendLock) {
                if (closed)
                    throw new ClientDisconnectedException ();
                halfCloseQueued = true;
                Monitor.Pulse (sendLock);
            }
        }

        /// <summary>
        /// Close the stream at once, dropping the bytes not yet sent.
        /// </summary>
        public void Close ()
        {
            lock (receivedLock) {
                lock (sendLock) {
                    closed = true;
                    Monitor.Pulse (sendLock);
                }
                Monitor.PulseAll (receivedLock);
            }
            // Closing the socket ends a read blocked on it
            stream.Close ();
            socket.Close ();
        }

        void ReadLoop ()
        {
            var buffer = new byte [ChunkSize];
            try {
                while (true) {
                    lock (receivedLock) {
                        while (receivedBytes >= MaxUnreadBytes && !closed)
                            Monitor.Wait (receivedLock);
                        if (closed)
                            break;
                    }
                    var size = stream.Read (buffer, 0, buffer.Length);
                    if (size <= 0)
                        break;
                    var chunk = new byte [size];
                    Array.Copy (buffer, chunk, size);
                    lock (receivedLock) {
                        received.Enqueue (chunk);
                        receivedBytes += size;
                    }
                    Interlocked.Add (ref bytesRead, size);
                    var handler = receivedHandler;
                    if (handler != null)
                        handler ();
                    Arrivals.Set ();
                }
            } catch (IOException) {
            } catch (ObjectDisposedException) {
            } catch (SocketException) {
            }
            lock (receivedLock)
                readEnded = true;
            Arrivals.Set ();
        }

        void WriteLoop ()
        {
            while (true) {
                byte[] data;
                lock (sendLock) {
                    while (!closed && unsent.Count == 0 && !halfCloseQueued)
                        Monitor.Wait (sendLock);
                    if (closed)
                        return;
                    if (unsent.Count == 0) {
                        ShutdownSend ();
                        return;
                    }
                    data = Take ();
                }
                try {
                    stream.Write (data, 0, data.Length);
                } catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException) {
                    lock (sendLock)
                        writeError = new ServerException (e.Message);
                    return;
                }
                Interlocked.Add (ref bytesWritten, data.Length);
                lock (sendLock)
                    unsentBytes -= data.Length;
            }
        }

        /// <summary>
        /// The queued chunks as one, so that a run of small writes is one send.
        /// </summary>
        byte[] Take ()
        {
            if (unsent.Count == 1)
                return unsent.Dequeue ();
            var data = new byte [unsentBytes];
            var offset = 0;
            while (unsent.Count > 0) {
                var chunk = unsent.Dequeue ();
                Array.Copy (chunk, 0, data, offset, chunk.Length);
                offset += chunk.Length;
            }
            return data;
        }

        bool Writable ()
        {
            try {
                return socket.Poll (0, SelectMode.SelectWrite);
            } catch (SocketException) {
                return false;
            } catch (ObjectDisposedException) {
                return false;
            }
        }

        void ShutdownSend ()
        {
            try {
                socket.Shutdown (SocketShutdown.Send);
            } catch (SocketException) {
            } catch (ObjectDisposedException) {
            }
        }
    }
}
