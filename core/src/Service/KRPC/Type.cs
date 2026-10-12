using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The type of a value a procedure takes or returns.
    /// </summary>
    [KRPCClass (Service = "KRPC")]
    public class Type : Equatable<Type>
    {
        internal TypeSpec Spec { get; private set; }

        internal Type (TypeSpec spec)
        {
            Spec = spec;
        }

        internal Type (System.Type type) : this (TypeSpec.Create (type))
        {
        }

        /// <summary>
        /// The CLR type a value of this type is held in. A nullable value type is a
        /// Nullable&lt;T&gt;.
        /// </summary>
        internal System.Type InternalType {
            get { return Spec.DeclaredType; }
        }

        // The type a nullable value holds, which the wire describes it by
        System.Type DescribedType {
            get { return Spec.Type; }
        }

        /// <summary>
        /// Whether the given object names the same type, nullable at the same positions.
        /// </summary>
        public override bool Equals (Type other)
        {
            // The object store keys on equality, so naming a type repeatedly reuses one object id
            return !ReferenceEquals (other, null) && Spec.Equals (other.Spec);
        }

        /// <summary>
        /// The hash code of the type.
        /// </summary>
        public override int GetHashCode ()
        {
            return Spec.GetHashCode ();
        }

        /// <summary>
        /// A double precision floating point number type.
        /// </summary>
        [KRPCMethod]
        public static Type Double ()
        {
            return new Type (typeof (double));
        }

        /// <summary>
        /// A single precision floating point number type.
        /// </summary>
        [KRPCMethod]
        public static Type Float ()
        {
            return new Type (typeof (float));
        }

        /// <summary>
        /// A signed 32-bit integer type.
        /// </summary>
        [KRPCMethod]
        public static Type Int ()
        {
            return new Type (typeof (int));
        }

        /// <summary>
        /// A signed 64-bit integer type.
        /// </summary>
        [KRPCMethod]
        public static Type Long ()
        {
            return new Type (typeof (long));
        }

        /// <summary>
        /// An unsigned 32-bit integer type.
        /// </summary>
        [KRPCMethod]
        public static Type UInt ()
        {
            return new Type (typeof (uint));
        }

        /// <summary>
        /// An unsigned 64-bit integer type.
        /// </summary>
        [KRPCMethod]
        public static Type ULong ()
        {
            return new Type (typeof (ulong));
        }

        /// <summary>
        /// A boolean type.
        /// </summary>
        [KRPCMethod]
        public static Type Bool ()
        {
            return new Type (typeof (bool));
        }

        /// <summary>
        /// A string type.
        /// </summary>
        [KRPCMethod]
        public static Type String ()
        {
            return new Type (typeof (string));
        }

        /// <summary>
        /// A byte sequence type.
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
        /// A tuple type. Throws an exception if there are no element types or more than seven.
        /// </summary>
        /// <param name="valueTypes">The types of the elements of the tuple.</param>
        [KRPCMethod]
        public static Type TupleType (IList<Type> valueTypes)
        {
            if (valueTypes == null || valueTypes.Count == 0)
                throw new ArgumentException ("Tuple types must have at least one element type");
            if (valueTypes.Count > TypeUtils.MaxTupleElements)
                throw new ArgumentException (
                    "A tuple cannot have more than " + TypeUtils.MaxTupleElements + " elements");
            if (valueTypes.Any (t => ReferenceEquals (t, null)))
                throw new ArgumentNullException (nameof (valueTypes));
            var specs = valueTypes.Select (t => t.Spec).ToList ();
            var genericType = System.Type.GetType ("System.Tuple`" + specs.Count);
            return new Type (TypeSpec.Create (
                genericType.MakeGenericType (specs.Select (t => t.DeclaredType).ToArray ()), false, specs));
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
            return new Type (TypeSpec.Create (
                typeof (IList<>).MakeGenericType (valueType.InternalType), false, new [] { valueType.Spec }));
        }

        /// <summary>
        /// A set type. Throws an exception if the element type is nullable.
        /// </summary>
        /// <param name="valueType">The type of the elements of the set.</param>
        [KRPCMethod]
        public static Type SetType (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            if (valueType.Nullable)
                throw new ArgumentException ("The elements of a set cannot be nullable");
            return new Type (TypeSpec.Create (
                typeof (HashSet<>).MakeGenericType (valueType.InternalType), false, new [] { valueType.Spec }));
        }

        /// <summary>
        /// A dictionary type. Throws an exception if the key type is nullable or not a valid
        /// key type.
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
            if (keyType.Nullable)
                throw new ArgumentException ("The keys of a dictionary cannot be nullable");
            if (!TypeUtils.IsAValidKeyType (keyType.InternalType))
                throw new ArgumentException (
                    keyType.InternalType + " is not a valid dictionary key type");
            return new Type (TypeSpec.Create (
                typeof (IDictionary<,>).MakeGenericType (keyType.InternalType, valueType.InternalType),
                false, new [] { keyType.Spec, valueType.Spec }));
        }

        /// <summary>
        /// A nullable type, holding a value of the given type or null.
        /// </summary>
        /// <param name="valueType">The type of the value.</param>
        [KRPCMethod]
        public static Type NullableType (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            return new Type (valueType.Spec.WithNullable (true));
        }

        /// <summary>
        /// The kind of the type.
        /// </summary>
        [KRPCProperty]
        public TypeCode Code {
            get {
                var type = DescribedType;
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
        /// The service a class, enumeration or structure type is defined in, or an empty string.
        /// </summary>
        [KRPCProperty]
        public string Service {
            get {
                var type = DescribedType;
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
        /// The name of a class, enumeration or structure type, or an empty string.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get {
                var type = DescribedType;
                if (TypeUtils.IsAClassType (type) || TypeUtils.IsAnEnumType (type) ||
                    TypeUtils.IsAStructType (type))
                    return type.Name;
                return string.Empty;
            }
        }

        /// <summary>
        /// The types of the elements of a tuple, list, set or dictionary type.
        /// </summary>
        /// <remarks>
        /// The value types of a tuple, the value type of a list or set, or the key and value
        /// types of a dictionary. An empty list for any other type.
        /// </remarks>
        [KRPCProperty]
        public IList<Type> Types {
            get {
                var type = DescribedType;
                if (TypeUtils.IsACollectionType (type))
                    return Spec.Types.Select (t => new Type (t)).ToList ();
                return new List<Type> ();
            }
        }

        /// <summary>
        /// Whether a value of the type may be null.
        /// </summary>
        /// <remarks>
        /// Any type can be nullable. A string or class value of a type that is not nullable
        /// is never null.
        /// </remarks>
        [KRPCProperty]
        public bool Nullable {
            get { return Spec.Nullable; }
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
