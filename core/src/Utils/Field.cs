using System;

namespace KRPC.Utils
{
    /// <summary>
    /// A getter and setter for a field or property, from <see cref="Accessor"/>.
    /// </summary>
    public sealed class Field<TInstance, TValue>
    {
        readonly Lazy<Action<TInstance, TValue>> set;

        /// <summary>
        /// Create a field from its getter, and a function that creates its setter on first use.
        /// </summary>
        public Field (Func<TInstance, TValue> get, Func<Action<TInstance, TValue>> createSet)
        {
            Get = get;
            set = new Lazy<Action<TInstance, TValue>> (createSet);
        }

        /// <summary>
        /// The getter.
        /// </summary>
        public Func<TInstance, TValue> Get { get; private set; }

        /// <summary>
        /// The setter, or null for a read-only field or property.
        /// </summary>
        public Action<TInstance, TValue> Set {
            get { return set.Value; }
        }
    }
}
