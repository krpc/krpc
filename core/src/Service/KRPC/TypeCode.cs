using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The kind of a type. The values match the type codes used by the
    /// communication protocol to describe procedure parameter and return types.
    /// </summary>
    [KRPCEnum (Service = "KRPC")]
    public enum TypeCode
    {
        /// <summary>
        /// A double precision floating point number.
        /// </summary>
        Double = 1,
        /// <summary>
        /// A single precision floating point number.
        /// </summary>
        Float = 2,
        /// <summary>
        /// A signed 32-bit integer.
        /// </summary>
        SInt32 = 3,
        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
        SInt64 = 4,
        /// <summary>
        /// An unsigned 32-bit integer.
        /// </summary>
        UInt32 = 5,
        /// <summary>
        /// An unsigned 64-bit integer.
        /// </summary>
        UInt64 = 6,
        /// <summary>
        /// A boolean.
        /// </summary>
        Bool = 7,
        /// <summary>
        /// A string.
        /// </summary>
        String = 8,
        /// <summary>
        /// A sequence of bytes.
        /// </summary>
        Bytes = 9,
        /// <summary>
        /// A class defined by a service.
        /// </summary>
        Class = 100,
        /// <summary>
        /// An enumeration defined by a service.
        /// </summary>
        Enumeration = 101,
        /// <summary>
        /// A structure defined by a service.
        /// </summary>
        Struct = 102,
        /// <summary>
        /// A tuple. The value types are given by <see cref="Type.Types"/>.
        /// </summary>
        Tuple = 300,
        /// <summary>
        /// A list. The value type is given by <see cref="Type.Types"/>.
        /// </summary>
        List = 301,
        /// <summary>
        /// A set. The value type is given by <see cref="Type.Types"/>.
        /// </summary>
        Set = 302,
        /// <summary>
        /// A dictionary. The key and value types are given by <see cref="Type.Types"/>.
        /// </summary>
        Dictionary = 303
    }
}
