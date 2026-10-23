using System;

namespace KRPC.Server.ProtocolBuffers
{
    static class Utils
    {
        /// <summary>
        /// Decode the base-128 varint that prefixes a length-delimited message. Returns the message
        /// length in bytes and sets prefixLength to the number of bytes the varint occupies, or sets
        /// prefixLength to zero if the buffer does not yet contain the complete varint.
        /// </summary>
        public static int DecodeMessageLength (byte[] data, int offset, int length, out int prefixLength)
        {
            var result = 0;
            // A varint encoding a 32-bit value is at most five bytes long.
            for (var i = 0; i < 5; i++) {
                if (i >= length) {
                    prefixLength = 0;
                    return 0;
                }
                int b = data [offset + i];
                // The fifth byte holds bits 28 to 34, of which only 28 to 30 fit a positive int
                if (i == 4 && b > 0x07)
                    throw new InvalidOperationException ("Message length prefix is out of range");
                result |= (b & 0x7f) << (7 * i);
                if ((b & 0x80) == 0) {
                    prefixLength = i + 1;
                    return result;
                }
            }
            throw new InvalidOperationException ("Message length prefix is not a valid varint");
        }
    }
}
