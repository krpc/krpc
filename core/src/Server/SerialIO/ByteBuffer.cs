using System;

namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// A first-in first-out queue of bytes. Data is appended at the back and taken from the front.
    /// The backing array is compacted when the space behind the data runs out, and grown only when
    /// the data itself no longer fits, so a buffer that is drained as fast as it is filled settles
    /// on a fixed size and stops allocating.
    /// </summary>
    sealed class ByteBuffer
    {
        const int initialSize = 8 * 1024;

        byte[] data = new byte [initialSize];
        int start;
        int end;

        /// <summary>
        /// Number of bytes held in the buffer.
        /// </summary>
        public int Length {
            get { return end - start; }
        }

        /// <summary>
        /// Append size bytes from buffer, starting at offset, to the back of the queue.
        /// </summary>
        public void Append (byte[] buffer, int offset, int size)
        {
            if (data.Length - end < size) {
                var length = Length;
                if (data.Length - length < size) {
                    var newSize = data.Length;
                    while (newSize - length < size)
                        newSize *= 2;
                    var newData = new byte [newSize];
                    Array.Copy (data, start, newData, 0, length);
                    data = newData;
                } else {
                    Array.Copy (data, start, data, 0, length);
                }
                start = 0;
                end = length;
            }
            Array.Copy (buffer, offset, data, end, size);
            end += size;
        }

        /// <summary>
        /// Remove up to size bytes from the front of the queue, writing them to buffer starting at
        /// offset. Returns the number of bytes taken, which is zero when the queue is empty.
        /// </summary>
        public int Take (byte[] buffer, int offset, int size)
        {
            size = Math.Min (size, Length);
            Array.Copy (data, start, buffer, offset, size);
            start += size;
            if (start == end) {
                start = 0;
                end = 0;
            }
            return size;
        }

        /// <summary>
        /// Discard the contents of the queue.
        /// </summary>
        public void Clear ()
        {
            start = 0;
            end = 0;
        }
    }
}
