using System.Runtime.InteropServices;

namespace KRPC.Client
{
    /// <summary>
    /// A float and the bits it is made of, laid over each other so that one can be had from
    /// the other. The framework converts a double to its bits and back, and has nothing that
    /// does the same for a float.
    /// </summary>
    [StructLayout (LayoutKind.Explicit)]
    struct Single32
    {
        [FieldOffset (0)]
        public float Value;

        [FieldOffset (0)]
        public uint Bits;

        public static uint ToBits (float value)
        {
            var single = default (Single32);
            single.Value = value;
            return single.Bits;
        }

        public static float ToSingle (uint bits)
        {
            var single = default (Single32);
            single.Bits = bits;
            return single.Value;
        }
    }
}
