using System;

namespace KRPC.Utils
{
    /// <summary>
    /// Provides a dynamically resizable array.
    /// Similar to a MemoryStream, but does not implement IDisposable.
    /// </summary>
    sealed class DynamicBuffer
    {
        const int CHUNK_SIZE = 32 * 1024;
        byte[] buffer = new byte[CHUNK_SIZE];

        public void Append (byte[] data, int offset, int length)
        {
            // If buffer is too small, grow it by a multiple of CHUNK_SIZE
            if (buffer.Length < Length + length) {
                byte[] oldBuffer = buffer;
                var newLength = buffer.Length + length;
                if (newLength % CHUNK_SIZE > 0)
                    newLength += CHUNK_SIZE - (newLength % CHUNK_SIZE);
                buffer = new byte [newLength];
                oldBuffer.CopyTo (buffer, 0);
            }
            // Append the data
            Array.Copy (data, offset, buffer, Length, length);
            Length += length - offset;
        }

        /// <summary>
        /// Ensure the buffer has room for at least 'count' more bytes past its current length, and
        /// return the backing array. Data may be written into it starting at Length, after which
        /// Length must be advanced by the number of bytes written. Lets data be read straight into
        /// the buffer with no intermediate array.
        /// </summary>
        public byte[] Reserve (int count)
        {
            // If the buffer is too small, grow it by a multiple of CHUNK_SIZE
            if (buffer.Length < Length + count) {
                var newLength = Length + count;
                if (newLength % CHUNK_SIZE > 0)
                    newLength += CHUNK_SIZE - (newLength % CHUNK_SIZE);
                var newBuffer = new byte [newLength];
                Array.Copy (buffer, newBuffer, Length);
                buffer = newBuffer;
            }
            return buffer;
        }

        public int Length { get; set; }

        public byte[] GetBuffer ()
        {
            return buffer;
        }

        public byte[] ToArray ()
        {
            var result = new byte [Length];
            Array.Copy (buffer, result, Length);
            return result;
        }
    }
}
