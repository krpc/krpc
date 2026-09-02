using System;
using System.Collections.Concurrent;
using Google.Protobuf;

namespace KRPC.Client
{
    /// <summary>
    /// The type of a value, together with whether the position holding it can be null. A
    /// generated stub names one wherever it encodes or decodes a value, in place of the type.
    /// </summary>
    /// <remarks>
    /// A C# type declares a nullable position where the value there is a value type, as
    /// <c>T?</c>, and a structure field declares one with <c>KRPCNullable</c>. A reference type
    /// inside a collection declares nothing, so a stub names that position by building a spec
    /// for it.
    /// </remarks>
    public sealed class TypeSpec
    {
        static readonly TypeSpec[] noTypes = new TypeSpec [0];

        static readonly ConcurrentDictionary<Type, TypeSpec> specs =
            new ConcurrentDictionary<Type, TypeSpec> ();

        readonly TypeSpec[] types;

        // The functions that encode and decode a value at this position, built on first use.
        // They are held here so that a collection asks for one once and calls it for each of
        // the values it holds.
        internal Func<object, ByteString> valueEncoder;
        internal Func<object, ByteString> itemEncoder;
        internal Func<ByteString, IConnection, object> valueDecoder;
        internal Func<ByteString, IConnection, object> itemDecoder;

        /// <summary>
        /// A spec for the given type, with the specs of the values it contains, in the order a
        /// collection holds them. A position left unnamed is read from the type.
        /// </summary>
        public TypeSpec (Type type, params TypeSpec[] containedTypes)
        {
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            // A Nullable<T> value is boxed to a plain T, so the type of the value is T and the
            // position it sits in is the nullable one
            var underlying = System.Nullable.GetUnderlyingType (type);
            Type = underlying ?? type;
            Nullable = underlying != null;
            types = containedTypes ?? noTypes;
        }

        /// <summary>
        /// A spec for a value at a position that can hold null.
        /// </summary>
        public static TypeSpec Null (Type type, params TypeSpec[] containedTypes)
        {
            return new TypeSpec (type, containedTypes) { Nullable = true };
        }

        /// <summary>
        /// The spec the given type gives on its own, kept for the life of the program. A value
        /// reached through reflection is encoded and decoded as this, where the only nullable
        /// positions to be had are the ones the type declares.
        /// </summary>
        public static TypeSpec For (Type type)
        {
            TypeSpec spec;
            if (specs.TryGetValue (type, out spec))
                return spec;
            return specs.GetOrAdd (type, new TypeSpec (type));
        }

        /// <summary>
        /// The type of the value.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Whether the position this value sits in can hold null.
        /// </summary>
        public bool Nullable { get; private set; }

        /// <summary>
        /// The spec of the value at the given position, which is the one named here or, where
        /// this spec names none, the one the type declares.
        /// </summary>
        internal TypeSpec At (int position)
        {
            if (position < types.Length)
                return types [position];
            return TypeInfo.For (Type).Types [position];
        }
    }
}
