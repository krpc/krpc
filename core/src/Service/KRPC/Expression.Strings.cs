using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    public partial class Expression
    {
        internal static string ConvertToStringHelper (object value)
        {
            return Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The string representation of a value.
        /// </summary>
        /// <remarks>
        /// A value other than a number, boolean, string or enumeration converts as .NET
        /// formats it, which for a collection is the name of its type. Throws an exception if
        /// the value is null.
        /// </remarks>
        /// <returns>The string representation of the value.</returns>
        /// <param name="arg">The value to convert.</param>
        [KRPCMethod]
        public static Expression ConvertToString (Expression arg)
        {
            const string position = "The argument of ConvertToString";
            var operand = NotNullOperand (arg, nameof (arg), position);
            CheckHasValue (operand, position);
            var value = UnwrapNullable (operand, position);
            var method = typeof (Expression).GetMethod (
                nameof (ConvertToStringHelper), BindingFlags.Static | BindingFlags.NonPublic);
            return new Expression (LinqExpression.Call (
                method, LinqExpression.Convert (value, typeof (object))));
        }

        /// <summary>
        /// The concatenation of strings.
        /// </summary>
        /// <remarks>
        /// Use <see cref="ConvertToString"/> to convert other values to strings.
        /// </remarks>
        /// <returns>The concatenated string.</returns>
        /// <param name="args">The strings to concatenate, in order.</param>
        [KRPCMethod]
        public static Expression StringConcat (IList<Expression> args)
        {
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
            CheckNoNullElements (args, "values to concatenate");
            var concat = typeof (string).GetMethod ("Concat", new [] { typeof (string []) });
            return new Expression (LinqExpression.Call (
                concat,
                LinqExpression.NewArrayInit (
                    typeof (string),
                    args.Select (x => StringOperand (x, nameof (args), "A value given to StringConcat")))));
        }

        /// <summary>
        /// The number of characters in a string.
        /// </summary>
        /// <returns>The length of the string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringLength (Expression arg)
        {
            var argString = StringOperand (arg, nameof (arg), "The argument of StringLength");
            return new Expression (LinqExpression.Property (
                argString, typeof (string).GetProperty ("Length")));
        }

        /// <summary>
        /// The character at the given position in a string, as a string of length one.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the index is out of range.
        /// </remarks>
        /// <returns>The character at the given position.</returns>
        /// <param name="arg">The string.</param>
        /// <param name="index">The position of the character, counting from zero.</param>
        [KRPCMethod]
        public static Expression StringGet (Expression arg, Expression index)
        {
            var argString = StringOperand (arg, nameof (arg), "The first argument of StringGet");
            CheckIsAnInt (index, nameof (index));
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (StringGetHelper), BindingFlags.Static | BindingFlags.NonPublic),
                argString, index));
        }

        internal static string StringGetHelper (string value, int index)
        {
            CheckIndex (index, value.Length);
            return value.Substring (index, 1);
        }

        /// <summary>
        /// Part of a string.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the start or the length is out of range.
        /// </remarks>
        /// <returns>The substring.</returns>
        /// <param name="arg">The string.</param>
        /// <param name="start">The position to start at, counting from zero.</param>
        /// <param name="length">The number of characters to take.</param>
        [KRPCMethod]
        public static Expression StringSubstring (Expression arg, Expression start, Expression length)
        {
            var argString = StringOperand (arg, nameof (arg), "The first argument of StringSubstring");
            CheckIsAnInt (start, nameof (start));
            CheckIsAnInt (length, nameof (length));
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (StringSubstringHelper), BindingFlags.Static | BindingFlags.NonPublic),
                argString, start, length));
        }

        internal static string StringSubstringHelper (string value, int start, int length)
        {
            if (start < 0 || length < 0 || start > value.Length - length)
                throw new IndexOutOfRangeException (
                    "The substring of length " + length + " at index " + start +
                    " is out of range for a string of " + value.Length + " characters");
            return value.Substring (start, length);
        }

        /// <summary>
        /// The position of the first occurrence of one string within another.
        /// </summary>
        /// <returns>The position of the value, counting from zero, or -1 if it does not occur.</returns>
        /// <param name="arg">The string to search.</param>
        /// <param name="value">The string to search for.</param>
        [KRPCMethod]
        public static Expression StringIndexOf (Expression arg, Expression value)
        {
            return new Expression (OrdinalStringCall ("IndexOf", "StringIndexOf", arg, value));
        }

        /// <summary>
        /// Whether one string occurs within another.
        /// </summary>
        /// <returns>Whether the value occurs in the string.</returns>
        /// <param name="arg">The string to search.</param>
        /// <param name="value">The string to search for.</param>
        [KRPCMethod]
        public static Expression StringContains (Expression arg, Expression value)
        {
            return new Expression (LinqExpression.GreaterThanOrEqual (
                OrdinalStringCall ("IndexOf", "StringContains", arg, value), LinqExpression.Constant (0)));
        }

        /// <summary>
        /// Whether a string starts with another.
        /// </summary>
        /// <returns>Whether the string starts with the value.</returns>
        /// <param name="arg">The string to test.</param>
        /// <param name="value">The string to test for.</param>
        [KRPCMethod]
        public static Expression StringStartsWith (Expression arg, Expression value)
        {
            return new Expression (OrdinalStringCall ("StartsWith", "StringStartsWith", arg, value));
        }

        /// <summary>
        /// Whether a string ends with another.
        /// </summary>
        /// <returns>Whether the string ends with the value.</returns>
        /// <param name="arg">The string to test.</param>
        /// <param name="value">The string to test for.</param>
        [KRPCMethod]
        public static Expression StringEndsWith (Expression arg, Expression value)
        {
            return new Expression (OrdinalStringCall ("EndsWith", "StringEndsWith", arg, value));
        }

        /// <summary>
        /// A string in upper case, independently of the game's language.
        /// </summary>
        /// <returns>The string in upper case.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringToUpper (Expression arg)
        {
            var argString = StringOperand (arg, nameof (arg), "The argument of StringToUpper");
            return new Expression (LinqExpression.Call (
                argString, typeof (string).GetMethod ("ToUpperInvariant", System.Type.EmptyTypes)));
        }

        /// <summary>
        /// A string in lower case, independently of the game's language.
        /// </summary>
        /// <returns>The string in lower case.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringToLower (Expression arg)
        {
            var argString = StringOperand (arg, nameof (arg), "The argument of StringToLower");
            return new Expression (LinqExpression.Call (
                argString, typeof (string).GetMethod ("ToLowerInvariant", System.Type.EmptyTypes)));
        }

        /// <summary>
        /// A string with white space removed from both ends.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringTrim (Expression arg)
        {
            return new Expression (TrimCall ("Trim", arg));
        }

        /// <summary>
        /// A string with white space removed from its start.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringTrimStart (Expression arg)
        {
            return new Expression (TrimCall ("TrimStart", arg));
        }

        /// <summary>
        /// A string with white space removed from its end.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringTrimEnd (Expression arg)
        {
            return new Expression (TrimCall ("TrimEnd", arg));
        }

        /// <summary>
        /// A string with every occurrence of one string replaced by another.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the string to replace is empty.
        /// </remarks>
        /// <returns>The string with the replacements made.</returns>
        /// <param name="arg">The string to search.</param>
        /// <param name="oldValue">The string to replace.</param>
        /// <param name="newValue">The string to replace it with.</param>
        [KRPCMethod]
        public static Expression StringReplace (Expression arg, Expression oldValue, Expression newValue)
        {
            var argString = StringOperand (arg, nameof (arg), "The first argument of StringReplace");
            var oldString = StringOperand (oldValue, nameof (oldValue), "The second argument of StringReplace");
            var newString = StringOperand (newValue, nameof (newValue), "The third argument of StringReplace");
            return new Expression (LinqExpression.Call (
                argString, typeof (string).GetMethod ("Replace", new [] { typeof (string), typeof (string) }),
                oldString, newString));
        }

        internal static IList<string> StringSplitHelper (string value, string separator)
        {
            // A list, so the parts can be appended to
            return new List<string> (value.Split (new [] { separator }, StringSplitOptions.None));
        }

        /// <summary>
        /// The parts of a string separated by another string.
        /// </summary>
        /// <returns>The parts of the string.</returns>
        /// <param name="arg">The string to split.</param>
        /// <param name="separator">The string that separates the parts.</param>
        [KRPCMethod]
        public static Expression StringSplit (Expression arg, Expression separator)
        {
            var argString = StringOperand (arg, nameof (arg), "The first argument of StringSplit");
            var separatorString = StringOperand (separator, nameof (separator), "The second argument of StringSplit");
            var method = typeof (Expression).GetMethod (
                nameof (StringSplitHelper), BindingFlags.Static | BindingFlags.NonPublic);
            return new Expression (LinqExpression.Call (method, argString, separatorString));
        }

        /// <summary>
        /// Strings joined together, separated by another string.
        /// </summary>
        /// <returns>The joined string.</returns>
        /// <param name="separator">The string to put between the values.</param>
        /// <param name="values">The strings to join.</param>
        [KRPCMethod]
        public static Expression StringJoin (Expression separator, Expression values)
        {
            const string position = "The second argument of StringJoin";
            var separatorString = StringOperand (separator, nameof (separator), "The first argument of StringJoin");
            if (ReferenceEquals (values, null))
                throw new ArgumentNullException (nameof (values));
            if (!typeof (IEnumerable<string>).IsAssignableFrom (values.Type))
                throw new InvalidOperationException (position + " is not a collection of strings");
            var join = typeof (string).GetMethod (
                "Join", new [] { typeof (string), typeof (IEnumerable<string>) });
            var strings = CheckNulls (
                values, values.Spec, SpecWith (values.Type, TypeSpec.Create (typeof (string))), position);
            return new Expression (LinqExpression.Call (join, separatorString, strings));
        }

        /// <summary>
        /// A string operand, checked when evaluated to not be null. Position describes
        /// the operand, for the error.
        /// </summary>
        static LinqExpression StringOperand (Expression expression, string name, string position)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (name);
            if (expression.Type != typeof (string))
                throw new InvalidOperationException (
                    position + " is not a string; use ConvertToString to convert a value to one");
            return CheckNulls (expression, expression.Spec, TypeSpec.Create (typeof (string)), position);
        }

        /// <summary>
        /// A call of a string method comparing with another string, done by ordinal
        /// value so that the result does not depend on the game's language.
        /// </summary>
        static LinqExpression OrdinalStringCall (string name, string rpc, Expression arg, Expression value)
        {
            var argString = StringOperand (arg, nameof (arg), "The first argument of " + rpc);
            var valueString = StringOperand (value, nameof (value), "The second argument of " + rpc);
            var method = typeof (string).GetMethod (
                name, new [] { typeof (string), typeof (StringComparison) });
            return LinqExpression.Call (
                argString, method, valueString, LinqExpression.Constant (StringComparison.Ordinal));
        }

        /// <summary>
        /// A call of one of the trim methods. Each takes the characters to trim, and
        /// trims white space when given none.
        /// </summary>
        static LinqExpression TrimCall (string name, Expression arg)
        {
            var argString = StringOperand (arg, nameof (arg), "The argument of String" + name);
            var method = typeof (string).GetMethod (name, new [] { typeof (char []) });
            return LinqExpression.Call (
                argString, method, LinqExpression.Constant (new char [0]));
        }
    }
}
