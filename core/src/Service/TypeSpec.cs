using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    /// <summary>
    /// A type together with which of the positions inside it can hold null. A C# type says what
    /// a value is, and nullability belongs to the positions the value is made of, so the two
    /// travel together wherever a value is encoded, decoded or described.
    /// </summary>
    public sealed class TypeSpec
    {
        readonly TypeSpec[] types;

        TypeSpec (Type type, Type declaredType, bool nullable, TypeSpec[] containedTypes)
        {
            Type = type;
            DeclaredType = declaredType;
            Nullable = nullable;
            types = containedTypes;
            Kind = KindOf (type);
        }

        /// <summary>
        /// The type of the value, with Nullable&lt;T&gt; unwrapped to the type it wraps.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The type the value is declared with, which a Nullable&lt;T&gt; keeps. A collection
        /// is built from the declared types of the values it holds, so that a position that
        /// can be null is able to hold one.
        /// </summary>
        public Type DeclaredType { get; private set; }

        /// <summary>
        /// Whether the position this value sits in can hold null.
        /// </summary>
        public bool Nullable { get; private set; }

        /// <summary>
        /// What the type is on the wire. Worked out here, once, as the answer costs
        /// reflection and every value encoded or decoded needs it.
        /// </summary>
        public TypeKind Kind { get; private set; }

        /// <summary>
        /// The values this one contains, in the order the wire type names them: the element of
        /// a list or a set, the key and value of a dictionary, or the items of a tuple. The
        /// fields of a structure are declared by the structure itself, so it contains none.
        /// </summary>
        public IList<TypeSpec> Types {
            get { return types; }
        }

        /// <summary>
        /// A spec for the given type, with no nullable position anywhere inside it.
        /// </summary>
        public static TypeSpec Create (Type type)
        {
            return Create (type, false, null, null);
        }

        /// <summary>
        /// A spec for the given type. Each path names a nullable position, a step at a time
        /// from the outside in, and an empty path is the value itself. Location names what the
        /// spec is being built for, and is reported with any error.
        /// </summary>
        public static TypeSpec Create (
            Type type, bool nullable, IEnumerable<IList<Position>> paths, string location)
        {
            var pathList = paths == null ? new List<IList<Position>> () : paths.ToList ();
            return Build (type, nullable, pathList, location);
        }

        static TypeSpec Build (
            Type type, bool nullable, IList<IList<Position>> paths, string location)
        {
            var declaredType = type;
            var underlyingType = System.Nullable.GetUnderlyingType (type);
            if (underlyingType != null) {
                type = underlyingType;
                nullable = true;
            }
            if (paths.Any (path => path.Count == 0))
                nullable = true;
            if (nullable && !TypeUtils.CanBeNull (declaredType))
                throw new ServiceException (
                    declaredType + " cannot be null, in " + location);
            var positions = PositionsOf (type);
            foreach (var path in paths) {
                if (path.Count > 0 && !positions.Any (x => x.Key == path [0]))
                    throw new ServiceException (
                        type + " has no position " + path [0] + ", in " + location);
            }
            var containedTypes = new TypeSpec [positions.Count];
            for (int i = 0; i < positions.Count; i++) {
                var position = positions [i];
                var innerPaths = paths
                    .Where (path => path.Count > 0 && path [0] == position.Key)
                    .Select (path => (IList<Position>)path.Skip (1).ToList ())
                    .ToList ();
                containedTypes [i] = Build (position.Value, false, innerPaths, location);
            }
            return new TypeSpec (type, declaredType, nullable, containedTypes);
        }

        /// <summary>
        /// What the given type is on the wire.
        /// </summary>
        static TypeKind KindOf (Type type)
        {
            if (type.IsEnum)
                return TypeUtils.IsAnEnumType (type) ? TypeKind.Enum : TypeKind.UndeclaredEnum;
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return TypeKind.Double;
            case TypeCode.Single:
                return TypeKind.Single;
            case TypeCode.Int32:
                return TypeKind.Int32;
            case TypeCode.Int64:
                return TypeKind.Int64;
            case TypeCode.UInt32:
                return TypeKind.UInt32;
            case TypeCode.UInt64:
                return TypeKind.UInt64;
            case TypeCode.Boolean:
                return TypeKind.Boolean;
            case TypeCode.String:
                return TypeKind.String;
            }
            if (type == typeof(byte[]))
                return TypeKind.Bytes;
            if (TypeUtils.IsAClassType (type))
                return TypeKind.Class;
            if (TypeUtils.IsAStructType (type))
                return TypeKind.Struct;
            if (TypeUtils.IsATupleCollectionType (type))
                return TypeKind.Tuple;
            if (TypeUtils.IsAListCollectionType (type))
                return TypeKind.List;
            if (TypeUtils.IsASetCollectionType (type))
                return TypeKind.Set;
            if (TypeUtils.IsADictionaryCollectionType (type))
                return TypeKind.Dictionary;
            if (TypeUtils.IsAMessageType (type))
                return TypeKind.Message;
            return TypeKind.Unknown;
        }

        /// <summary>
        /// The types the given type contains, in the order the wire type names them, each with
        /// the position a path names it by. A dictionary key and a set element have no name,
        /// and are given one no path can spell.
        /// </summary>
        static IList<KeyValuePair<Position?, Type>> PositionsOf (Type type)
        {
            var result = new List<KeyValuePair<Position?, Type>> ();
            var arguments = type.IsGenericType ? type.GetGenericArguments () : new Type [0];
            if (TypeUtils.IsAListCollectionType (type)) {
                result.Add (At (Position.Element, arguments [0]));
            } else if (TypeUtils.IsASetCollectionType (type)) {
                result.Add (At (null, arguments [0]));
            } else if (TypeUtils.IsADictionaryCollectionType (type)) {
                result.Add (At (null, arguments [0]));
                result.Add (At (Position.Value, arguments [1]));
            } else if (TypeUtils.IsATupleCollectionType (type)) {
                for (int i = 0; i < arguments.Length; i++) {
                    // A tuple longer than the positions Item1 to Item7 name has no name for its
                    // remaining items, which no path can then reach
                    var position = i < 7 ? (Position?)(Position.Item1 + i) : null;
                    result.Add (At (position, arguments [i]));
                }
            }
            return result;
        }

        static KeyValuePair<Position?, Type> At (Position? position, Type type)
        {
            return new KeyValuePair<Position?, Type> (position, type);
        }
    }
}
