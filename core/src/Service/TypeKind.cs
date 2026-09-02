namespace KRPC.Service
{
    /// <summary>
    /// What a type is on the wire, which decides how a value of it is encoded and decoded.
    /// A spec works it out once from the type it holds, so that encoding a value costs no
    /// reflection.
    /// </summary>
    public enum TypeKind
    {
        /// <summary>
        /// A type nothing can be encoded as.
        /// </summary>
        Unknown,
        /// <summary>
        /// An enumeration a service declares, encoded as its integer value.
        /// </summary>
        Enum,
        /// <summary>
        /// An enumeration no service declares. A value of it encodes, and no value decodes.
        /// </summary>
        UndeclaredEnum,
        /// <summary>
        /// A double precision floating point number.
        /// </summary>
        Double,
        /// <summary>
        /// A single precision floating point number.
        /// </summary>
        Single,
        /// <summary>
        /// A signed 32-bit integer.
        /// </summary>
        Int32,
        /// <summary>
        /// A signed 64-bit integer.
        /// </summary>
        Int64,
        /// <summary>
        /// An unsigned 32-bit integer.
        /// </summary>
        UInt32,
        /// <summary>
        /// An unsigned 64-bit integer.
        /// </summary>
        UInt64,
        /// <summary>
        /// A boolean.
        /// </summary>
        Boolean,
        /// <summary>
        /// A string.
        /// </summary>
        String,
        /// <summary>
        /// An array of bytes.
        /// </summary>
        Bytes,
        /// <summary>
        /// A class a service declares, encoded as the identifier of the instance.
        /// </summary>
        Class,
        /// <summary>
        /// A structure a service declares, encoded as its field values.
        /// </summary>
        Struct,
        /// <summary>
        /// A tuple, encoded as the values of its items.
        /// </summary>
        Tuple,
        /// <summary>
        /// A list, encoded as the values of its elements.
        /// </summary>
        List,
        /// <summary>
        /// A set, encoded as the values of its elements.
        /// </summary>
        Set,
        /// <summary>
        /// A dictionary, encoded as its key and value pairs.
        /// </summary>
        Dictionary,
        /// <summary>
        /// A protocol buffer message the protocol itself defines.
        /// </summary>
        Message
    }
}
