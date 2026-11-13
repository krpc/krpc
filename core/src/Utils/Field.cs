using System;

namespace KRPC.Utils
{
    /// <summary>
    /// A getter and setter for a field or property, from <see cref="Accessor"/>.
    /// </summary>
    public sealed class Field<TInstance, TValue>
    {
        /// <summary>
        /// Create a field from its getter and setter.
        /// </summary>
        public Field (Func<TInstance, TValue> get, Action<TInstance, TValue> set)
        {
            Get = get;
            Set = set;
        }

        /// <summary>
        /// The getter.
        /// </summary>
        public Func<TInstance, TValue> Get { get; private set; }

        /// <summary>
        /// The setter, or null for a read-only field or property.
        /// </summary>
        public Action<TInstance, TValue> Set { get; private set; }
    }
}
