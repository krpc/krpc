using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace KRPC.Utils
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    static class Text
    {
        const byte CONTINUATION_MASK = 0xc0;
        const byte CONTINUATION_HEAD = 0x80;
        const byte ONE_BYTE_MASK = 0x80;
        const byte ONE_BYTE_HEAD = 0x00;
        const byte TWO_BYTE_MASK = 0xe0;
        const byte TWO_BYTE_HEAD = 0xc0;
        const byte THREE_BYTE_MASK = 0xf0;
        const byte THREE_BYTE_HEAD = 0xe0;
        const byte FOUR_BYTE_MASK = 0xf8;
        const byte FOUR_BYTE_HEAD = 0xf0;

        /// <summary>
        /// Returns true if the given data is a valid UTF8 string.
        /// </summary>
        public static bool IsValidUTF8 (byte[] data, int index, int count)
        {
            try {
                Encoding encoding = new UTF8Encoding (true, true);
                encoding.GetCharCount (data, index, count);
                return true;
            } catch (DecoderFallbackException) {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the given data is valid, but possible truncated, UTF8 string.
        /// If true, sets <paramref name="length"/> to the number of bytes at the end of the array that are a valid, but truncated UTF8 character/
        /// </summary>
        public static bool IsValidTruncatedUTF8 (byte[] data, int index, int count, ref int length)
        {
            if (count == 0)
                return true;
            int continuationBytes = 0;
            int position = index + count - 1;
            // Count the number of bytes at the end of the data that are continuation bytes
            while (position >= index && (data [position] & CONTINUATION_MASK) == CONTINUATION_HEAD) {
                continuationBytes++;
                position--;
            }
            // Fail if there is no start byte
            if (position < index)
                return false;
            var startByte = data [position];
            // Check that the code point is in the correct range for the number of bytes
            if ((startByte & TWO_BYTE_MASK) == 0)
                return false;
            if ((startByte & THREE_BYTE_MASK) == 0)
                return false;
            if ((startByte & FOUR_BYTE_MASK) == 0)
                return false;
            // Get the maximum number of continuation bytes allowed based on the next byte, which must be a start byte
            int maxContinuationBytes;
            if ((startByte & ONE_BYTE_MASK) == ONE_BYTE_HEAD)
                maxContinuationBytes = 0;
            else if ((startByte & TWO_BYTE_MASK) == TWO_BYTE_HEAD)
                maxContinuationBytes = 1;
            else if ((startByte & THREE_BYTE_MASK) == THREE_BYTE_HEAD)
                maxContinuationBytes = 2;
            else if ((startByte & FOUR_BYTE_MASK) == FOUR_BYTE_HEAD)
                maxContinuationBytes = 3;
            else
                // Start byte was not valid
                return false;
            // Check that maximum code point U+10FFFF is not exceeded
            // has binary value xxxx100 xx001111 xx111111 xx111111
            if (maxContinuationBytes == 3) {
                if ((startByte & (~FOUR_BYTE_MASK)) > 0x4)
                    return false;
                if (continuationBytes > 0 && (data [position + 1] & (~CONTINUATION_MASK)) > 0xf)
                    return false;
            }
            // Fail if there are more continuation bytes than allowed by the start byte
            if (continuationBytes > maxContinuationBytes)
                return false;
            // Validate the rest of the string
            if (continuationBytes != maxContinuationBytes)
                count -= 1 + continuationBytes;
            bool result = IsValidUTF8 (data, index, count);
            if (result && continuationBytes != maxContinuationBytes)
                length = 1 + continuationBytes;
            return result;
        }
    }
}
