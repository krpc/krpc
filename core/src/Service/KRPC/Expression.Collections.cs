using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        /// Whether a collection holds a value, compared with <see cref="ValueEquals"/>.
        /// </summary>
        static bool ContainsHelper<T> (IEnumerable<T> collection, T value)
        {
            var set = collection as HashSet<T>;
            if (set != null && set.Comparer == ValuesComparer<T> ())
                return set.Contains (value);
            foreach (var element in collection) {
                if (ValueEquals (element, value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// A lookup of a value in a collection, as a call to the given function with the
        /// collection and the value converted to the given type. A null finds no match in
        /// a collection that cannot hold one, so its lookup gives false.
        /// </summary>
        static LinqExpression LookUp (
            Expression collection, Expression value, System.Type type, bool holdsNull, string position,
            Func<LinqExpression, LinqExpression, LinqExpression> lookUp)
        {
            var nullValueType = NullableValueType (value.Type) != null;
            var nullReference = !value.Type.IsValueType && value.Spec != null && value.Spec.Nullable;
            if (holdsNull || !(nullValueType || nullReference))
                return lookUp (collection, ConvertElement (value, type, position));
            var nullableType = type.IsValueType ? typeof (Nullable<>).MakeGenericType (type) : type;
            var source = LinqExpression.Variable (collection.Type, "collection");
            var sought = LinqExpression.Variable (nullableType, "value");
            var isNull = type.IsValueType
                ? (LinqExpression)LinqExpression.Not (LinqExpression.Property (sought, "HasValue"))
                : LinqExpression.ReferenceEqual (sought, LinqExpression.Constant (null, type));
            // The collection is evaluated before the value, as in the lookup without a check
            return LinqExpression.Block (
                new [] { source, sought },
                LinqExpression.Assign (source, collection),
                LinqExpression.Assign (sought, ConvertElement (value, nullableType, position)),
                LinqExpression.Condition (
                    isNull, LinqExpression.Constant (false),
                    lookUp (source, type.IsValueType ? LinqExpression.Property (sought, "Value") : (LinqExpression)sought)));
        }

        /// <summary>
        /// A collection as a list.
        /// </summary>
        /// <returns>The collection as a list.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression ToList (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to ToList");
            var valueType = GetEnumerableValueType (arg);
            var toList = typeof (Enumerable).GetMethod ("ToList");
            toList = toList.MakeGenericMethod (valueType);
            var list = AsCollectionInterface (LinqExpression.Call (toList, arg));
            return new Expression (list, SpecWith (list.Type, ElementSpec (arg)));
        }

        /// <summary>
        /// A collection as a set.
        /// </summary>
        /// <remarks>
        /// Throws an exception if a value is null.
        /// </remarks>
        /// <returns>The collection as a set.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression ToSet (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to ToSet");
            var valueType = GetEnumerableValueType (arg);
            var element = ElementSpec (arg);
            LinqExpression values = arg;
            if (NullableValueType (valueType) != null) {
                var value = LinqExpression.Parameter (valueType, "value");
                var select = SelectMethod (valueType, NonNullableType (valueType));
                values = LinqExpression.Call (select, arg, LinqExpression.Lambda (
                    UnwrapNotNull (value, NonNullableType (valueType), SetValuePosition), value));
                valueType = NonNullableType (valueType);
            }
            // The set is checked once built, as a lazily evaluated sequence would be
            // evaluated a second time by checking it first
            var set = SetOfCall (valueType, values);
            var setType = set.Type;
            var spec = SpecWith (setType, element == null ? null : element.WithNullable (false));
            return new Expression (CheckNulls (set, SpecWith (setType, element), spec, SetValuePosition), spec);
        }

        /// <summary>
        /// An element of a tuple, list or dictionary.
        /// </summary>
        /// <remarks>
        /// A tuple takes a constant integer index, a list a zero indexed integer and a
        /// dictionary a key. Throws an exception if the index is out of range or the key is
        /// missing, and for a tuple when the expression is created.
        /// </remarks>
        /// <returns>The element.</returns>
        /// <param name="arg">The tuple, list or dictionary.</param>
        /// <param name="index">The index or key of the element.</param>
        [KRPCMethod]
        public static Expression Get (Expression arg, Expression index)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Get");
            if (ReferenceEquals (index, null))
                throw new ArgumentNullException (nameof (index));
            CheckIsNotAString (arg);
            CheckIsNotBytes (arg);
            var argType = arg.Type;
            if (argType.Name.StartsWith("Tuple`", StringComparison.Ordinal)) {
                // The elements of a tuple differ in type, so the index is read when the tree is built
                var constant = index.internalExpression as ConstantExpression;
                if (constant == null || !(constant.Value is int))
                    throw new ArgumentException (
                        "The index into a tuple must be a constant integer");
                var tupleIndex = (int)constant.Value;
                var property = argType.GetProperty ("Item" + (tupleIndex + 1));
                if (property == null)
                    throw IndexOutOfRange (tupleIndex, argType.GetGenericArguments ().Length);
                return new Expression (
                    LinqExpression.Property (arg, property), arg.Spec.Types [tupleIndex]);
            }
            var method = argType.GetMethod ("get_Item");
            if (method == null)
                throw new InvalidOperationException (
                    argType + " does not have elements that can be accessed by index");
            // A list is indexed by position, and a dictionary by a key of its own type
            var indexType = method.GetParameters () [0].ParameterType;
            if (IsADictionary (arg)) {
                var types = DictionaryTypes (arg);
                var helper = typeof (Expression)
                    .GetMethod (nameof (DictionaryGetHelper), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod (types);
                return new Expression (
                    LinqExpression.Call (helper, arg, ConvertTo (index, TypeSpec.Create (types [0]), DictionaryKeyPosition)),
                    ElementSpec (arg));
            }
            CheckIsAnInt (index, nameof (index));
            return ElementOf (LinqExpression.Call (
                CollectionHelper (nameof (ListGetHelper), method.ReturnType), arg,
                ConvertElement (index, indexType, "The index")), arg);
        }

        // The list indexer throws ArgumentOutOfRangeException, and an index out of range
        // is an IndexOutOfRangeException to a client

        internal static T ListGetHelper<T> (IList<T> list, int index)
        {
            CheckIndex (index, list.Count);
            return list [index];
        }

        internal static void CheckIndex (int index, int count)
        {
            if (index < 0 || index >= count)
                throw IndexOutOfRange (index, count);
        }

        static IndexOutOfRangeException IndexOutOfRange (int index, int count)
        {
            return new IndexOutOfRangeException (
                "The index " + index + " is out of range for " + count + " elements");
        }

        static MethodInfo CollectionHelper (string name, System.Type valueType)
        {
            return typeof (Expression)
                .GetMethod (name, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (valueType);
        }

        internal static TValue DictionaryGetHelper<TKey, TValue> (
            IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            if (!dictionary.TryGetValue (key, out value))
                throw new KeyNotFoundException (
                    "The key " + Convert.ToString (key, CultureInfo.InvariantCulture) + " is not in the dictionary");
            return value;
        }

        /// <summary>
        /// The number of values in a collection.
        /// </summary>
        /// <returns>The number of elements in the collection.</returns>
        /// <param name="arg">The list, set or dictionary.</param>
        [KRPCMethod]
        public static Expression Count (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Count");
            CheckIsEnumerable (arg);
            var count = GetCountProperty (arg.Type);
            if (count == null)
                throw new InvalidOperationException (
                    "A lazily evaluated sequence does not have a count. " +
                    "Convert it to a list or a set first.");
            return new Expression (LinqExpression.Property (arg, count));
        }

        /// <summary>
        /// The sum of the values of a collection.
        /// </summary>
        /// <returns>The sum of the elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Sum (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Sum");
            return new Expression (AggregateCall ("Sum", arg));
        }

        /// <summary>
        /// The largest value of a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty and its values are not nullable.
        /// </remarks>
        /// <returns>The largest value in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Max (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Max");
            return new Expression (AggregateCall ("Max", arg));
        }

        /// <summary>
        /// The smallest value of a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty and its values are not nullable.
        /// </remarks>
        /// <returns>The smallest value in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Min (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Min");
            return new Expression (AggregateCall ("Min", arg));
        }

        /// <summary>
        /// The average of the values of a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty and its values are not nullable.
        /// </remarks>
        /// <returns>The average of the elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Average (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Average");
            return new Expression (AggregateCall ("Average", arg));
        }

        static readonly System.Type[] integerTypes =
            { typeof (int), typeof (long), typeof (uint), typeof (ulong) };

        // An integer sum wraps on overflow, as the other integer operators do, and
        // skips a null as Enumerable does. Enumerable has no Sum or Average over
        // unsigned integers.
        static LinqExpression AggregateCall (string name, Expression arg)
        {
            var element = GetEnumerableValueType (arg);
            var number = Nullable.GetUnderlyingType (element) ?? element;
            if (name == "Sum" && integerTypes.Contains (number)) {
                var total = LinqExpression.Parameter (element, "total");
                var value = LinqExpression.Parameter (element, "value");
                LinqExpression sum = LinqExpression.Add (total, value);
                if (element != number)
                    sum = LinqExpression.Condition (
                        LinqExpression.Property (value, "HasValue"), sum, total);
                var add = LinqExpression.Lambda (
                    typeof (Func<,,>).MakeGenericType (element, element, element),
                    sum, total, value);
                var aggregate = typeof (Enumerable).GetMethods ()
                    .Single (x => x.Name == "Aggregate" && x.GetGenericArguments ().Length == 2 &&
                             x.GetParameters ().Length == 3)
                    .MakeGenericMethod (element, element);
                return LinqExpression.Call (
                    aggregate, arg, LinqExpression.Constant (Convert.ChangeType (0, number), element), add);
            }
            LinqExpression source = arg;
            // The average of longs is taken over doubles, as Enumerable sums longs checked
            if (name == "Average" &&
                (number == typeof (long) || number == typeof (uint) || number == typeof (ulong))) {
                var doubleType = element == number ? typeof (double) : typeof (double?);
                var value = LinqExpression.Parameter (element, "value");
                var select = SelectMethod (element, doubleType);
                source = LinqExpression.Call (
                    select, arg, LinqExpression.Lambda (
                        LinqExpression.Convert (value, doubleType), value));
            }
            var method = typeof (Enumerable).GetMethod (name, new [] { source.Type });
            if (method == null && (name == "Max" || name == "Min") &&
                (number == typeof (uint) || number == typeof (ulong)))
                method = typeof (Enumerable).GetMethods ()
                    .Single (x => x.Name == name && x.IsGenericMethodDefinition &&
                             x.GetGenericArguments ().Length == 1 && x.GetParameters ().Length == 1 &&
                             TakesASequence (x))
                    .MakeGenericMethod (element);
            if (method == null)
                throw new InvalidOperationException (
                    name + " is not defined over a collection of type " + arg.Type);
            return LinqExpression.Call (method, source);
        }

        /// <summary>
        /// The result of a function for each value of a collection.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The collection of function results.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The function, taking an element of the collection.</param>
        [KRPCMethod]
        public static Expression Select (Expression arg, Expression func)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Select");
            var sourceType = GetEnumerableValueType (arg);
            var resultType = GetFunctionResultType (func, nameof (func), 1);
            CheckIsFunction (func, sourceType, resultType);
            var select = SelectMethod (sourceType, resultType);
            var call = LinqExpression.Call (
                select, arg, AdaptFunction (func, "A value given to the function", ElementSpec (arg)));
            return new Expression (call, SpecWith (call.Type, ResultSpec (func, resultType)));
        }

        /// <summary>
        /// The values of a collection for which a predicate function returns true.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The filtered collection.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The predicate function, taking an element of the collection.</param>
        [KRPCMethod]
        public static Expression Where (Expression arg, Expression func)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Where");
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (func, sourceType, typeof(bool));
            var where = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "Where" &&
                         x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2);
            where = where.MakeGenericMethod (sourceType);
            var call = LinqExpression.Call (
                where, arg, AdaptFunction (func, "A value given to the function", ElementSpec (arg)));
            return new Expression (call, SpecWith (call.Type, ElementSpec (arg)));
        }

        /// <summary>
        /// A collection without its first count values.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The collection without its first count values.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="count">The number of values to skip.</param>
        [KRPCMethod]
        public static Expression Skip (Expression arg, Expression count)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Skip");
            CheckIsAnInt (count, nameof (count));
            var sourceType = GetEnumerableValueType (arg);
            var skip = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "Skip" && x.GetParameters ().Length == 2 && TakesASequence (x) &&
                x.GetParameters () [1].ParameterType == typeof (int));
            skip = skip.MakeGenericMethod (sourceType);
            var call = LinqExpression.Call (skip, arg, count);
            return new Expression (call, SpecWith (call.Type, ElementSpec (arg)));
        }

        /// <summary>
        /// The first count values of a collection.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The first count values of the collection.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="count">The number of values to take.</param>
        [KRPCMethod]
        public static Expression Take (Expression arg, Expression count)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Take");
            CheckIsAnInt (count, nameof (count));
            var sourceType = GetEnumerableValueType (arg);
            var take = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "Take" && x.GetParameters () [1].ParameterType == typeof (int));
            take = take.MakeGenericMethod (sourceType);
            var call = LinqExpression.Call (take, arg, count);
            return new Expression (call, SpecWith (call.Type, ElementSpec (arg)));
        }

        /// <summary>
        /// The values of the collections a function returns for each value of a collection,
        /// flattened into one.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The flattened collection of function results.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The function, taking an element of the collection
        /// and returning a collection.</param>
        [KRPCMethod]
        public static Expression SelectMany (Expression arg, Expression func)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to SelectMany");
            var sourceType = GetEnumerableValueType (arg);
            var funcResultType = GetFunctionResultType (func, nameof (func), 1);
            CheckIsFunction (func, sourceType, funcResultType);
            if (!typeof (IEnumerable).IsAssignableFrom (funcResultType) ||
                !funcResultType.IsGenericType ||
                global::KRPC.Utils.Reflection.IsGenericType (funcResultType, typeof (IDictionary<,>)))
                throw new InvalidOperationException (
                    "The function must return a collection of values, such as a list or a set");
            var resultType = funcResultType.GetGenericArguments () [0];
            var selectMany = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "SelectMany" &&
                         x.GetParameters ().Length == 2 &&
                         x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2);
            selectMany = selectMany.MakeGenericMethod (sourceType, resultType);
            var function = AdaptFunction (func, "A value given to the function", ElementSpec (arg));
            var funcResult = ResultSpec (func, funcResultType);
            if (funcResult.Nullable) {
                var value = LinqExpression.Parameter (sourceType, "value");
                function = LinqExpression.Lambda (CheckNulls (
                    LinqExpression.Invoke (function, value), funcResult, funcResult.WithNullable (false),
                    "A collection the function given to SelectMany returns"), value);
            }
            var call = LinqExpression.Call (selectMany, arg, function);
            return new Expression (call, SpecWith (
                call.Type, funcResult.Types.Count == 1 ? funcResult.Types [0] : null));
        }

        /// <summary>
        /// A dictionary built from a collection, with a key and a value computed from each
        /// element.
        /// </summary>
        /// <remarks>
        /// A repeated key takes the last of its values.
        /// </remarks>
        /// <returns>The dictionary.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="keyFunc">The function computing an element's key.</param>
        /// <param name="valueFunc">The function computing an element's value.</param>
        [KRPCMethod]
        public static Expression BuildDictionary (Expression arg, Expression keyFunc, Expression valueFunc)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to BuildDictionary");
            var sourceType = GetEnumerableValueType (arg);
            System.Type keyType;
            var keys = KeyFunction (arg, keyFunc, nameof (keyFunc), out keyType);
            if (!TypeUtils.IsAValidKeyType (keyType))
                throw new InvalidOperationException (keyType + " is not a valid dictionary key type");
            var valueType = GetFunctionResultType (valueFunc, nameof (valueFunc), 1);
            CheckIsFunction (valueFunc, sourceType, valueType);
            var helper = typeof (Expression)
                .GetMethod (nameof (BuildDictionaryHelper), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (sourceType, keyType, valueType);
            var call = LinqExpression.Call (
                helper, arg, keys,
                AdaptFunction (valueFunc, "A value given to the function", ElementSpec (arg)));
            return new Expression (call, SpecWith (call.Type, null, ResultSpec (valueFunc, valueType)));
        }

        static IDictionary<TKey, TValue> BuildDictionaryHelper<TSource, TKey, TValue> (
            IEnumerable<TSource> source, Func<TSource, TKey> key, Func<TSource, TValue> value)
        {
            var dictionary = new Dictionary<TKey, TValue> ();
            foreach (var element in source)
                dictionary [key (element)] = value (element);
            return dictionary;
        }

        internal static IList<TKey> DictionaryKeysHelper<TKey, TValue> (
            IDictionary<TKey, TValue> dictionary)
        {
            return new List<TKey> (dictionary.Keys);
        }

        internal static IList<TValue> DictionaryValuesHelper<TKey, TValue> (
            IDictionary<TKey, TValue> dictionary)
        {
            return new List<TValue> (dictionary.Values);
        }

        /// <summary>
        /// The keys of a dictionary, as a list.
        /// </summary>
        /// <remarks>
        /// The collection operations take the keys of a dictionary from here, and its values
        /// from <see cref="DictionaryValues"/>.
        /// </remarks>
        /// <returns>The keys of the dictionary.</returns>
        /// <param name="dictionary">The dictionary.</param>
        [KRPCMethod]
        public static Expression DictionaryKeys (Expression dictionary)
        {
            dictionary = NotNullOperand (dictionary, nameof (dictionary), "The dictionary given to DictionaryKeys");
            var call = DictionaryPartCall (dictionary, nameof (DictionaryKeysHelper));
            return new Expression (call, SpecWith (call.Type, dictionary.Spec.Types [0]));
        }

        /// <summary>
        /// The values of a dictionary, as a list.
        /// </summary>
        /// <returns>The values of the dictionary.</returns>
        /// <param name="dictionary">The dictionary.</param>
        [KRPCMethod]
        public static Expression DictionaryValues (Expression dictionary)
        {
            dictionary = NotNullOperand (dictionary, nameof (dictionary), "The dictionary given to DictionaryValues");
            var call = DictionaryPartCall (dictionary, nameof (DictionaryValuesHelper));
            return new Expression (call, SpecWith (call.Type, dictionary.Spec.Types [1]));
        }

        // Selecting a single value out of a collection

        /// <summary>
        /// The first value of a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty.
        /// </remarks>
        /// <returns>The first value.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression First (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to First");
            return ElementOf (EnumerableCall ("First", 1, arg), arg);
        }

        /// <summary>
        /// The last value of a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty.
        /// </remarks>
        /// <returns>The last value.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression Last (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Last");
            return ElementOf (EnumerableCall ("Last", 1, arg), arg);
        }

        /// <summary>
        /// The value at the given position in a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the index is out of range.
        /// </remarks>
        /// <returns>The value at the given position.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="index">The position of the value, counting from zero.</param>
        [KRPCMethod]
        public static Expression ElementAt (Expression arg, Expression index)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to ElementAt");
            CheckIsAnInt (index, nameof (index));
            var sourceType = GetEnumerableValueType (arg);
            return ElementOf (LinqExpression.Call (
                CollectionHelper (nameof (ElementAtHelper), sourceType), arg, index), arg);
        }

        internal static T ElementAtHelper<T> (IEnumerable<T> source, int index)
        {
            var list = source as IList<T>;
            if (list != null)
                return ListGetHelper (list, index);
            var count = 0;
            foreach (var value in source) {
                if (count == index)
                    return value;
                count++;
            }
            throw IndexOutOfRange (index, count);
        }

        internal static TSource MinByHelper<TSource, TKey> (
            IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            return ByKeyHelper (source, selector, -1);
        }

        internal static TSource MaxByHelper<TSource, TKey> (
            IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            return ByKeyHelper (source, selector, 1);
        }

        /// <summary>
        /// The order of keys, with strings compared by character code as a client compares them.
        /// </summary>
        static IComparer<TKey> KeyComparer<TKey> ()
        {
            if (typeof (TKey) == typeof (string))
                return (IComparer<TKey>)StringComparer.Ordinal;
            if (TypeUtils.IsATupleCollectionType (typeof (TKey)))
                return new OrdinalTupleComparer<TKey> ();
            return Comparer<TKey>.Default;
        }

        /// <summary>
        /// Orders tuples element by element, comparing a string element by character code.
        /// A tuple's default order compares its elements with the culture sensitive
        /// default comparer.
        /// </summary>
        sealed class OrdinalTupleComparer<TKey> : IComparer<TKey>, IComparer
        {
            public int Compare (TKey x, TKey y)
            {
                return Compare ((object)x, (object)y);
            }

            public int Compare (object x, object y)
            {
                if (x is string || y is string)
                    return string.CompareOrdinal ((string)x, (string)y);
                var tuple = x as IStructuralComparable;
                if (tuple != null)
                    return tuple.CompareTo (y, this);
                return Comparer.Default.Compare (x, y);
            }
        }

        /// <summary>
        /// The value of a collection whose key is the smallest or the largest,
        /// selected in a single pass.
        /// </summary>
        static TSource ByKeyHelper<TSource, TKey> (
            IEnumerable<TSource> source, Func<TSource, TKey> selector, int wanted)
        {
            var comparer = KeyComparer<TKey> ();
            bool found = false;
            TSource best = default (TSource);
            TKey bestKey = default (TKey);
            foreach (var value in source) {
                var key = selector (value);
                if (!found || System.Math.Sign (comparer.Compare (key, bestKey)) == wanted) {
                    best = value;
                    bestKey = key;
                    found = true;
                }
            }
            if (!found)
                throw new InvalidOperationException ("The collection is empty");
            return best;
        }

        /// <summary>
        /// The value of a collection with the smallest key.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty.
        /// </remarks>
        /// <returns>The value with the smallest key.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="key">A function producing the key to compare values by.</param>
        [KRPCMethod]
        public static Expression MinBy (Expression arg, Expression key)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to MinBy");
            return ElementOf (ByKeyCall (nameof (MinByHelper), arg, key), arg);
        }

        /// <summary>
        /// The value of a collection with the largest key.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty.
        /// </remarks>
        /// <returns>The value with the largest key.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="key">A function producing the key to compare values by.</param>
        [KRPCMethod]
        public static Expression MaxBy (Expression arg, Expression key)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to MaxBy");
            return ElementOf (ByKeyCall (nameof (MaxByHelper), arg, key), arg);
        }

        // Reshaping a collection. Each produces a lazily evaluated sequence unless
        // stated otherwise. Use ToList or ToSet to convert one to a concrete
        // collection before it is returned to a client

        /// <summary>
        /// The values of a collection with duplicates removed, keeping the first
        /// occurrence of each.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The distinct values of the collection.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression Distinct (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Distinct");
            var sourceType = GetEnumerableValueType (arg);
            var distinct = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "Distinct" && x.GetParameters ().Length == 2 && TakesASequence (x));
            return SequenceOf (LinqExpression.Call (
                distinct.MakeGenericMethod (sourceType), arg, ValuesComparerCall (sourceType)),
                ElementSpec (arg));
        }

        /// <summary>
        /// The values of a collection in the opposite order.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The reversed collection.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression Reverse (Expression arg)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Reverse");
            return SequenceOf (EnumerableCall ("Reverse", 1, arg), ElementSpec (arg));
        }

        /// <summary>
        /// The values in either of two collections, with duplicates removed.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The union of the two collections.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Union (Expression arg1, Expression arg2)
        {
            arg1 = NotNullOperand (arg1, nameof (arg1), "The first collection given to Union");
            arg2 = NotNullOperand (arg2, nameof (arg2), "The second collection given to Union");
            var valueType = JoinedValueType (arg1, arg2, nameof (Union));
            return SequenceOf (SetOperationCall ("Union", arg1, arg2, valueType), JoinedElementSpec (arg1, arg2, valueType));
        }

        /// <summary>
        /// The values in both of two collections, with duplicates removed.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The intersection of the two collections.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Intersect (Expression arg1, Expression arg2)
        {
            arg1 = NotNullOperand (arg1, nameof (arg1), "The first collection given to Intersect");
            arg2 = NotNullOperand (arg2, nameof (arg2), "The second collection given to Intersect");
            var valueType = JoinedValueType (arg1, arg2, nameof (Intersect));
            return SequenceOf (SetOperationCall ("Intersect", arg1, arg2, valueType), JoinedElementSpec (arg1, arg2, valueType));
        }

        /// <summary>
        /// The values of the first collection that are not in the second, with
        /// duplicates removed.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The difference between the two collections.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Except (Expression arg1, Expression arg2)
        {
            arg1 = NotNullOperand (arg1, nameof (arg1), "The first collection given to Except");
            arg2 = NotNullOperand (arg2, nameof (arg2), "The second collection given to Except");
            var valueType = JoinedValueType (arg1, arg2, nameof (Except));
            return SequenceOf (SetOperationCall ("Except", arg1, arg2, valueType), JoinedElementSpec (arg1, arg2, valueType));
        }

        /// <summary>
        /// The results of a function applied to the values of two collections in pairs.
        /// </summary>
        /// <remarks>
        /// The result is as long as the shorter of the two collections. It is lazily
        /// evaluated, and <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The combined collection.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        /// <param name="func">A function combining a value from each collection.</param>
        [KRPCMethod]
        public static Expression Zip (Expression arg1, Expression arg2, Expression func)
        {
            arg1 = NotNullOperand (arg1, nameof (arg1), "The first collection given to Zip");
            arg2 = NotNullOperand (arg2, nameof (arg2), "The second collection given to Zip");
            var sourceType1 = GetEnumerableValueType (arg1);
            var sourceType2 = GetEnumerableValueType (arg2);
            var resultType = GetFunctionResultType (func, nameof (func), 2);
            CheckIsFunction (func, sourceType1, sourceType2, resultType);
            // Newer frameworks also offer overloads pairing values into tuples and
            // combining three collections, so the one taking a function is named
            var zip = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "Zip" && x.GetParameters ().Length == 3 &&
                x.GetParameters () [2].ParameterType.IsGenericType &&
                x.GetParameters () [2].ParameterType.GetGenericTypeDefinition () ==
                typeof (Func<,,>));
            var call = LinqExpression.Call (
                zip.MakeGenericMethod (sourceType1, sourceType2, resultType),
                arg1, arg2,
                AdaptFunction (func, "A value given to the function", ElementSpec (arg1), ElementSpec (arg2)));
            return new Expression (call, SpecWith (call.Type, ResultSpec (func, resultType)));
        }

        internal static IDictionary<TKey, IList<TSource>> GroupByHelper<TSource, TKey> (
            IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            var groups = new Dictionary<TKey, IList<TSource>> ();
            foreach (var value in source) {
                var key = selector (value);
                IList<TSource> group;
                if (!groups.TryGetValue (key, out group)) {
                    group = new List<TSource> ();
                    groups [key] = group;
                }
                group.Add (value);
            }
            return groups;
        }

        /// <summary>
        /// The values of a collection grouped by the key a function produces for each of them.
        /// </summary>
        /// <returns>A dictionary of each key to the values that produced it.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="key">A function producing the key to group values by.</param>
        [KRPCMethod]
        public static Expression GroupBy (Expression arg, Expression key)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to GroupBy");
            System.Type keyType;
            var keys = KeyFunction (arg, key, nameof (key), out keyType);
            if (!TypeUtils.IsAValidKeyType (keyType))
                throw new InvalidOperationException (
                    keyType + " is not a valid dictionary key type, so it cannot be grouped by");
            var method = typeof (Expression)
                .GetMethod (nameof (GroupByHelper), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (GetEnumerableValueType (arg), keyType);
            var call = LinqExpression.Call (method, arg, keys);
            var groups = call.Type.GetGenericArguments () [1];
            return new Expression (call, SpecWith (call.Type, null, SpecWith (groups, ElementSpec (arg))));
        }

        /// <summary>
        /// A value taken from a collection, with the spec of the collection's values.
        /// </summary>
        static Expression ElementOf (LinqExpression call, Expression collection)
        {
            var element = ElementSpec (collection);
            return new Expression (call, element == null ? null : TypeSpec.Create (call.Type, element));
        }

        /// <summary>
        /// A sequence of values with the given spec.
        /// </summary>
        static Expression SequenceOf (LinqExpression call, TypeSpec element)
        {
            return new Expression (call, SpecWith (call.Type, element));
        }

        /// <summary>
        /// The common type of the values of two collections combined into one by the named
        /// operation. Throws an exception if there is none.
        /// </summary>
        static System.Type JoinedValueType (Expression arg1, Expression arg2, string operation)
        {
            return CommonType (
                new [] { GetEnumerableValueType (arg1), GetEnumerableValueType (arg2) },
                "values of the collections given to " + operation);
        }

        /// <summary>
        /// The values of a collection, widened to the given type.
        /// </summary>
        static LinqExpression WidenedValues (Expression collection, System.Type type)
        {
            var sourceType = GetEnumerableValueType (collection);
            if (sourceType == type)
                return collection;
            var value = LinqExpression.Parameter (sourceType, "value");
            var select = SelectMethod (sourceType, type);
            return LinqExpression.Call (select, collection, LinqExpression.Lambda (Widen (value, type), value));
        }

        /// <summary>
        /// The spec of the values of two collections combined into one at the given type,
        /// nullable wherever the values of either are.
        /// </summary>
        static TypeSpec JoinedElementSpec (Expression arg1, Expression arg2, System.Type type)
        {
            var element1 = ElementSpec (arg1);
            var element2 = ElementSpec (arg2);
            if (element1 == null || element2 == null)
                return null;
            return TypeSpec.Join (TypeSpec.Create (type, element1), TypeSpec.Create (type, element2));
        }

        static System.Type[] DictionaryTypes (Expression dictionary)
        {
            CheckIsADictionary (dictionary);
            return dictionary.Type.GetGenericArguments ();
        }

        static LinqExpression DictionaryPartCall (Expression dictionary, string helper)
        {
            var types = DictionaryTypes (dictionary);
            var method = typeof (Expression)
                .GetMethod (helper, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (types);
            return LinqExpression.Call (method, dictionary);
        }

        /// <summary>
        /// A call of an Enumerable method taking only the collection, over the type of
        /// its values.
        /// </summary>
        static LinqExpression EnumerableCall (string name, int parameters, Expression arg)
        {
            var sourceType = GetEnumerableValueType (arg);
            var method = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == name && x.GetParameters ().Length == parameters &&
                TakesASequence (x));
            return LinqExpression.Call (method.MakeGenericMethod (sourceType), arg);
        }

        /// <summary>
        /// Whether an Enumerable method takes a sequence as its first parameter. Newer
        /// class libraries add overloads of some methods, such as Reverse, that take an
        /// array.
        /// </summary>
        static bool TakesASequence (MethodInfo method)
        {
            var parameters = method.GetParameters ();
            if (parameters.Length == 0)
                return false;
            var type = parameters [0].ParameterType;
            return type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IEnumerable<>);
        }

        /// <summary>
        /// A call of an Enumerable method combining two collections, with their values
        /// widened to the given type.
        /// </summary>
        static LinqExpression SetOperationCall (string name, Expression arg1, Expression arg2, System.Type valueType)
        {
            var method = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == name && x.GetParameters ().Length == 3 && TakesASequence (x) &&
                x.GetParameters () [1].ParameterType.IsGenericType &&
                x.GetParameters () [1].ParameterType.GetGenericTypeDefinition () == typeof (IEnumerable<>));
            return LinqExpression.Call (
                method.MakeGenericMethod (valueType), WidenedValues (arg1, valueType),
                WidenedValues (arg2, valueType), ValuesComparerCall (valueType));
        }

        /// <summary>
        /// A call of one of the helpers taking a collection and a function producing a
        /// key for each of its values.
        /// </summary>
        static LinqExpression ByKeyCall (string helper, Expression arg, Expression key)
        {
            var sourceType = GetEnumerableValueType (arg);
            var keyType = GetFunctionResultType (key, nameof (key), 1);
            CheckIsFunction (key, sourceType, keyType);
            CheckIsOrdered (keyType);
            var method = typeof (Expression)
                .GetMethod (helper, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (sourceType, keyType);
            return LinqExpression.Call (
                method, arg, AdaptFunction (key, "A value given to the function", ElementSpec (arg)));
        }

        /// <summary>
        /// The function computing a dictionary key from each value of a collection, and the
        /// type of the key. A nullable value type key gives its value, and a key is checked
        /// to not be null when the function runs.
        /// </summary>
        static LinqExpression KeyFunction (Expression arg, Expression key, string name, out System.Type keyType)
        {
            var sourceType = GetEnumerableValueType (arg);
            var resultType = GetFunctionResultType (key, name, 1);
            CheckIsFunction (key, sourceType, resultType);
            var function = AdaptFunction (key, "A value given to the function", ElementSpec (arg));
            keyType = NonNullableType (resultType);
            var value = LinqExpression.Parameter (sourceType, "value");
            LinqExpression result = LinqExpression.Invoke (function, value);
            var checkedResult = CheckNulls (
                UnwrapNotNull (result, keyType, DictionaryKeyPosition),
                ResultSpec (key, resultType), TypeSpec.Create (keyType), DictionaryKeyPosition);
            if (checkedResult == result)
                return function;
            return LinqExpression.Lambda (checkedResult, value);
        }

        /// <summary>
        /// Check that the values of a key type can be ordered: a number, string, boolean or
        /// enumeration, a nullable one, or a tuple of them.
        /// </summary>
        static void CheckIsOrdered (System.Type keyType)
        {
            if (!IsOrdered (keyType))
                throw new InvalidOperationException (
                    "The values of " + keyType + " have no order. A key must be a number, " +
                    "string, boolean or enumeration, or a tuple of them.");
        }

        static bool IsOrdered (System.Type type)
        {
            type = NonNullableType (type);
            if (IsNumericType (type) || type == typeof (string) || type == typeof (bool) || type.IsEnum)
                return true;
            return TypeUtils.IsATupleCollectionType (type) && type.GetGenericArguments ().All (IsOrdered);
        }

        static void CheckIsAnInt (Expression expression, string name)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (name);
            if (expression.Type != typeof (int))
                throw new InvalidOperationException (
                    "Expected an integer for " + name + "; use a cast to convert a value to one");
        }

        /// <summary>
        /// Whether a collection contains a value.
        /// </summary>
        /// <returns>Whether the collection contains the value.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="value">The value to look for.</param>
        [KRPCMethod]
        public static Expression Contains (Expression arg, Expression value)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Contains");
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (IsADictionary (arg))
                throw new InvalidOperationException (
                    "Use ContainsKey to look for a key of a dictionary");
            var sourceType = GetEnumerableValueType (arg);
            var byEquals = (sourceType.IsValueType && !TypeUtils.IsAStructType (sourceType)) ||
                sourceType == typeof (string);
            var contains = byEquals
                ? typeof (Enumerable).GetMethods ().Single (
                    x => x.Name == "Contains" && x.GetParameters ().Length == 2 && TakesASequence (x))
                : typeof (Expression).GetMethod (
                    nameof (ContainsHelper), BindingFlags.Static | BindingFlags.NonPublic);
            contains = contains.MakeGenericMethod (sourceType);
            return new Expression (LookUp (
                arg, value, sourceType, ElementSpec (arg).Nullable, "The value given to Contains",
                (x, y) => LinqExpression.Call (contains, x, y)));
        }

        /// <summary>
        /// Whether a dictionary contains a key.
        /// </summary>
        /// <returns>Whether the dictionary contains the key.</returns>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key to look for.</param>
        [KRPCMethod]
        public static Expression ContainsKey (Expression dictionary, Expression key)
        {
            dictionary = NotNullOperand (dictionary, nameof (dictionary), "The dictionary given to ContainsKey");
            if (ReferenceEquals (key, null))
                throw new ArgumentNullException (nameof (key));
            var types = DictionaryTypes (dictionary);
            var method = typeof (IDictionary<,>).MakeGenericType (types).GetMethod ("ContainsKey");
            return new Expression (LookUp (
                dictionary, key, types [0], false, "The key given to ContainsKey",
                (x, y) => LinqExpression.Call (x, method, y)));
        }

        /// <summary>
        /// The result of applying an accumulator function over a collection.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the collection is empty.
        /// </remarks>
        /// <returns>The accumulated value.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="func">The accumulator function.</param>
        [KRPCMethod]
        public static Expression Aggregate (Expression arg, Expression func)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Aggregate");
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (func, sourceType, sourceType, sourceType);
            var aggregate = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Aggregate" && x.GetParameters ().Length == 2);
            aggregate = aggregate.MakeGenericMethod (sourceType);
            // The accumulated value is the first element, then each value the function returns
            var resultSpec = TypeSpec.Join (
                TypeSpec.Create (sourceType, ElementSpec (arg)), ResultSpec (func, sourceType));
            var call = LinqExpression.Call (
                aggregate, arg,
                AdaptFunction (func, "A value given to the function", resultSpec, ElementSpec (arg)));
            return new Expression (call, resultSpec);
        }

        /// <summary>
        /// The result of applying an accumulator function over a collection, starting from a seed.
        /// </summary>
        /// <remarks>
        /// The seed widens to the type of the accumulated value, the function's result.
        /// </remarks>
        /// <returns>The accumulated value.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="seed">The seed value.</param>
        /// <param name="func">The accumulator function.</param>
        [KRPCMethod]
        public static Expression AggregateWithSeed (Expression arg, Expression seed, Expression func)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to AggregateWithSeed");
            if (ReferenceEquals (seed, null))
                throw new ArgumentNullException (nameof (seed));
            CheckHasValue (seed, "The seed given to AggregateWithSeed");
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            // The seed widens to the type of the accumulated value
            var accumulateType = GetFunctionResultType (func, nameof (func), 2);
            CheckIsFunction (func, accumulateType, sourceType, accumulateType);
            var accumulateSpec = func.parameterSpecs == null
                ? TypeSpec.Create (accumulateType) : func.parameterSpecs [0];
            var seedValue = ConvertTo (seed, accumulateSpec, "The seed given to AggregateWithSeed");
            var aggregate = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Aggregate" && x.GetParameters ().Length == 3);
            aggregate = aggregate.MakeGenericMethod (sourceType, accumulateType);
            var resultSpec = TypeSpec.Join (SpecAs (seed, accumulateType), ResultSpec (func, accumulateType));
            var call = LinqExpression.Call (
                aggregate, arg, seedValue,
                AdaptFunction (func, "A value given to the function", resultSpec, ElementSpec (arg)));
            return new Expression (call, resultSpec);
        }

        /// <summary>
        /// The values of one collection followed by those of another.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The first collection followed by the second.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Concat (Expression arg1, Expression arg2)
        {
            arg1 = NotNullOperand (arg1, nameof (arg1), "The first collection given to Concat");
            arg2 = NotNullOperand (arg2, nameof (arg2), "The second collection given to Concat");
            var valueType = JoinedValueType (arg1, arg2, nameof (Concat));
            var concat = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Concat");
            concat = concat.MakeGenericMethod (valueType);
            return SequenceOf (
                LinqExpression.Call (concat, WidenedValues (arg1, valueType), WidenedValues (arg2, valueType)),
                JoinedElementSpec (arg1, arg2, valueType));
        }

        /// <summary>
        /// The values of a collection ordered by the key a function produces for each of them.
        /// </summary>
        /// <remarks>
        /// The result is lazily evaluated. <see cref="ToList"/> makes it concrete.
        /// </remarks>
        /// <returns>The ordered collection.</returns>
        /// <param name="arg">The collection to order.</param>
        /// <param name="key">A function producing the key to order values by.</param>
        [KRPCMethod]
        public static Expression OrderBy (Expression arg, Expression key)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to OrderBy");
            var sourceType = GetEnumerableValueType (arg);
            var keyType = GetFunctionResultType (key, nameof (key), 1);
            CheckIsFunction (key, sourceType, keyType);
            CheckIsOrdered (keyType);
            var orderBy = typeof (Enumerable).GetMethods ().Single (x => x.Name == "OrderBy" && x.GetParameters ().Length == 3);
            orderBy = orderBy.MakeGenericMethod (sourceType, keyType);
            var comparer = typeof (Expression)
                .GetMethod (nameof (KeyComparer), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (keyType).Invoke (null, null);
            return SequenceOf (LinqExpression.Call (
                orderBy, arg, AdaptFunction (key, "A value given to the function", ElementSpec (arg)),
                LinqExpression.Constant (comparer, typeof (IComparer<>).MakeGenericType (keyType))),
                ElementSpec (arg));
        }

        /// <summary>
        /// Whether a predicate function returns true for every value of a collection.
        /// </summary>
        /// <returns>Whether all items satisfy the predicate.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="predicate">The predicate function.</param>
        [KRPCMethod]
        public static Expression All (Expression arg, Expression predicate)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to All");
            if (ReferenceEquals (predicate, null))
                throw new ArgumentNullException (nameof (predicate));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (predicate, sourceType, typeof (bool));
            var all = typeof (Enumerable).GetMethods ().Single (x => x.Name == "All");
            all = all.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (
                all, arg, AdaptFunction (predicate, "A value given to the function", ElementSpec (arg))));
        }

        /// <summary>
        /// Whether a predicate function returns true for any value of a collection.
        /// </summary>
        /// <returns>Whether any item satisfies the predicate.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="predicate">The predicate function.</param>
        [KRPCMethod]
        public static Expression Any (Expression arg, Expression predicate)
        {
            arg = NotNullOperand (arg, nameof (arg), "The collection given to Any");
            if (ReferenceEquals (predicate, null))
                throw new ArgumentNullException (nameof (predicate));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (predicate, sourceType, typeof (bool));
            var any = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Any" && x.GetParameters ().Length == 2);
            any = any.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (
                any, arg, AdaptFunction (predicate, "A value given to the function", ElementSpec (arg))));
        }

        /// <summary>
        /// An operand, checked when evaluated if its spec allows a null.
        /// Position describes the operand, for the error.
        /// </summary>
        static Expression NotNullOperand (Expression operand, string name, string position)
        {
            if (ReferenceEquals (operand, null))
                throw new ArgumentNullException (name);
            var spec = operand.Spec;
            if (spec == null || !spec.Nullable || operand.Type.IsValueType)
                return operand;
            var target = spec.WithNullable (false);
            return new Expression (CheckNulls (operand, spec, target, position), target);
        }

        static void CheckIsEnumerable (Expression collection)
        {
            if (ReferenceEquals (collection, null))
                throw new ArgumentNullException (nameof (collection));
            CheckIsNotAString (collection);
            CheckIsNotBytes (collection);
            if (!typeof (IEnumerable).IsAssignableFrom (collection.Type) ||
                !collection.Type.IsGenericType)
                throw new InvalidOperationException ("Expected an enumerable collection type");
        }

        // A string is enumerable, and is not a collection
        static void CheckIsNotAString (Expression expression)
        {
            if (expression.Type == typeof (string))
                throw new InvalidOperationException (
                    "A string is not a collection. Use the string operations instead.");
        }

        // A bytes value is enumerable, and is a single value
        static void CheckIsNotBytes (Expression expression)
        {
            if (expression.Type == typeof (byte[]))
                throw new InvalidOperationException ("A bytes value is not a collection.");
        }

        /// <summary>
        /// The Count property of a collection type. A service procedure returns an
        /// interface type, which declares nothing itself and inherits the property
        /// from ICollection, so the interfaces have to be searched as well.
        /// </summary>
        static PropertyInfo GetCountProperty (System.Type type)
        {
            return type.GetProperty ("Count") ??
                type.GetInterfaces ()
                    .Select (x => x.GetProperty ("Count"))
                    .FirstOrDefault (property => property != null);
        }

        static System.Type GetEnumerableValueType (Expression collection)
        {
            CheckIsEnumerable (collection);
            CheckIsNotADictionary (collection);
            return collection.Type.GetGenericArguments () [0];
        }

        // A dictionary enumerates as key-value pairs, which have no kRPC type
        static void CheckIsNotADictionary (Expression expression)
        {
            if (IsADictionary (expression))
                throw new InvalidOperationException (
                    "A dictionary cannot be used as a collection of values. Use " +
                    "DictionaryKeys or DictionaryValues to obtain a list of them.");
        }

        static void CheckIsADictionary (Expression expression)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            if (!IsADictionary (expression))
                throw new InvalidOperationException ("Expected a dictionary");
        }

        static bool IsADictionary (Expression expression)
        {
            return global::KRPC.Utils.Reflection.IsGenericType (
                expression.Type, typeof (IDictionary<,>));
        }

        /// <summary>
        /// The type of the value a function of one or two arguments produces. Throws an
        /// exception if the value is not a function of that many arguments.
        /// </summary>
        static System.Type GetFunctionResultType (Expression function, string name, int parameters)
        {
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (name);
            var types = function.Type.GetGenericArguments ();
            if (types.Length != parameters + 1)
                throw new InvalidOperationException (
                    "Expected a function taking " +
                    (parameters == 1 ? "one argument" : "two arguments"));
            return types [parameters];
        }

        static void CheckIsFunction (Expression function, System.Type parameterType, System.Type returnType)
        {
            if (!typeof (Func<,>)
                .MakeGenericType (parameterType, returnType)
                .IsAssignableFrom (function.Type))
                throw new InvalidOperationException (
                    "Expected a function taking one argument of type " + parameterType + ", " +
                    "with return type " + returnType);
        }

        static void CheckIsFunction (Expression function, System.Type parameterType1, System.Type parameterType2, System.Type returnType)
        {
            if (!typeof (Func<,,>)
                .MakeGenericType (parameterType1, parameterType2, returnType)
                .IsAssignableFrom (function.Type))
                throw new InvalidOperationException (
                    "Expected a function taking two arguments of type " + parameterType1 + " and " + parameterType2 + ", " +
                    "with return type " + returnType);
        }
    }
}
