namespace KRPC.Service.Attributes
{
    /// <summary>
    /// A position inside a type that one value sits in, named so that
    /// <see cref="KRPCNullableAttribute"/> can point at it. A dictionary key and a set element
    /// have no name here, which is what makes them the positions that cannot be nullable.
    /// </summary>
    public enum Position
    {
        /// <summary>
        /// The element of a list.
        /// </summary>
        Element,
        /// <summary>
        /// The value of a dictionary.
        /// </summary>
        Value,
        /// <summary>
        /// The first item of a tuple.
        /// </summary>
        Item1,
        /// <summary>
        /// The second item of a tuple.
        /// </summary>
        Item2,
        /// <summary>
        /// The third item of a tuple.
        /// </summary>
        Item3,
        /// <summary>
        /// The fourth item of a tuple.
        /// </summary>
        Item4,
        /// <summary>
        /// The fifth item of a tuple.
        /// </summary>
        Item5,
        /// <summary>
        /// The sixth item of a tuple.
        /// </summary>
        Item6,
        /// <summary>
        /// The seventh item of a tuple.
        /// </summary>
        Item7
    }
}
