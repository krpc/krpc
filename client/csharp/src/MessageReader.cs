using System.IO;
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

        // How much more room to make when a message does not fit in the buffer.
        const int IncreaseSize = 512 * 1024;

        // A length prefix is a varint, and one that does not end within five bytes cannot be the
        // uint32 it is meant to be.
        const int MaximumPrefixLength = 5;

        readonly Stream stream;
        byte[] buffer = new byte [InitialSize];

        // What has been read but not yet handed out, as the half open range [start, end) of the
        // buffer.
        int start;
        int end;

        public MessageReader (Stream messageStream)
        {
            stream = messageStream;
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
        /// Read the next message, and return whether one was read. If a stop event is given and
        /// is signaled while waiting for a message, returns false without having read one.
        /// </summary>
        public bool Read (EventWaitHandle stopEvent = null)
        {
            int size;
            int prefix;
            while (!Prefix (out size, out prefix)) {
                if (!Fill (stopEvent))
                    return false;
            }
            // The prefix and the message together, so that neither making room for the message
            // nor reading the rest of it moves the buffer once the message has been found in it.
            Reserve (prefix + size);
            while (end - start < prefix + size) {
                if (!Fill (stopEvent))
                    return false;
            }
            Offset = start + prefix;
            Size = size;
            start += prefix + size;
            return true;
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

        /// <summary>
        /// Read a block from the stream into the buffer, and return whether to carry on. A stop
        /// event is checked either side of the read, since the read itself does not observe it.
        /// </summary>
        bool Fill (EventWaitHandle stopEvent)
        {
            if (Stopped (stopEvent))
                return false;
            // Room for at least one byte more than is in hand, so that there is somewhere to put
            // what the read takes.
            Reserve (end - start + 1);
            var read = stream.Read (buffer, end, buffer.Length - end);
            if (read <= 0)
                throw new EndOfStreamException ("Connection closed by the server");
            end += read;
            return !Stopped (stopEvent);
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

        static bool Stopped (EventWaitHandle stopEvent)
        {
            return stopEvent != null && stopEvent.WaitOne (0);
        }
    }
}
