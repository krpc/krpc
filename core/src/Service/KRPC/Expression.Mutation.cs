using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    public partial class Expression
    {
        /// <summary>
        /// A statement adding a value to a list or a set.
        /// </summary>
        /// <remarks>
        /// The value goes on the end of a list. A set holding the value is left unchanged.
        /// </remarks>
        /// <param name="collection">The list or set to add to.</param>
        /// <param name="value">The value to add.</param>
        [KRPCMethod]
        public static Expression Append (Expression collection, Expression value)
        {
            collection = NotNullOperand (collection, nameof (collection), "The collection given to Append");
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (IsADictionary (collection))
                throw new InvalidOperationException (
                    "An entry is added to a dictionary with Set, which takes its key.");
            var valueType = GetEnumerableValueType (collection);
            if (IsASet (collection))
                return new Expression (LinqExpression.Call (
                    CollectionHelper (nameof (SetAddHelper), valueType), collection,
                    ConvertTo (value, ElementSpec (collection), SetValuePosition)));
            CheckIsAList (collection);
            var add = typeof (ICollection<>).MakeGenericType (valueType).GetMethod ("Add");
            return new Expression (LinqExpression.Call (
                collection, add, ConvertTo (value, ElementSpec (collection), "A value of a list")));
        }

        /// <summary>
        /// A statement setting an element of a list, or an entry of a dictionary.
        /// </summary>
        /// <remarks>
        /// A key the dictionary does not hold is added. Throws an exception if the index
        /// is out of range for the list.
        /// </remarks>
        /// <param name="collection">The list or dictionary.</param>
        /// <param name="index">The index of the element, counting from zero, or the key of the entry.</param>
        /// <param name="value">The value to set.</param>
        [KRPCMethod]
        public static Expression Set (Expression collection, Expression index, Expression value)
        {
            collection = NotNullOperand (collection, nameof (collection), "The collection given to Set");
            if (ReferenceEquals (index, null))
                throw new ArgumentNullException (nameof (index));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            CheckIsNotAString (collection);
            if (IsADictionary (collection)) {
                var types = DictionaryTypes (collection);
                var entry = typeof (IDictionary<,>).MakeGenericType (types).GetProperty ("Item");
                return new Expression (LinqExpression.Block (
                    typeof (void),
                    LinqExpression.Assign (
                        LinqExpression.Property (
                            collection, entry, ConvertTo (index, TypeSpec.Create (types [0]), DictionaryKeyPosition)),
                        ConvertTo (value, ElementSpec (collection), "A value of a dictionary"))));
            }
            if (!IsAList (collection))
                throw new InvalidOperationException ("Expected a list or a dictionary");
            var valueType = GetEnumerableValueType (collection);
            CheckIsAnInt (index, nameof (index));
            return new Expression (LinqExpression.Call (
                CollectionHelper (nameof (ListSetHelper), valueType), collection, index,
                ConvertTo (value, ElementSpec (collection), "A value of a list")));
        }

        /// <summary>
        /// A removal of a value from a list or a set, or of an entry from a dictionary.
        /// </summary>
        /// <remarks>
        /// The first occurrence is removed from a list.
        /// </remarks>
        /// <returns>Whether the collection held the value.</returns>
        /// <param name="collection">The list, set or dictionary.</param>
        /// <param name="value">The value to remove, or the key of the entry to
        /// remove.</param>
        [KRPCMethod]
        public static Expression Remove (Expression collection, Expression value)
        {
            collection = NotNullOperand (collection, nameof (collection), "The collection given to Remove");
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (IsADictionary (collection)) {
                var types = DictionaryTypes (collection);
                var removeEntry = typeof (IDictionary<,>).MakeGenericType (types).GetMethod ("Remove");
                return new Expression (LookUp (
                    collection, value, types [0], false, "The key given to Remove",
                    (x, y) => LinqExpression.Call (x, removeEntry, y)));
            }
            var valueType = GetEnumerableValueType (collection);
            CheckIsAListOrASet (collection);
            var helper = IsAList (collection) ? nameof (ListRemoveHelper) : nameof (SetRemoveHelper);
            return new Expression (LookUp (
                collection, value, valueType, ElementSpec (collection).Nullable, "The value given to Remove",
                (x, y) => LinqExpression.Call (CollectionHelper (helper, valueType), x, y)));
        }

        /// <summary>
        /// A statement removing the element at an index of a list.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the index is out of range.
        /// </remarks>
        /// <param name="list">The list.</param>
        /// <param name="index">The index of the element, counting from zero.</param>
        [KRPCMethod]
        public static Expression RemoveAt (Expression list, Expression index)
        {
            list = NotNullOperand (list, nameof (list), "The list given to RemoveAt");
            if (IsADictionary (list))
                throw new InvalidOperationException (
                    "An entry is removed from a dictionary with Remove, which takes its key.");
            var valueType = GetEnumerableValueType (list);
            CheckIsAList (list);
            CheckIsAnInt (index, nameof (index));
            return new Expression (LinqExpression.Call (
                CollectionHelper (nameof (ListRemoveAtHelper), valueType), list, index));
        }

        /// <summary>
        /// A statement removing every value from a list or a set, or every entry from a dictionary.
        /// </summary>
        /// <param name="collection">The list, set or dictionary.</param>
        [KRPCMethod]
        public static Expression Clear (Expression collection)
        {
            collection = NotNullOperand (collection, nameof (collection), "The collection given to Clear");
            if (IsADictionary (collection)) {
                var types = DictionaryTypes (collection);
                return new Expression (LinqExpression.Call (
                    collection,
                    typeof (ICollection<>)
                        .MakeGenericType (typeof (KeyValuePair<,>).MakeGenericType (types))
                        .GetMethod ("Clear")));
            }
            var valueType = GetEnumerableValueType (collection);
            CheckIsAListOrASet (collection);
            return new Expression (LinqExpression.Call (
                collection, typeof (ICollection<>).MakeGenericType (valueType).GetMethod ("Clear")));
        }

        internal static void ListSetHelper<T> (IList<T> list, int index, T value)
        {
            CheckIndex (index, list.Count);
            list [index] = value;
        }

        internal static void ListRemoveAtHelper<T> (IList<T> list, int index)
        {
            CheckIndex (index, list.Count);
            list.RemoveAt (index);
        }

        // A set a procedure returns compares its values by the default equality, so these
        // helpers compare values with ValuesComparer

        internal static void SetAddHelper<T> (ISet<T> set, T value)
        {
            if (!ContainsHelper (set, value))
                set.Add (value);
        }

        internal static bool ListRemoveHelper<T> (IList<T> list, T value)
        {
            var comparer = ValuesComparer<T> ();
            for (int i = 0; i < list.Count; i++) {
                if (comparer.Equals (list [i], value)) {
                    list.RemoveAt (i);
                    return true;
                }
            }
            return false;
        }

        internal static bool SetRemoveHelper<T> (ISet<T> set, T value)
        {
            var comparer = ValuesComparer<T> ();
            var hashSet = set as HashSet<T>;
            if (hashSet != null && hashSet.Comparer == comparer)
                return set.Remove (value);
            var equal = set.Where (x => comparer.Equals (x, value)).ToList ();
            foreach (var x in equal)
                set.Remove (x);
            return equal.Count > 0;
        }

        // Which kind of collection an expression produces. A list and a set are both
        // ICollection, so the operations for only one of them check which it is
        static bool IsAList (Expression expression)
        {
            return global::KRPC.Utils.Reflection.IsGenericType (
                expression.Type, typeof (IList<>));
        }

        static bool IsASet (Expression expression)
        {
            return global::KRPC.Utils.Reflection.IsGenericType (
                expression.Type, typeof (ISet<>));
        }

        static void CheckIsAList (Expression expression)
        {
            if (!IsAList (expression))
                throw new InvalidOperationException ("Expected a list");
        }

        static void CheckIsAListOrASet (Expression expression)
        {
            if (!IsAList (expression) && !IsASet (expression))
                throw new InvalidOperationException ("Expected a list or a set");
        }
    }
}
