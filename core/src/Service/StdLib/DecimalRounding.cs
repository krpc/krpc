using System;
using System.Collections.Generic;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// The rule for a value exactly halfway between two candidates.
    /// </summary>
    enum MidpointRule
    {
        ToEven,
        AwayFromZero,
        Up
    }

    /// <summary>
    /// Rounding of a double to a number of decimal places, decided on the exact value of
    /// the double, as Python's round decides it. A tie is a value exactly halfway in
    /// decimal, so every comparison is on exact integers.
    /// </summary>
    /// <remarks>
    /// Comparisons are exact, on integers of up to about 70 32-bit words built from a
    /// mantissa, a power of two and a power of ten. The game ships no System.Numerics,
    /// so the integers are lists of words.
    /// </remarks>
    static class DecimalRounding
    {
        const long InfinityBits = 0x7FF0000000000000;

        // Exact as doubles up to 1e22
        static readonly double[] powersOfTen = {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
            1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22
        };

        /// <summary>
        /// The value rounded to the given number of decimal places, for a finite non-zero
        /// value and digits between -308 and 323.
        /// </summary>
        internal static double Round (double x, int digits, MidpointRule rule)
        {
            var negative = x < 0;
            var bits = BitConverter.DoubleToInt64Bits (Math.Abs (x));
            ulong mantissa;
            int exponent;
            Decompose (bits, out mantissa, out exponent);
            // At 2^54 and above, every decimal within half a unit of the last place
            // rounds back to x
            var estimate = Scale (Math.Abs (x), digits);
            if (estimate >= 18014398509481984.0)
                return x;
            // The integer part of x * 10^digits, from an estimate that is out by at most one
            var floor = (ulong)Math.Floor (estimate);
            while (floor > 0 && Compare (mantissa, exponent, digits, floor, 0, 0) < 0)
                floor--;
            while (Compare (mantissa, exponent, digits, floor + 1, 0, 0) >= 0)
                floor++;
            var half = Compare (mantissa, exponent, digits, 2 * floor + 1, -1, 0);
            var rounded = floor;
            if (half > 0 || (half == 0 && RoundsUp (floor, negative, rule)))
                rounded++;
            var result = ToDouble (rounded, -digits);
            if (double.IsInfinity (result))
                throw new OverflowException ("The rounded value is too large to represent");
            return negative ? -result : result;
        }

        static bool RoundsUp (ulong floor, bool negative, MidpointRule rule)
        {
            switch (rule) {
            case MidpointRule.ToEven:
                return floor % 2 == 1;
            case MidpointRule.AwayFromZero:
                return true;
            default:
                // Toward positive infinity, which for a negative value is toward zero
                return !negative;
            }
        }

        static double Scale (double x, int exponent10)
        {
            // In two steps above 10^308, the largest power of ten a double holds
            if (exponent10 > 308)
                return Scale (Scale (x, 308), exponent10 - 308);
            if (exponent10 < -308)
                return Scale (Scale (x, -308), exponent10 + 308);
            var magnitude = Math.Abs (exponent10);
            var power = magnitude < powersOfTen.Length ? powersOfTen [magnitude] : Math.Pow (10, magnitude);
            return exponent10 >= 0 ? x * power : x / power;
        }

        /// <summary>
        /// The double nearest to value * 10^exponent10, with a tie to an even mantissa.
        /// The estimate is within a few units of the last place, and steps to the double
        /// whose neighboring midpoints bracket the exact value.
        /// </summary>
        static double ToDouble (ulong value, int exponent10)
        {
            if (value == 0)
                return 0;
            // The bits of a positive double are ordered as its value, and those of
            // infinity read as 2^1024
            var bits = BitConverter.DoubleToInt64Bits (Scale (value, exponent10));
            while (bits > 0 && CompareToMidpoint (value, exponent10, bits - 1) < 0)
                bits--;
            while (bits < InfinityBits && CompareToMidpoint (value, exponent10, bits) > 0)
                bits++;
            if (bits % 2 == 1) {
                if (bits > 0 && CompareToMidpoint (value, exponent10, bits - 1) == 0)
                    bits--;
                else if (bits < InfinityBits && CompareToMidpoint (value, exponent10, bits) == 0)
                    bits++;
            }
            return BitConverter.Int64BitsToDouble (bits);
        }

        /// <summary>
        /// Compares value * 10^exponent10 with the midpoint between the double with the
        /// given bits and the next one up.
        /// </summary>
        static int CompareToMidpoint (ulong value, int exponent10, long bits)
        {
            ulong lowMantissa, highMantissa;
            int lowExponent, highExponent;
            Decompose (bits, out lowMantissa, out lowExponent);
            Decompose (bits + 1, out highMantissa, out highExponent);
            // The exponents differ by at most one, so the sum fits in 55 bits
            var sum = lowMantissa + (highMantissa << (highExponent - lowExponent));
            return Compare (value, 0, exponent10, sum, lowExponent - 1, 0);
        }

        /// <summary>
        /// The mantissa and exponent of a non-negative double, as an integer times a power
        /// of two. Infinity decomposes as 2^1024.
        /// </summary>
        static void Decompose (long bits, out ulong mantissa, out int exponent)
        {
            var biased = (int)(bits >> 52) & 0x7FF;
            var fraction = (ulong)bits & 0xFFFFFFFFFFFFFUL;
            if (biased == 0) {
                mantissa = fraction;
                exponent = -1074;
            } else {
                mantissa = fraction | (1UL << 52);
                exponent = biased - 1075;
            }
        }

        /// <summary>
        /// Compares a * 2^a2 * 10^a10 with b * 2^b2 * 10^b10, exactly.
        /// </summary>
        static int Compare (ulong a, int a2, int a10, ulong b, int b2, int b10)
        {
            if (a == 0 || b == 0)
                return a.CompareTo (b);
            // Divide both sides by the smaller powers, so every exponent is non-negative
            var min2 = Math.Min (a2, b2);
            var min10 = Math.Min (a10, b10);
            var left = Integer (a, a2 - min2, a10 - min10);
            var right = Integer (b, b2 - min2, b10 - min10);
            if (left.Count != right.Count)
                return left.Count.CompareTo (right.Count);
            for (int i = left.Count - 1; i >= 0; i--) {
                if (left [i] != right [i])
                    return left [i].CompareTo (right [i]);
            }
            return 0;
        }

        /// <summary>
        /// The integer value * 2^exponent2 * 10^exponent10, as little-endian 32-bit
        /// words with no leading zero word.
        /// </summary>
        static List<uint> Integer (ulong value, int exponent2, int exponent10)
        {
            var words = new List<uint> { (uint)value, (uint)(value >> 32) };
            for (; exponent10 >= 9; exponent10 -= 9)
                Multiply (words, 1000000000);
            if (exponent10 > 0)
                Multiply (words, (uint)powersOfTen [exponent10]);
            var wordShift = exponent2 / 32;
            var bitShift = exponent2 % 32;
            if (bitShift > 0) {
                uint carry = 0;
                for (int i = 0; i < words.Count; i++) {
                    var word = words [i];
                    words [i] = (word << bitShift) | carry;
                    carry = word >> (32 - bitShift);
                }
                words.Add (carry);
            }
            words.InsertRange (0, new uint [wordShift]);
            while (words.Count > 0 && words [words.Count - 1] == 0)
                words.RemoveAt (words.Count - 1);
            return words;
        }

        static void Multiply (List<uint> words, uint factor)
        {
            ulong carry = 0;
            for (int i = 0; i < words.Count; i++) {
                var product = (ulong)words [i] * factor + carry;
                words [i] = (uint)product;
                carry = product >> 32;
            }
            if (carry > 0)
                words.Add ((uint)carry);
        }
    }
}
