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
