using System;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// The values of the enumerations a service declares. A flags enumeration, one with
    /// <see cref="FlagsAttribute"/>, holds any combination of its values. Any other enumeration
    /// holds only the values it declares.
    /// </summary>
    public static class Enumerations
    {
        /// <summary>
        /// Whether the enumeration is a flags enumeration.
        /// </summary>
        public static bool IsFlags (Type type)
        {
            return Reflection.HasAttribute<FlagsAttribute> (type);
        }

        /// <summary>
        /// The union of the values a flags enumeration declares.
        /// </summary>
        public static int Mask (Type type)
        {
            var mask = 0;
            foreach (var value in Enum.GetValues (type))
                mask |= (int)value;
            return mask;
        }

        /// <summary>
        /// Whether the number is a value of the enumeration. For a flags enumeration, it is one
        /// when it holds no bit outside the union of the declared values.
        /// </summary>
        public static bool IsValid (Type type, int value)
        {
            if (IsFlags (type))
                return (value & ~Mask (type)) == 0;
            return Enum.IsDefined (type, value);
        }

        /// <summary>
        /// Check the values of a flags enumeration. It declares 0, and each value is 0, a single
        /// bit, or a union of single bits the enumeration declares. Throws a
        /// <see cref="ServiceException"/> if not.
        /// </summary>
        public static void ValidateFlags (Type type)
        {
            if (!Enum.IsDefined (type, 0))
                throw new ServiceException (
                    "KRPCEnum " + type + " is a flags enumeration, and declares no value of 0");
            var bits = 0;
            foreach (var value in Enum.GetValues (type)) {
                var number = (int)value;
                if (number < 0)
                    throw new ServiceException (
                        "KRPCEnum " + type + " is a flags enumeration, and its value " + value + " is negative");
                if ((number & (number - 1)) == 0)
                    bits |= number;
            }
            foreach (var value in Enum.GetValues (type)) {
                var number = (int)value;
                if ((number & ~bits) != 0)
                    throw new ServiceException (
                        "KRPCEnum " + type + " is a flags enumeration, and its value " + value +
                        " holds a bit that no single bit value of it declares");
            }
        }
    }
}
