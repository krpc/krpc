using System;

namespace KRPC.Service
{
    /// <summary>
    /// A type together with whether the position holding it can be null. A C# type says what a
    /// value is, and nullability belongs to the position the value sits in, so the two travel
    /// together wherever a value is encoded, decoded or described.
    /// </summary>
    public sealed class TypeSpec
    {
        TypeSpec (Type type, Type declaredType, bool nullable)
        {
            Type = type;
            DeclaredType = declaredType;
            Nullable = nullable;
        }

        /// <summary>
        /// The type of the value, with Nullable&lt;T&gt; unwrapped to the type it wraps.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The type the value is declared with, which a Nullable&lt;T&gt; keeps.
        /// </summary>
        public Type DeclaredType { get; private set; }

        /// <summary>
        /// Whether the position this value sits in can hold null.
        /// </summary>
        public bool Nullable { get; private set; }

        /// <summary>
        /// A spec for the given type, at a position that cannot be null.
        /// </summary>
        public static TypeSpec Create (Type type)
        {
            return Create (type, false);
        }

        /// <summary>
        /// A spec for the given type. A Nullable&lt;T&gt; is the type it wraps, at a position
        /// that can be null.
        /// </summary>
        public static TypeSpec Create (Type type, bool nullable)
        {
            var underlyingType = System.Nullable.GetUnderlyingType (type);
            if (underlyingType != null)
                return new TypeSpec (underlyingType, type, true);
            return new TypeSpec (type, type, nullable);
        }
    }
}
