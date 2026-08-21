using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Service.Scanner;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The type of a value, for use with server side expressions.
    /// For example, the types of expression function parameters and the
    /// target types of casts.
    /// </summary>
    [KRPCClass (Service = "KRPC")]
    public class Type
    {
        internal System.Type InternalType { get; private set; }

        internal Type (System.Type type)
        {
            InternalType = type;
        }

        /// <summary>
        /// Double type.
        /// </summary>
        [KRPCMethod]
        public static Type Double ()
        {
            return new Type (typeof (double));
        }

        /// <summary>
        /// Float type.
        /// </summary>
        [KRPCMethod]
        public static Type Float ()
        {
            return new Type (typeof (float));
        }

        /// <summary>
        /// Int type. A signed 32-bit integer.
        /// </summary>
        [KRPCMethod]
        public static Type Int ()
        {
            return new Type (typeof (int));
        }

        /// <summary>
        /// Long type. A signed 64-bit integer.
        /// </summary>
        [KRPCMethod]
        public static Type Long ()
        {
            return new Type (typeof (long));
        }

        /// <summary>
        /// UInt type. An unsigned 32-bit integer.
        /// </summary>
        [KRPCMethod]
        public static Type UInt ()
        {
            return new Type (typeof (uint));
        }

        /// <summary>
        /// ULong type. An unsigned 64-bit integer.
        /// </summary>
        [KRPCMethod]
        public static Type ULong ()
        {
            return new Type (typeof (ulong));
        }

        /// <summary>
        /// Bool type.
        /// </summary>
        [KRPCMethod]
        public static Type Bool ()
        {
            return new Type (typeof (bool));
        }

        /// <summary>
        /// String type.
        /// </summary>
        [KRPCMethod]
        public static Type String ()
        {
            return new Type (typeof (string));
        }

        /// <summary>
        /// Bytes type. A sequence of bytes.
        /// </summary>
        [KRPCMethod]
        public static Type Bytes ()
        {
            return new Type (typeof (byte[]));
        }

        /// <summary>
        /// The type of a class defined by a service.
        /// </summary>
        /// <param name="service">The name of the service the class is defined in.</param>
        /// <param name="name">The name of the class.</param>
        [KRPCMethod]
        public static Type ClassType (string service, string name)
        {
            var serviceSignature = GetServiceSignature (service);
            ClassSignature cls;
            if (!serviceSignature.Classes.TryGetValue (name, out cls))
                throw new ArgumentException (
                    "Class \"" + name + "\" not found in service \"" + service + "\"");
            return new Type (cls.UnderlyingType);
        }

        /// <summary>
        /// The type of an enumeration defined by a service.
        /// </summary>
        /// <param name="service">The name of the service the enumeration is defined in.</param>
        /// <param name="name">The name of the enumeration.</param>
        [KRPCMethod]
        public static Type EnumerationType (string service, string name)
        {
            var serviceSignature = GetServiceSignature (service);
            EnumerationSignature enm;
            if (!serviceSignature.Enumerations.TryGetValue (name, out enm))
                throw new ArgumentException (
                    "Enumeration \"" + name + "\" not found in service \"" + service + "\"");
            return new Type (enm.UnderlyingType);
        }

        /// <summary>
        /// The type of a structure defined by a service.
        /// </summary>
        /// <param name="service">The name of the service the structure is defined in.</param>
        /// <param name="name">The name of the structure.</param>
        [KRPCMethod]
        public static Type StructType (string service, string name)
        {
            var serviceSignature = GetServiceSignature (service);
            StructSignature str;
            if (!serviceSignature.Structs.TryGetValue (name, out str))
                throw new ArgumentException (
                    "Structure \"" + name + "\" not found in service \"" + service + "\"");
            return new Type (str.UnderlyingType);
        }

        /// <summary>
        /// A tuple type.
        /// </summary>
        /// <param name="valueTypes">The types of the elements of the tuple.</param>
        [KRPCMethod]
        public static Type TupleType (IList<Type> valueTypes)
        {
            if (valueTypes == null || valueTypes.Count == 0)
                throw new ArgumentException ("Tuple types must have at least one element type");
            var types = valueTypes.Select (t => t.InternalType).ToArray ();
            var genericType = System.Type.GetType ("System.Tuple`" + types.Length);
            if (genericType == null)
                throw new ArgumentException (
                    "Tuple types with " + types.Length + " elements are not supported");
            return new Type (genericType.MakeGenericType (types));
        }

        /// <summary>
        /// A list type.
        /// </summary>
        /// <param name="valueType">The type of the elements of the list.</param>
        [KRPCMethod]
        public static Type ListType (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            return new Type (typeof (IList<>).MakeGenericType (valueType.InternalType));
        }

        /// <summary>
        /// A set type.
        /// </summary>
        /// <param name="valueType">The type of the elements of the set.</param>
        [KRPCMethod]
        public static Type SetType (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            return new Type (typeof (HashSet<>).MakeGenericType (valueType.InternalType));
        }

        /// <summary>
        /// A dictionary type.
        /// </summary>
        /// <param name="keyType">The type of the keys of the dictionary.</param>
        /// <param name="valueType">The type of the values of the dictionary.</param>
        [KRPCMethod]
        public static Type DictionaryType (Type keyType, Type valueType)
        {
            if (ReferenceEquals (keyType, null))
                throw new ArgumentNullException (nameof (keyType));
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            if (!TypeUtils.IsAValidKeyType (keyType.InternalType))
                throw new ArgumentException (
                    keyType.InternalType + " is not a valid dictionary key type");
            return new Type (
                typeof (IDictionary<,>).MakeGenericType (keyType.InternalType, valueType.InternalType));
        }

        /// <summary>
        /// The kind of the type.
        /// </summary>
        [KRPCProperty]
        public TypeCode Code {
            get {
                var type = InternalType;
                switch (System.Type.GetTypeCode (type)) {
                case System.TypeCode.Double:
                    return TypeCode.Double;
                case System.TypeCode.Single:
                    return TypeCode.Float;
                case System.TypeCode.Int32:
                    if (type.IsEnum)
                        break;
                    return TypeCode.SInt32;
                case System.TypeCode.Int64:
                    return TypeCode.SInt64;
                case System.TypeCode.UInt32:
                    return TypeCode.UInt32;
                case System.TypeCode.UInt64:
                    return TypeCode.UInt64;
                case System.TypeCode.Boolean:
                    return TypeCode.Bool;
                case System.TypeCode.String:
                    return TypeCode.String;
                }
                if (type == typeof (byte[]))
                    return TypeCode.Bytes;
                if (TypeUtils.IsAClassType (type))
                    return TypeCode.Class;
                if (TypeUtils.IsAnEnumType (type))
                    return TypeCode.Enumeration;
                if (TypeUtils.IsAStructType (type))
                    return TypeCode.Struct;
                if (TypeUtils.IsATupleCollectionType (type))
                    return TypeCode.Tuple;
                if (TypeUtils.IsAListCollectionType (type))
                    return TypeCode.List;
                if (TypeUtils.IsASetCollectionType (type))
                    return TypeCode.Set;
                if (TypeUtils.IsADictionaryCollectionType (type))
                    return TypeCode.Dictionary;
                throw new InvalidOperationException (
                    type + " is not a type that can be sent to a client");
            }
        }

        /// <summary>
        /// For a class, enumeration or structure type, the name of the service it is
        /// defined in. An empty string otherwise.
        /// </summary>
        [KRPCProperty]
        public string Service {
            get {
                var type = InternalType;
                if (TypeUtils.IsAClassType (type))
                    return TypeUtils.GetClassServiceName (type);
                if (TypeUtils.IsAnEnumType (type))
                    return TypeUtils.GetEnumServiceName (type);
                if (TypeUtils.IsAStructType (type))
                    return TypeUtils.GetStructServiceName (type);
                return string.Empty;
            }
        }

        /// <summary>
        /// For a class, enumeration or structure type, its name. An empty string otherwise.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get {
                var type = InternalType;
                if (TypeUtils.IsAClassType (type) || TypeUtils.IsAnEnumType (type) ||
                    TypeUtils.IsAStructType (type))
                    return type.Name;
                return string.Empty;
            }
        }

        /// <summary>
        /// For a tuple, list, set or dictionary type, the types of its elements.
        /// The value types of a tuple, the value type of a list or set, or the key
        /// and value types of a dictionary. An empty list otherwise.
        /// </summary>
        [KRPCProperty]
        public IList<Type> Types {
            get {
                var type = InternalType;
                if (TypeUtils.IsACollectionType (type))
                    return type.GetGenericArguments ().Select (t => new Type (t)).ToList ();
                return new List<Type> ();
            }
        }

        static ServiceSignature GetServiceSignature (string service)
        {
            ServiceSignature signature;
            if (!Services.Instance.Signatures.TryGetValue (service, out signature))
                throw new ArgumentException ("Service \"" + service + "\" not found");
            return signature;
        }
    }
}
