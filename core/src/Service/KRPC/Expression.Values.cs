using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Service.Attributes;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    public partial class Expression
    {
        /// <summary>
        /// A tuple of the given elements. Throws an exception if there are no elements or more
        /// than seven.
        /// </summary>
        /// <returns>The tuple.</returns>
        /// <param name="elements">The elements.</param>
        [KRPCMethod]
        public static Expression CreateTuple (IList<Expression> elements)
        {
            if (ReferenceEquals (elements, null))
                throw new ArgumentNullException (nameof (elements));
            if (elements.Count == 0)
                throw new ArgumentException ("A tuple must have at least one element");
            if (elements.Count > TypeUtils.MaxTupleElements)
                throw new ArgumentException (
                    "A tuple cannot have more than " + TypeUtils.MaxTupleElements + " elements");
            CheckNoNullElements (elements, "elements of a tuple");
            CheckEachHasValue (elements, "An element of a tuple");
            var values = elements.Select (x => AsCollectionInterface (x.internalExpression)).ToArray ();
            var method = typeof (Tuple)
                .GetMethods ()
                .First (m => m.Name == "Create" && m.GetGenericArguments ().Length == elements.Count);
            method = method.MakeGenericMethod (values.Select (x => x.Type).ToArray ());
            var call = LinqExpression.Call (method, values);
            return new Expression (call, SpecWith (
                call.Type, elements.Select ((x, i) => SpecAs (x, values [i].Type)).ToArray ()));
        }

        /// <summary>
        /// A structure with the given field values. Throws an exception if the field values do
        /// not match its fields.
        /// </summary>
        /// <returns>The structure.</returns>
        /// <param name="type">The type of the structure.</param>
        /// <param name="fieldValues">The values of the structure's fields,
        /// in the order the structure declares them.</param>
        [KRPCMethod]
        public static Expression CreateStruct (Type type, IList<Expression> fieldValues)
        {
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            if (ReferenceEquals (fieldValues, null))
                throw new ArgumentNullException (nameof (fieldValues));
            // A nullable structure type constructs the structure it holds
            var structType = type.Spec.Type;
            if (!TypeUtils.IsAStructType (structType))
                throw new ArgumentException (structType + " is not a structure type");
            var fields = TypeUtils.GetStructFields (structType);
            if (fieldValues.Count != fields.Count)
                throw new ArgumentException (
                    structType + " has " + fields.Count + " fields, got " +
                    fieldValues.Count + " field values");
            CheckNoNullElements (fieldValues, "field values of a structure");
            var bindings = new MemberBinding [fields.Count];
            for (var i = 0; i < fields.Count; i++) {
                var field = fields [i];
                var value = ConvertTo (
                    fieldValues [i], TypeUtils.GetStructFieldSpec (structType, field),
                    "The value of field " + field.Name);
                if (!field.PropertyType.IsAssignableFrom (value.Type))
                    throw new InvalidOperationException (
                        "Incorrect expression type for field " + field.Name + " of " +
                        structType + ". Expected an expression of type " +
                        field.PropertyType + ", got " + value.Type);
                bindings [i] = LinqExpression.Bind (field, value);
            }
            return new Expression (
                LinqExpression.MemberInit (LinqExpression.New (structType), bindings));
        }

        /// <summary>
        /// A list of the given values, with numbers widened to their common type. Throws an
        /// exception if there are no values.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="values">The values.</param>
        [KRPCMethod]
        public static Expression CreateList (IList<Expression> values)
        {
            CheckHasValues (values, nameof (values), "list", "CreateEmptyList");
            CheckNoNullElements (values, "values of a list");
            CheckEachHasValue (values, "A value of a list");
            var valueType = CommonType (values, "values of a list");
            var listType = typeof (List<>).MakeGenericType (valueType);
            var ctor = listType.GetConstructor (new [] { typeof (IEnumerable<>).MakeGenericType (valueType) });
            var args = LinqExpression.NewArrayInit (valueType, Widened (values, valueType));
            var list = AsCollectionInterface (LinqExpression.New (ctor, args));
            return new Expression (list, SpecWith (list.Type, CommonSpec (values, valueType)));
        }

        /// <summary>
        /// A set of the given values, with numbers widened to their common type. Throws an
        /// exception if there are no values, or a value is null.
        /// </summary>
        /// <remarks>
        /// The values are evaluated in an unspecified order.
        /// </remarks>
        /// <returns>The set.</returns>
        /// <param name="values">The values.</param>
        [KRPCMethod]
        public static Expression CreateSet (HashSet<Expression> values)
        {
            CheckHasValues (values, nameof (values), "set", "CreateEmptySet");
            CheckNoNullElements (values, "values of a set");
            CheckEachHasValue (values, SetValuePosition);
            var valueType = NonNullableType (CommonType (values, "values of a set"));
            var element = CommonSpec (values, valueType).WithNullable (false);
            var args = LinqExpression.NewArrayInit (
                valueType, Widened (values, valueType, element, SetValuePosition));
            var set = SetOfCall (valueType, args);
            return new Expression (set, SpecWith (set.Type, element));
        }

        /// <summary>
        /// A dictionary of the given keys and corresponding values, with numbers widened to
        /// their common type. Throws an exception if there are no keys, the counts differ, the
        /// keys are not of a valid key type, or a key is null.
        /// </summary>
        /// <remarks>
        /// A repeated key takes the last of its values.
        /// </remarks>
        /// <returns>The dictionary.</returns>
        /// <param name="keys">The keys.</param>
        /// <param name="values">The values.</param>
        [KRPCMethod]
        public static Expression CreateDictionary (IList<Expression> keys, IList<Expression> values)
        {
            CheckHasValues (keys, nameof (keys), "dictionary", "CreateEmptyDictionary");
            CheckHasValues (values, nameof (values), "dictionary", "CreateEmptyDictionary");
            if (keys.Count != values.Count)
                throw new ArgumentException (
                    "A dictionary needs as many values as keys, got " + keys.Count +
                    " keys and " + values.Count + " values");
            CheckNoNullElements (keys, "keys of a dictionary");
            CheckNoNullElements (values, "values of a dictionary");
            CheckEachHasValue (keys, DictionaryKeyPosition);
            CheckEachHasValue (values, "A value of a dictionary");
            var keyType = NonNullableType (CommonType (keys, "keys of a dictionary"));
            var valueType = CommonType (values, "values of a dictionary");
            if (!TypeUtils.IsAValidKeyType (keyType))
                throw new ArgumentException (keyType + " is not a valid dictionary key type");
            var method = typeof(Expression).GetMethod("CreateDictionaryHelper", BindingFlags.Static | BindingFlags.NonPublic);
            method = method.MakeGenericMethod (keyType, valueType);
            var keysArg = LinqExpression.NewArrayInit (
                keyType, Widened (keys, keyType, TypeSpec.Create (keyType), DictionaryKeyPosition));
            var valuesArg = LinqExpression.NewArrayInit (valueType, Widened (values, valueType));
            var call = LinqExpression.Call (method, keysArg, valuesArg);
            return new Expression (call, SpecWith (call.Type, null, CommonSpec (values, valueType)));
        }

        /// <summary>
        /// Check that a collection of expressions holds an expression at every position.
        /// The factories read the type and the built node of each one.
        /// </summary>
        static void CheckNoNullElements (IEnumerable<Expression> values, string what)
        {
            foreach (var value in values)
                if (ReferenceEquals (value, null))
                    throw new ArgumentException ("The " + what + " cannot be null");
        }

        static void CheckEachHasValue (IEnumerable<Expression> values, string position)
        {
            foreach (var value in values)
                CheckHasValue (value, position);
        }

        /// <summary>
        /// Check that a collection is being constructed from at least one value. The
        /// element type is taken from the values, so an empty collection has to be
        /// constructed by the factory that is given the type instead.
        /// </summary>
        static void CheckHasValues (
            ICollection<Expression> values, string name, string kind, string emptyFactory)
        {
            if (ReferenceEquals (values, null))
                throw new ArgumentNullException (name);
            if (values.Count == 0)
                throw new ArgumentException (
                    "Cannot determine the element type of an empty " + kind +
                    ". Use " + emptyFactory + " to construct one.");
        }

        /// <summary>
        /// The common type of the values a collection is being constructed from. Throws
        /// when there is none.
        /// </summary>
        static System.Type CommonType (IEnumerable<Expression> values, string what)
        {
            return CommonType (values.Select (x => x.Type).ToList (), what);
        }

        static System.Type CommonType (IList<System.Type> types, string what)
        {
            var common = CommonType (types);
            if (common == null) {
                // Sorted, so the message does not depend on the order of a set's values
                var names = types.Select (x => CollectionInterfaceType (x).ToString ())
                    .Distinct ().OrderBy (x => x, StringComparer.Ordinal);
                throw new InvalidOperationException (
                    "The " + what + " must all be of the same type, or numbers with a " +
                    "common type, got " + string.Join (", ", names.ToArray ()));
            }
            return common;
        }

        /// <summary>
        /// The common type of a set of types, or null when there is none. It depends only
        /// on which types are present: it is nullable wherever any type is, the widest of
        /// a set of numbers, and tuples and collections join position by position.
        /// </summary>
        static System.Type CommonType (IEnumerable<System.Type> types)
        {
            var distinct = types.Select (CollectionInterfaceType).Distinct ().ToList ();
            if (distinct.Count == 1)
                return distinct [0];
            var nullable = distinct.Any (x => NullableValueType (x) != null);
            var values = distinct.Select (NonNullableType).Distinct ().ToList ();
            System.Type common = null;
            if (values.Count == 1)
                common = values [0];
            else if (values.All (IsNumericType))
                common = FindCommonNumericType (values);
            else
                common = CommonContainedType (values);
            if (common == null || !nullable)
                return common;
            return typeof (Nullable<>).MakeGenericType (common);
        }

        /// <summary>
        /// The common type of a set of tuples, or of collections of one kind, with the
        /// common type at each position. Null when there is none.
        /// </summary>
        static System.Type CommonContainedType (IList<System.Type> types)
        {
            if (!types.All (IsAJoinableType))
                return null;
            var definition = types [0].GetGenericTypeDefinition ();
            if (types.Any (x => x.GetGenericTypeDefinition () != definition))
                return null;
            var common = new System.Type [definition.GetGenericArguments ().Length];
            for (int i = 0; i < common.Length; i++) {
                var position = i;
                common [i] = CommonType (types.Select (x => x.GetGenericArguments () [position]));
                if (common [i] == null)
                    return null;
            }
            return definition.MakeGenericType (common);
        }

        static IEnumerable<LinqExpression> Widened (IEnumerable<Expression> values, System.Type type)
        {
            return values.Select (x => Widen (x.internalExpression, type));
        }

        /// <summary>
        /// The values a collection is constructed from, widened to its element type and
        /// checked to hold no null the element type cannot.
        /// </summary>
        static IEnumerable<LinqExpression> Widened (
            IEnumerable<Expression> values, System.Type type, TypeSpec element, string position)
        {
            return values.Select (x => CheckNulls (
                Widen (UnwrapNotNull (x.internalExpression, type, position), type),
                SpecAs (x, type), element, position));
        }

        const string SetValuePosition = "A value of a set";
        const string DictionaryKeyPosition = "A key of a dictionary";

        /// <summary>
        /// The given type, or the value type a nullable value type holds. A set's values and
        /// a dictionary's keys cannot be null.
        /// </summary>
        static System.Type NonNullableType (System.Type type)
        {
            return NullableValueType (type) ?? type;
        }

        /// <summary>
        /// The value a nullable value type holds, when going to a position of the given type
        /// that cannot hold null. Throws when evaluated if the value is null.
        /// </summary>
        static LinqExpression UnwrapNotNull (LinqExpression value, System.Type type, string position)
        {
            if (NullableValueType (type) != null)
                return value;
            return UnwrapNullable (value, position);
        }

        /// <summary>
        /// The spec of the element type of a collection being constructed from the given
        /// values, nullable wherever one of the values is.
        /// </summary>
        static TypeSpec CommonSpec (IEnumerable<Expression> values, System.Type type)
        {
            TypeSpec result = null;
            foreach (var value in values) {
                var valueSpec = SpecAs (value, type);
                result = result == null ? valueSpec : TypeSpec.Join (result, valueSpec);
            }
            return result;
        }

        static IDictionary<Key, Value> CreateDictionaryHelper<Key, Value> (Key[] keys, Value[] values)
        {
            var dictionary = new Dictionary<Key, Value> ();
            for (int i = 0; i < keys.Length; i++)
                dictionary [keys [i]] = values [i];
            return dictionary;
        }

        /// <summary>
        /// An empty list of the given value type.
        /// </summary>
        /// <returns>The empty list.</returns>
        /// <param name="valueType">The type of the values the list holds.</param>
        [KRPCMethod]
        public static Expression CreateEmptyList (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            var listType = typeof (List<>).MakeGenericType (valueType.InternalType);
            var list = AsCollectionInterface (LinqExpression.New (listType.GetConstructor (System.Type.EmptyTypes)));
            return new Expression (list, SpecWith (list.Type, valueType.Spec));
        }

        /// <summary>
        /// An empty set of the given value type. Throws an exception if the value type is
        /// nullable.
        /// </summary>
        /// <returns>The empty set.</returns>
        /// <param name="valueType">The type of the values the set holds.</param>
        [KRPCMethod]
        public static Expression CreateEmptySet (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            if (valueType.Nullable)
                throw new ArgumentException ("The elements of a set cannot be nullable");
            var set = SetOfCall (valueType.InternalType, LinqExpression.NewArrayInit (valueType.InternalType));
            return new Expression (set, SpecWith (set.Type, valueType.Spec));
        }

        /// <summary>
        /// An empty dictionary of the given key and value types. Throws an exception if the
        /// key type is nullable or not a valid key type.
        /// </summary>
        /// <returns>The empty dictionary.</returns>
        /// <param name="keyType">The type of the dictionary's keys.</param>
        /// <param name="valueType">The type of the dictionary's values.</param>
        [KRPCMethod]
        public static Expression CreateEmptyDictionary (Type keyType, Type valueType)
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
            var dictionaryType = typeof (Dictionary<,>).MakeGenericType (
                keyType.InternalType, valueType.InternalType);
            var dictionary = AsCollectionInterface (
                LinqExpression.New (dictionaryType.GetConstructor (System.Type.EmptyTypes)));
            return new Expression (dictionary, SpecWith (dictionary.Type, null, valueType.Spec));
        }

        /// <summary>
        /// The value of a field of a structure. Throws an exception if the value is not a
        /// structure, or has no field of the given name.
        /// </summary>
        /// <returns>The value of the field.</returns>
        /// <param name="arg">The structure.</param>
        /// <param name="name">The name of the field to access.</param>
        [KRPCMethod]
        public static Expression GetField (Expression arg, string name)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var structure = UnwrapNullable (arg, "The structure");
            var argType = structure.Type;
            if (!TypeUtils.IsAStructType (argType))
                throw new ArgumentException (argType + " is not a structure type");
            var field = TypeUtils.GetStructFields (argType)
                .FirstOrDefault (property => property.Name == name);
            if (field == null)
                throw new ArgumentException (
                    argType + " does not have a field called \"" + name + "\"");
            return new Expression (
                LinqExpression.Property (structure, field), TypeUtils.GetStructFieldSpec (argType, field));
        }
    }
}
