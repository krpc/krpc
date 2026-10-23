using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace KRPC.Client
{
    /// <summary>
    /// Reads length prefixed messages from a stream, a block at a time.
    /// </summary>
    /// <remarks>
    /// The server puts the length of a message in front of it as a varint. Taken straight from
    /// the stream, finding out how long a message is costs a read for every byte of that prefix
    /// and the message itself costs another, so a response that has already arrived whole still
    /// takes several reads to pick up. Buffering the stream instead means a read takes whatever
    /// has arrived, and messages are then taken out of the buffer; what came along with one is
    /// already in hand when the next is asked for. A message is parsed where it lies, so nothing
    /// is copied out of the buffer on the way.
    /// </remarks>
    sealed class MessageReader
    {
        // Big enough that anything a server sends arrives in one read.
        const int InitialSize = 1024 * 1024;

        // The extra room to make when a message does not fit in the buffer.
        const int IncreaseSize = 512 * 1024;

        // A length prefix is a varint, and one that does not end within five bytes cannot be the
        // uint32 it is meant to be.
        const int MaximumPrefixLength = 5;

        readonly Stream stream;
        readonly Socket socket;
        byte[] buffer = new byte [InitialSize];

        // The bytes read but not yet handed out, as the half open range [start, end) of the
        // buffer.
        int start;
        int end;

        public MessageReader (Stream messageStream, Socket pollSocket = null)
        {
            stream = messageStream;
            socket = pollSocket;
        }

        /// <summary>
        /// The buffer holding the message last read, where in it that message starts and how
        /// long it is. They describe that message until the next one is read.
        /// </summary>
        public byte[] Buffer {
            get { return buffer; }
        }

        /// <summary>
        /// See <see cref="Buffer"/>.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// See <see cref="Buffer"/>.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Whether a whole message has been read that is not yet handed out. True for a
        /// malformed length prefix, so the next read throws.
        /// </summary>
        public bool HasMessage {
            get {
                if (end == start)
                    return false;
                int size;
                int prefix;
                try {
                    if (!Prefix (out size, out prefix))
                        return false;
                } catch (InvalidDataException) {
                    return true;
                }
                return end - start - prefix >= size;
            }
        }

        /// <summary>
        /// Read the next message, waiting for as long as it takes.
        /// </summary>
        public void Read ()
        {
            Read (Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Read the next message. Throws a TimeoutException if no whole message arrives within
        /// the timeout, or if a message that has started to arrive receives no new byte within
        /// the stall timeout. The timeouts need the socket the reader was given.
        /// </summary>
        public void Read (TimeSpan timeout, TimeSpan stallTimeout)
        {
            deadline = timeout == Timeout.InfiniteTimeSpan ? null : (DateTime?)(DateTime.UtcNow + timeout);
            stall = stallTimeout;
            int size;
            int prefix;
            while (!Prefix (out size, out prefix))
                Fill ();
            // The prefix and the message together, so that neither making room for the message
            // nor reading the rest of it moves the buffer once the message has been found in it.
            Reserve (prefix + size);
            while (end - start < prefix + size)
                Fill ();
            Offset = start + prefix;
            Size = size;
            start += prefix + size;
        }

        /// <summary>
        /// The length of the next message and of the prefix carrying it, from what has been read
        /// so far. False if the whole prefix has not arrived yet.
        /// </summary>
        bool Prefix (out int size, out int length)
        {
            size = 0;
            length = 0;
            ulong value = 0;
            var shift = 0;
            for (var i = start; i < end; i++) {
                var b = buffer [i];
                value |= (ulong)(b & 0x7f) << shift;
                length++;
                if ((b & 0x80) == 0) {
                    // Room is made for the prefix and the message together, so the two have to
                    // add up to something an array can be indexed by.
                    if (value > int.MaxValue - MaximumPrefixLength)
                        throw new InvalidDataException ("Message is longer than can be read");
                    size = (int)value;
                    return true;
                }
                if (length == MaximumPrefixLength)
                    throw new InvalidDataException ("Message length prefix is malformed");
                shift += 7;
            }
            return false;
        }

        DateTime? deadline;
        TimeSpan stall = Timeout.InfiniteTimeSpan;

        // When a read last received a byte
        DateTime progress = DateTime.UtcNow;

        /// <summary>
        /// Whether the buffer holds part of a message.
        /// </summary>
        public bool HasBuffered {
            get { return end > start; }
        }

        /// <summary>
        /// The time left before the part of a message in the buffer stalls.
        /// </summary>
        public TimeSpan StallRemaining (TimeSpan stallTimeout)
        {
            var remaining = stallTimeout - (DateTime.UtcNow - progress);
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        /// <summary>
        /// Wait until the socket has data to read, for as long as the timeouts allow.
        /// </summary>
        void WaitForData ()
        {
            if (socket == null)
                return;
            // The stall clock runs from the last byte received, which an earlier read may have
            // taken
            var wait = Timeout.InfiniteTimeSpan;
            if (end > start && stall != Timeout.InfiniteTimeSpan)
                wait = StallRemaining (stall);
            if (deadline.HasValue) {
                var remaining = deadline.Value - DateTime.UtcNow;
                if (remaining < TimeSpan.Zero)
                    remaining = TimeSpan.Zero;
                if (wait == Timeout.InfiniteTimeSpan || remaining < wait)
                    wait = remaining;
            }
            if (wait == Timeout.InfiniteTimeSpan)
                return;
            var microseconds = (int)Math.Min (wait.TotalMilliseconds * 1000, int.MaxValue);
            if (!socket.Poll (microseconds, SelectMode.SelectRead))
                throw new TimeoutException ("Timed out waiting for data from the server");
        }

        /// <summary>
        /// Read a block from the stream into the buffer.
        /// </summary>
        void Fill ()
        {
            WaitForData ();
            // Room for at least one byte more than is in hand, so that there is somewhere to put
            // what the read takes.
            Reserve (end - start + 1);
            var read = stream.Read (buffer, end, buffer.Length - end);
            if (read <= 0)
                throw new EndOfStreamException ("Connection closed by the server");
            end += read;
            progress = DateTime.UtcNow;
        }

        /// <summary>
        /// Make room for the given number of bytes from the start of what has been read, moving
        /// that down the buffer and growing the buffer as needed.
        /// </summary>
        void Reserve (int size)
        {
            if (buffer.Length - start >= size)
                return;
            if (start > 0) {
                System.Buffer.BlockCopy (buffer, start, buffer, 0, end - start);
                end -= start;
                start = 0;
            }
            if (buffer.Length >= size)
                return;
            var length = buffer.Length;
            while (length < size) {
                // Stepping up past what an array can hold would wrap around to a negative
                // length, so the last step up is to exactly what is needed.
                if (length > int.MaxValue - IncreaseSize) {
                    length = size;
                    break;
                }
                length += IncreaseSize;
            }
            var grown = new byte [length];
            System.Buffer.BlockCopy (buffer, 0, grown, 0, end);
            buffer = grown;
        }
    }
}
