using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Google.Protobuf;
using KRPC.Client.Attributes;

namespace KRPC.Client
{
    /// <summary>
    /// The kinds of value the encoding carries that are not a number, a string or a block of
    /// bytes. Which one a type is decides how it is encoded and decoded.
    /// </summary>
    enum TypeKind
    {
        Unsupported,
        Bytes,
        Class,
        Struct,
        Tuple,
        List,
        Set,
        Dictionary,
        Message,
        Event
    }

    /// <summary>
    /// What the encoding needs to know about a type, worked out once and kept.
    /// </summary>
    /// <remarks>
    /// Deciding which kind a type is means walking its interfaces and its base types, and
    /// building a value of it means finding a constructor, a property or a method by name.
    /// Neither answer changes over the life of a program, and both were being worked out again
    /// for every value encoded or decoded: a call returning a list of parts asked what a part is
    /// once per part, and a call returning a position asked what a tuple of three doubles is,
    /// and then which constructor builds one, on every call.
    ///
    /// The delegates below are compiled from expression trees, as the client already does to
    /// turn a call into a procedure to invoke.
    /// </remarks>
    sealed class TypeInfo
    {
        static readonly ConcurrentDictionary<Type, TypeInfo> cache =
            new ConcurrentDictionary<Type, TypeInfo> ();

        /// <summary>
        /// What is known about the given type.
        /// </summary>
        public static TypeInfo For (Type type)
        {
            TypeInfo info;
            if (cache.TryGetValue (type, out info))
                return info;
            return cache.GetOrAdd (type, new TypeInfo (type));
        }

        TypeInfo (Type type)
        {
            if (type.Equals (typeof(byte[]))) {
                Kind = TypeKind.Bytes;
            } else if (type.IsSubclassOf (typeof(RemoteObject))) {
                Kind = TypeKind.Class;
                NewObject = ObjectConstructor (type);
            } else if (type.IsDefined (typeof(KRPCStructAttribute), false)) {
                Kind = TypeKind.Struct;
                var fields = StructFields (type);
                Arguments = Array.ConvertAll (fields, field => field.PropertyType);
                Items = StructItems (type, fields);
                NewTuple = StructConstructor (type, Arguments);
            } else if (IsATupleType (type)) {
                Kind = TypeKind.Tuple;
                Arguments = type.GetGenericArguments ();
                Items = TupleItems (Arguments);
                NewTuple = TupleConstructor (Arguments);
            } else if (IsAGenericType (type, typeof(IList<>))) {
                Kind = TypeKind.List;
                Arguments = type.GetGenericArguments ();
                New = Constructor (typeof(List<>).MakeGenericType (Arguments));
            } else if (IsAGenericType (type, typeof(ISet<>))) {
                Kind = TypeKind.Set;
                Arguments = type.GetGenericArguments ();
                New = Constructor (typeof(HashSet<>).MakeGenericType (Arguments));
                Add = SetAdder (type, Arguments [0]);
            } else if (IsAGenericType (type, typeof(IDictionary<,>))) {
                Kind = TypeKind.Dictionary;
                Arguments = type.GetGenericArguments ();
                New = Constructor (typeof(Dictionary<,>).MakeGenericType (Arguments));
            } else if (typeof(IMessage).IsAssignableFrom (type)) {
                Kind = TypeKind.Message;
            } else if (type.Equals (typeof(Event))) {
                Kind = TypeKind.Event;
            } else {
                Kind = TypeKind.Unsupported;
            }
        }

        /// <summary>
        /// Which kind of value the type carries.
        /// </summary>
        public TypeKind Kind { get; private set; }

        /// <summary>
        /// The types of the values a collection holds: the item types of a tuple, the element
        /// type of a list or a set, or the key and value types of a dictionary.
        /// </summary>
        public Type[] Arguments { get; private set; }

        /// <summary>
        /// Build a remote object of the type, given the connection it lives on and its
        /// identifier on the server.
        /// </summary>
        public Func<IConnection, ulong, RemoteObject> NewObject { get; private set; }

        /// <summary>
        /// Build an empty collection to decode a list, a set or a dictionary into.
        /// </summary>
        public Func<object> New { get; private set; }

        /// <summary>
        /// Add a value to a set built by <see cref="New"/>.
        /// </summary>
        public Action<object, object> Add { get; private set; }

        /// <summary>
        /// Build a tuple, or a structure, from its items, in the order <see cref="Arguments"/>
        /// names them.
        /// </summary>
        public Func<object[], object> NewTuple { get; private set; }

        /// <summary>
        /// Take the items out of a tuple, or the fields out of a structure, in the order
        /// <see cref="Arguments"/> names them.
        /// </summary>
        public Func<object, object>[] Items { get; private set; }

        static Func<IConnection, ulong, RemoteObject> ObjectConstructor (Type type)
        {
            var connection = Expression.Parameter (typeof(IConnection));
            var id = Expression.Parameter (typeof(ulong));
            var constructor = type.GetConstructor (new [] { typeof(IConnection), typeof(ulong) });
            return Expression.Lambda<Func<IConnection, ulong, RemoteObject>> (
                Expression.New (constructor, connection, id), connection, id).Compile ();
        }

        static Func<object> Constructor (Type type)
        {
            return Expression.Lambda<Func<object>> (
                Expression.Convert (Expression.New (type), typeof(object))).Compile ();
        }

        static Action<object, object> SetAdder (Type type, Type valueType)
        {
            var set = Expression.Parameter (typeof(object));
            var item = Expression.Parameter (typeof(object));
            // The result of Add says whether the set already held the value, and is discarded.
            return Expression.Lambda<Action<object, object>> (
                Expression.Block (
                    Expression.Call (Expression.Convert (set, type), type.GetMethod ("Add"),
                                     Expression.Convert (item, valueType)),
                    Expression.Empty ()),
                set, item).Compile ();
        }

        /// <summary>
        /// The properties that carry the fields of a structure type, in the order the type
        /// declares them, which is the order their values are encoded in. Reflection does not
        /// promise an order for the properties of a type, so they are ordered by metadata token,
        /// which is assigned in declaration order.
        /// </summary>
        static PropertyInfo[] StructFields (Type type)
        {
            var fields = type.GetProperties (BindingFlags.Public | BindingFlags.Instance);
            Array.Sort (fields, (x, y) => x.MetadataToken.CompareTo (y.MetadataToken));
            return fields;
        }

        static Func<object[], object> StructConstructor (Type type, Type[] fieldTypes)
        {
            var values = Expression.Parameter (typeof(object[]));
            var items = new Expression [fieldTypes.Length];
            for (var i = 0; i < fieldTypes.Length; i++)
                items [i] = Expression.Convert (
                    Expression.ArrayIndex (values, Expression.Constant (i)), fieldTypes [i]);
            return Expression.Lambda<Func<object[], object>> (
                Expression.Convert (
                    Expression.New (type.GetConstructor (fieldTypes), items), typeof(object)),
                values).Compile ();
        }

        static Func<object, object>[] StructItems (Type type, PropertyInfo[] fields)
        {
            var items = new Func<object, object> [fields.Length];
            for (var i = 0; i < fields.Length; i++) {
                var value = Expression.Parameter (typeof(object));
                items [i] = Expression.Lambda<Func<object, object>> (
                    Expression.Convert (
                        Expression.Property (Expression.Convert (value, type), fields [i]),
                        typeof(object)),
                    value).Compile ();
            }
            return items;
        }

        static Type TupleType (Type[] valueTypes)
        {
            var generic = Type.GetType (
                "System.Tuple`" + valueTypes.Length.ToString (CultureInfo.InvariantCulture));
            return generic.MakeGenericType (valueTypes);
        }

        static Func<object[], object> TupleConstructor (Type[] valueTypes)
        {
            var values = Expression.Parameter (typeof(object[]));
            var items = new Expression [valueTypes.Length];
            for (var i = 0; i < valueTypes.Length; i++)
                items [i] = Expression.Convert (
                    Expression.ArrayIndex (values, Expression.Constant (i)), valueTypes [i]);
            return Expression.Lambda<Func<object[], object>> (
                Expression.Convert (
                    Expression.New (TupleType (valueTypes).GetConstructor (valueTypes), items),
                    typeof(object)),
                values).Compile ();
        }

        static Func<object, object>[] TupleItems (Type[] valueTypes)
        {
            var tupleType = TupleType (valueTypes);
            var items = new Func<object, object> [valueTypes.Length];
            for (var i = 0; i < valueTypes.Length; i++) {
                var tuple = Expression.Parameter (typeof(object));
                var name = "Item" + (i + 1).ToString (CultureInfo.InvariantCulture);
                items [i] = Expression.Lambda<Func<object, object>> (
                    Expression.Convert (
                        Expression.Property (Expression.Convert (tuple, tupleType),
                                             tupleType.GetProperty (name)),
                        typeof(object)),
                    tuple).Compile ();
            }
            return items;
        }

        static bool IsATupleType (Type type)
        {
            return
            IsAGenericType (type, typeof(Tuple<>)) ||
            IsAGenericType (type, typeof(Tuple<,>)) ||
            IsAGenericType (type, typeof(Tuple<,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,,,>));
        }

        static bool IsAGenericType (Type type, Type genericType)
        {
            while (!ReferenceEquals (type, null)) {
                if (type.IsGenericType && type.GetGenericTypeDefinition ().Equals (genericType))
                    return true;
                foreach (var intType in type.GetInterfaces())
                    if (IsAGenericType (intType, genericType))
                        return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}
