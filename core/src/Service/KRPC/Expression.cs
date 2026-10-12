using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A server side expression.
    /// </summary>
    /// <remarks>
    /// Numeric operands of differing types are converted to a common type.
    /// </remarks>
    [KRPCClass (Service = "KRPC")]
    public partial class Expression
    {
        readonly LinqExpression internalExpression;
        TypeSpec spec;
        // The specs of a function's parameters and of its result, for a function created
        // with Lambda
        TypeSpec[] parameterSpecs;
        TypeSpec resultSpec;

        Func<object[], object> compiled;
        bool isChecked;

        internal Expression(LinqExpression expression)
        {
            internalExpression = expression;
        }

        internal Expression (LinqExpression expression, TypeSpec valueSpec)
        {
            internalExpression = expression;
            spec = valueSpec;
        }

        /// <summary>
        /// Convert a kRPC expression to a System.Linq.Expressions.Expression.
        /// </summary>
        public static implicit operator LinqExpression (Expression expression)
        {
            if (ReferenceEquals (expression, null))
                return null;
            return expression.internalExpression;
        }

        /// <summary>
        /// Convert a System.Linq.Expressions.Expression to a kRPC expression.
        /// </summary>
        public static implicit operator Expression (LinqExpression expression)
        {
            if (ReferenceEquals (expression, null))
                return null;
            return new Expression(expression);
        }

        internal System.Type Type {
            get { return internalExpression.Type; }
        }

        /// <summary>
        /// The type of the value, with the positions inside it that can hold null. A node
        /// that computes a value holds no null, and one that passes a value on is given the
        /// spec of where the value came from. Null for a node with no value.
        /// </summary>
        internal TypeSpec Spec {
            get {
                if (spec == null && internalExpression.Type != typeof (void))
                    spec = TypeSpec.Create (internalExpression.Type);
                return spec;
            }
        }

        /// <summary>
        /// The spec of a value converted to the given type, nullable where the value is and
        /// the type can hold a null.
        /// </summary>
        static TypeSpec SpecAs (Expression value, System.Type type)
        {
            return value.Spec == null ? TypeSpec.Create (type) : TypeSpec.Create (type, value.Spec);
        }

        /// <summary>
        /// A spec for the given type, with the given specs for the values it contains. A
        /// null leaves that position as the type declares it.
        /// </summary>
        static TypeSpec SpecWith (System.Type type, params TypeSpec[] containedTypes)
        {
            var plain = TypeSpec.Create (type);
            if (plain.Types.Count != containedTypes.Length)
                return plain;
            var types = new TypeSpec [containedTypes.Length];
            for (int i = 0; i < types.Length; i++) {
                var position = plain.Types [i].DeclaredType;
                types [i] = containedTypes [i] == null
                    ? plain.Types [i] : TypeSpec.Create (position, containedTypes [i]);
            }
            return TypeSpec.Create (type, plain.Nullable, types);
        }

        /// <summary>
        /// The spec of the values a list, set or sequence holds, or of the values of a
        /// dictionary.
        /// </summary>
        static TypeSpec ElementSpec (Expression collection)
        {
            var types = collection.Spec.Types;
            if (types.Count > 0)
                return types [types.Count - 1];
            var arguments = collection.Type.IsGenericType ? collection.Type.GetGenericArguments () : null;
            return arguments == null ? null : TypeSpec.Create (arguments [arguments.Length - 1]);
        }

        /// <summary>
        /// Check a value at a position that cannot hold null, where the value can. A
        /// nullable value type has already been converted to the value it holds, which
        /// throws for a null. Position describes where the value is going, for the error.
        /// </summary>
        static LinqExpression CheckNulls (
            LinqExpression value, TypeSpec valueSpec, TypeSpec target, string position,
            bool topLevel = true)
        {
            if (valueSpec == null || target == null)
                return value;
            var atTop = topLevel && valueSpec.Nullable && !target.Nullable && !value.Type.IsValueType;
            var nested = !ContainedNoMoreNullable (valueSpec, target);
            if (nested && target.Kind == TypeKind.Unknown && TypeSpec.SequenceElementType (value.Type) != null)
                return SequenceWithNoNulls (value, target, position);
            if (nested)
                return LinqExpression.Call (
                    typeof (Expression).GetMethod (nameof (NoNulls), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod (value.Type),
                    value, LinqExpression.Constant (topLevel ? target : target.WithNullable (true)),
                    LinqExpression.Constant (position));
            if (atTop)
                return LinqExpression.Call (
                    typeof (Expression).GetMethod (nameof (NotNull), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod (value.Type),
                    value, LinqExpression.Constant (position));
            return value;
        }

        // A lazily evaluated sequence is checked as it is enumerated, as walking it first
        // would evaluate it twice
        static LinqExpression SequenceWithNoNulls (LinqExpression value, TypeSpec target, string position)
        {
            var element = TypeSpec.SequenceElementType (value.Type);
            var x = LinqExpression.Parameter (element, "x");
            var check = LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (NoNulls), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod (element),
                x, LinqExpression.Constant (target.Types [0]), LinqExpression.Constant (position));
            return LinqExpression.Call (SelectMethod (element, element), value, LinqExpression.Lambda (check, x));
        }

        // Enumerable.Select over a sequence, with a function of each value
        static MethodInfo SelectMethod (System.Type source, System.Type result)
        {
            return typeof (Enumerable).GetMethods ()
                .Single (x => x.Name == "Select" && x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2)
                .MakeGenericMethod (source, result);
        }

        static bool ContainedNoMoreNullable (TypeSpec valueSpec, TypeSpec target)
        {
            if (valueSpec.Types.Count != target.Types.Count)
                return true;
            for (int i = 0; i < valueSpec.Types.Count; i++) {
                if (!valueSpec.Types [i].IsNoMoreNullableThan (target.Types [i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Convert a value to the type of a position, as <see cref="ConvertElement"/> does,
        /// and check it holds no null the position cannot.
        /// </summary>
        static LinqExpression ConvertTo (Expression value, TypeSpec target, string position)
        {
            var converted = ConvertElement (value, target.DeclaredType, position);
            return CheckNulls (converted, SpecAs (value, converted.Type), target, position);
        }

        internal static T NotNull<T> (T value, string position) where T : class
        {
            if (value == null)
                throw new NullReferenceException (position + " is null, which its type does not allow");
            return value;
        }

        internal static T NoNulls<T> (T value, TypeSpec target, string position)
        {
            CheckNoNulls (value, target, position);
            return value;
        }

        static void CheckNoNulls (object value, TypeSpec target, string position)
        {
            if (value == null) {
                if (!target.Nullable)
                    throw new NullReferenceException (
                        position + " holds a null where its type does not allow one");
                return;
            }
            switch (target.Kind) {
            case TypeKind.List:
            case TypeKind.Set:
                foreach (var item in (IEnumerable)value)
                    CheckNoNulls (item, target.Types [0], position);
                break;
            case TypeKind.Dictionary:
                foreach (DictionaryEntry entry in (IDictionary)value)
                    CheckNoNulls (entry.Value, target.Types [1], position);
                break;
            case TypeKind.Tuple:
                for (int i = 0; i < target.Types.Count; i++)
                    CheckNoNulls (value.GetType ().GetProperty ("Item" + (i + 1)).GetValue (value, null),
                        target.Types [i], position);
                break;
            }
        }

        /// <summary>
        /// Reported when a procedure called by a function pauses execution to
        /// resume on a later tick.
        /// </summary>
        internal const string YieldedMessage =
            "A procedure called by the function paused execution, to resume on a " +
            "later tick. A function is evaluated within a single tick, so a " +
            "procedure that does this cannot be called from one.";

        /// <summary>
        /// Reported when a function evaluates to null where its result cannot be null.
        /// </summary>
        internal const string NullResultMessage =
            "The function evaluated to null, which its result does not allow";

        /// <summary>
        /// The parameters of the function. A function created with <see cref="Lambda"/>
        /// takes the lambda's parameters, and any other function takes none.
        /// </summary>
        internal IList<ParameterExpression> Parameters {
            get {
                var lambda = internalExpression as LambdaExpression;
                return lambda == null ? (IList<ParameterExpression>)NoParameters : lambda.Parameters;
            }
        }

        static readonly ParameterExpression[] NoParameters = new ParameterExpression [0];

        /// <summary>
        /// The spec of the function's parameter at the given position.
        /// </summary>
        TypeSpec ParameterSpec (int position)
        {
            return parameterSpecs == null || parameterSpecs [position] == null
                ? TypeSpec.Create (Parameters [position].Type) : parameterSpecs [position];
        }

        /// <summary>
        /// The type of the value the function evaluates to. A function created with
        /// <see cref="Lambda"/> evaluates to the value it returns when called.
        /// </summary>
        System.Type ResultType {
            get {
                var lambda = internalExpression as LambdaExpression;
                return lambda == null ? internalExpression.Type : lambda.ReturnType;
            }
        }

        /// <summary>
        /// The spec of the value the function evaluates to. Null for a function with no value.
        /// </summary>
        internal TypeSpec FunctionSpec {
            get {
                if (!(internalExpression is LambdaExpression))
                    return Spec;
                if (ResultType == typeof (void))
                    return null;
                return resultSpec ?? TypeSpec.Create (ResultType);
            }
        }

        /// <summary>
        /// Compile the expression, if it has not been compiled already.
        /// </summary>
        internal void Compile ()
        {
            if (compiled != null)
                return;
            // A function with parameters takes its arguments from an array
            var arguments = LinqExpression.Parameter (typeof (object[]), "arguments");
            LinqExpression value = internalExpression;
            var lambda = internalExpression as LambdaExpression;
            if (lambda != null)
                value = LinqExpression.Invoke (lambda, lambda.Parameters.Select (
                    (x, i) => LinqExpression.Convert (
                        LinqExpression.ArrayIndex (arguments, LinqExpression.Constant (i)), x.Type)));
            LinqExpression body;
            if (value.Type == typeof (void))
                body = LinqExpression.Block (value, LinqExpression.Constant (null));
            else
                body = LinqExpression.Convert (value, typeof (object));
            compiled = LinqExpression.Lambda<Func<object[], object>> (body, arguments).Compile ();
        }

        /// <summary>
        /// Evaluate the expression with the given arguments, giving null for an expression
        /// with no value. Throws an exception if a procedure it calls pauses execution, or if
        /// it evaluates to null where its result cannot be null.
        /// </summary>
        internal object Evaluate (object[] arguments = null)
        {
            Compile ();
            object value;
            try {
                value = compiled (arguments ?? NoArguments);
            } catch (YieldException) {
                throw new InvalidOperationException (YieldedMessage);
            }
            if (ReferenceEquals (value, null) && HasReturnType && !FunctionSpec.Nullable)
                throw new NullReferenceException (NullResultMessage);
            return value;
        }

        static readonly object[] NoArguments = new object [0];

        /// <summary>
        /// Decode the encoded arguments of a call to the function, one per parameter. Throws
        /// an exception if the number of arguments differs from the number of parameters, or
        /// an argument is null where its parameter cannot be.
        /// </summary>
        internal object[] DecodeArguments (IList<byte[]> arguments)
        {
            var parameters = Parameters;
            var count = arguments == null ? 0 : arguments.Count;
            if (count != parameters.Count)
                throw new ArgumentException (ArgumentCountMessage (parameters.Count, count));
            var values = new object [count];
            for (int i = 0; i < count; i++) {
                var spec = ParameterSpec (i);
                if (arguments [i] == null) {
                    if (!spec.Nullable)
                        throw new ArgumentException (
                            "The argument for parameter " + parameters [i].Name +
                            " is null, which its type does not allow");
                    continue;
                }
                // An unknown object or a short tuple fails to decode, and an object of the
                // wrong class decodes as itself
                object value = null;
                try {
                    value = Server.ProtocolBuffers.Encoder.Decode (
                        Google.Protobuf.ByteString.CopyFrom (arguments [i]), spec);
                } catch (ObjectDestroyedException) {
                    throw;
                } catch (System.Exception) {
                }
                if (ReferenceEquals (value, null) || !spec.Type.IsInstanceOfType (value))
                    throw new ArgumentException (
                        "The argument for parameter " + parameters [i].Name +
                        " is not a value of type " + spec.Type);
                values [i] = value;
            }
            return values;
        }

        /// <summary>
        /// Throws an exception if the function takes parameters, as a function that is
        /// streamed or checked for an event is evaluated with no arguments.
        /// </summary>
        internal void CheckNoParameters ()
        {
            if (Parameters.Count > 0)
                throw new ArgumentException (
                    "A function that is streamed, or checked for an event, cannot take parameters");
        }

        /// <summary>
        /// The type of the value the expression evaluates to. Throws an exception if the
        /// value cannot be sent to a client.
        /// </summary>
        /// <remarks>
        /// A function created with <see cref="Lambda"/> evaluates to the value it returns. A
        /// lazily evaluated collection, such as from <see cref="Select"/>, is sent once
        /// converted by <see cref="ToList"/> or <see cref="ToSet"/>.
        /// </remarks>
        [KRPCProperty]
        public Type ReturnType {
            get {
                GetValidReturnType ();
                return new Type (FunctionSpec);
            }
        }

        /// <summary>
        /// The types of the function's parameters.
        /// </summary>
        /// <remarks>
        /// Empty for a function that is not created with <see cref="Lambda"/>.
        /// </remarks>
        [KRPCProperty]
        public IList<Type> ParameterTypes {
            get {
                return Enumerable.Range (0, Parameters.Count)
                    .Select (i => new Type (ParameterSpec (i))).ToList ();
            }
        }

        /// <summary>
        /// Whether the expression evaluates to a value.
        /// </summary>
        /// <remarks>
        /// An expression with no value has no <see cref="ReturnType"/>. An empty
        /// collection and no value both encode to an empty sequence of bytes.
        /// </remarks>
        [KRPCProperty]
        public bool HasReturnType {
            get { return ResultType != typeof (void); }
        }

        /// <summary>
        /// The expression's type, checked to be a type that can be sent to a client.
        /// </summary>
        internal System.Type GetValidReturnType ()
        {
            var type = ResultType;
            if (type == typeof (void))
                throw new InvalidOperationException (
                    "The function does not evaluate to a value");
            // A protocol buffer message type, such as Services, has no type code to
            // describe it to a client
            if (TypeUtils.IsAMessageType (type))
                throw new InvalidOperationException (
                    "The function evaluates to a protocol buffer message of type " +
                    type.Name + ", which cannot be sent to a client.");
            if (!TypeUtils.IsAValidType (type))
                throw new InvalidOperationException (
                    "The function evaluates to a value of type " + type + ", " +
                    "which cannot be sent to a client. If the value is a lazily " +
                    "evaluated collection, use ToList or ToSet to convert it.");
            return type;
        }

        /// <summary>
        /// The most nodes a function can compile to. A node used in several places
        /// counts once for each, as the compiler expands it in each place. Compiling
        /// takes about a microsecond a node.
        /// </summary>
        const int MaxNodes = 1000000;

        /// <summary>
        /// A visitor that throws once it has visited more than <see cref="MaxNodes"/>
        /// nodes. Each step of a chain that uses the step before twice doubles the
        /// nodes, so a short chain can take hours to visit.
        /// </summary>
        abstract class BoundedVisitor : ExpressionVisitor
        {
            int visited;

            public override LinqExpression Visit (LinqExpression node)
            {
                if (node != null && ++visited > MaxNodes)
                    throw new InvalidOperationException (
                        "A function cannot have more than " + MaxNodes + " nodes. " +
                        "A node used in more than one place counts once for each place.");
                return base.Visit (node);
            }
        }

        /// <summary>
        /// Throws if the expression has more than <see cref="MaxNodes"/> nodes, uses a
        /// parameter or variable outside the function or block that declares it, or has a
        /// break, continue or return outside its loop or function or leaving a finally block.
        /// </summary>
        internal void Check ()
        {
            if (isChecked)
                return;
            new Checker ().Visit (internalExpression);
            isChecked = true;
        }

        sealed partial class Checker : BoundedVisitor
        {
            readonly HashSet<ParameterExpression> declared = new HashSet<ParameterExpression> ();

            void Declare (IEnumerable<ParameterExpression> parameters, Action visit)
            {
                var added = parameters.Where (declared.Add).ToList ();
                visit ();
                foreach (var parameter in added)
                    declared.Remove (parameter);
            }

            protected override LinqExpression VisitLambda<T> (Expression<T> node)
            {
                Declare (node.Parameters, () => Visit (node.Body));
                return node;
            }

            protected override LinqExpression VisitBlock (BlockExpression node)
            {
                Declare (node.Variables, () => Visit (node.Expressions));
                return node;
            }

            protected override LinqExpression VisitParameter (ParameterExpression node)
            {
                if (!declared.Contains (node))
                    throw new InvalidOperationException (
                        node.Name + " is used outside the function or block that declares it");
                return node;
            }
        }

        static readonly Dictionary<Tuple<System.Type, object>, Expression> constants =
            new Dictionary<Tuple<System.Type, object>, Expression> ();

        /// <summary>
        /// A constant expression of the given type and value, shared with every other
        /// constant of that type and value, so that the object store gives them all a
        /// single object identifier. The key includes the type so that constants of
        /// equal value but differing type stay distinct. Sharing is safe because the
        /// trees are immutable.
        /// </summary>
        static Expression Constant (System.Type type, object value)
        {
            var key = Tuple.Create (type, KeyOfConstant (value));
            Expression constant;
            if (!constants.TryGetValue (key, out constant)) {
                constant = new Expression (LinqExpression.Constant (value, type));
                constants [key] = constant;
            }
            return constant;
        }

        /// <summary>
        /// The part of a constant's key that stands for its value. A floating point
        /// value is keyed on its bits, because Equals makes -0.0 and 0.0 the same key
        /// while they are different constants.
        /// </summary>
        static object KeyOfConstant (object value)
        {
            if (value is double)
                return BitConverter.DoubleToInt64Bits ((double)value);
            if (value is float)
                return BitConverter.ToInt32 (BitConverter.GetBytes ((float)value), 0);
            // An array compares by reference, so bytes are keyed on their contents
            var bytes = value as byte[];
            if (bytes != null)
                return Convert.ToBase64String (bytes);
            return value;
        }

        /// <summary>
        /// Drop the shared constants. They are registered with the object store, which is
        /// emptied once no server is left for a client to hold an identifier through, so
        /// they live no longer than the identifiers naming them.
        /// </summary>
        internal static void ClearConstants ()
        {
            constants.Clear ();
            nullConstants.Clear ();
        }

        static bool IsNumericType (System.Type type)
        {
            return
                type == typeof (double) ||
                type == typeof (float) ||
                type == typeof (int) ||
                type == typeof (long) ||
                type == typeof (uint) ||
                type == typeof (ulong);
        }

        /// <summary>
        /// The common type both operands are implicitly convertible to, following C#'s
        /// binary numeric promotion rules.
        /// </summary>
        static System.Type CommonNumericType (System.Type type0, System.Type type1)
        {
            var common = FindCommonNumericType (type0, type1);
            if (common == null)
                throw new InvalidOperationException (
                    "No implicit conversion between " + type0 + " and " + type1 + ". " +
                    "Use a cast to convert one of the operands.");
            return common;
        }

        /// <summary>
        /// The common type both operands are implicitly convertible to, or null when
        /// there is none. An unsigned 64 bit integer has no common type with a signed
        /// integer.
        /// </summary>
        static System.Type FindCommonNumericType (System.Type type0, System.Type type1)
        {
            return FindCommonNumericType (new [] { type0, type1 });
        }

        /// <summary>
        /// The narrowest numeric type that every one of a set of numeric types widens to,
        /// or null when there is none. For two types it is C#'s binary numeric promotion.
        /// </summary>
        static System.Type FindCommonNumericType (ICollection<System.Type> types)
        {
            if (types.Contains (typeof (double)))
                return typeof (double);
            if (types.Contains (typeof (float)))
                return typeof (float);
            if (types.Contains (typeof (ulong)))
                return types.All (x => x == typeof (uint) || x == typeof (ulong)) ? typeof (ulong) : null;
            if (types.Contains (typeof (long)) || (types.Contains (typeof (uint)) && types.Contains (typeof (int))))
                return typeof (long);
            if (types.Contains (typeof (uint)))
                return typeof (uint);
            return typeof (int);
        }

        /// <summary>
        /// Convert the operands of a binary operation to a common numeric type,
        /// when they are numeric operands of differing types.
        /// </summary>
        static void PromoteOperands (ref LinqExpression arg0, ref LinqExpression arg1, string name)
        {
            arg0 = UnwrapNullable (arg0, "The first operand of " + name);
            arg1 = UnwrapNullable (arg1, "The second operand of " + name);
            PromoteNumbers (ref arg0, ref arg1);
        }

        static void PromoteNumbers (ref LinqExpression arg0, ref LinqExpression arg1)
        {
            var type0 = arg0.Type;
            var type1 = arg1.Type;
            if (type0 == type1 || !IsNumericType (type0) || !IsNumericType (type1))
                return;
            var common = CommonNumericType (type0, type1);
            if (type0 != common)
                arg0 = LinqExpression.Convert (arg0, common);
            if (type1 != common)
                arg1 = LinqExpression.Convert (arg1, common);
        }

        /// <summary>
        /// The type of the value a nullable value type holds, or null for any other type.
        /// </summary>
        static System.Type NullableValueType (System.Type type)
        {
            return Nullable.GetUnderlyingType (type);
        }

        /// <summary>
        /// Convert the operands to a common numeric type, keeping a null when either
        /// is a nullable value type. Used where a null operand has a meaning: equality
        /// and the branches of a conditional.
        /// </summary>
        static void PromoteNullableOperands (ref LinqExpression arg0, ref LinqExpression arg1)
        {
            if (arg0 == null || arg1 == null)
                return;
            var underlying0 = NullableValueType (arg0.Type);
            var underlying1 = NullableValueType (arg1.Type);
            if (underlying0 == null && underlying1 == null) {
                PromoteNumbers (ref arg0, ref arg1);
                return;
            }
            var type0 = underlying0 ?? arg0.Type;
            var type1 = underlying1 ?? arg1.Type;
            System.Type common;
            if (type0 == type1)
                common = type0;
            else if (IsNumericType (type0) && IsNumericType (type1))
                common = CommonNumericType (type0, type1);
            else
                return;
            var nullable = typeof (Nullable<>).MakeGenericType (common);
            if (arg0.Type != nullable)
                arg0 = LinqExpression.Convert (arg0, nullable);
            if (arg1.Type != nullable)
                arg1 = LinqExpression.Convert (arg1, nullable);
        }

        /// <summary>
        /// The value of a nullable value type, which throws when evaluated if the value is
        /// null. A procedure that can return null gives a nullable value, and operators and
        /// conversions take the value it holds. Position describes the value, for the error.
        /// </summary>
        static LinqExpression UnwrapNullable (LinqExpression expression, string position)
        {
            if (expression == null)
                return null;
            var underlying = NullableValueType (expression.Type);
            if (underlying == null)
                return expression;
            return LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (NotNullValue), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod (underlying),
                expression, LinqExpression.Constant (position));
        }

        internal static T NotNullValue<T> (T? value, string position) where T : struct
        {
            if (!value.HasValue)
                throw new NullReferenceException (position + " is null, which its type does not allow");
            return value.Value;
        }

        static Expression NumericBinaryOp (
            Func<LinqExpression, LinqExpression, LinqExpression> op, Expression arg0, Expression arg1, string name)
        {
            CheckOperands (arg0, arg1);
            LinqExpression left = arg0;
            LinqExpression right = arg1;
            PromoteOperands (ref left, ref right, name);
            return new Expression (op (left, right));
        }

        static Expression NullableBinaryOp (Func<LinqExpression, LinqExpression, LinqExpression> op, Expression arg0, Expression arg1)
        {
            CheckOperands (arg0, arg1);
            LinqExpression left = arg0;
            LinqExpression right = arg1;
            PromoteNullableOperands (ref left, ref right);
            return new Expression (op (left, right));
        }

        static void CheckOperands (Expression arg0, Expression arg1)
        {
            if (ReferenceEquals (arg0, null))
                throw new ArgumentNullException (nameof (arg0));
            if (ReferenceEquals (arg1, null))
                throw new ArgumentNullException (nameof (arg1));
        }

        /// <summary>
        /// A constant value of double precision floating point type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantDouble(double value)
        {
            return Constant (typeof (double), value);
        }

        /// <summary>
        /// A constant value of single precision floating point type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantFloat(float value)
        {
            return Constant (typeof (float), value);
        }

        /// <summary>
        /// A constant value of integer type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantInt(int value)
        {
            return Constant (typeof (int), value);
        }

        /// <summary>
        /// A constant value of long integer type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantLong (long value)
        {
            return Constant (typeof (long), value);
        }

        /// <summary>
        /// A constant value of unsigned integer type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantUInt (uint value)
        {
            return Constant (typeof (uint), value);
        }

        /// <summary>
        /// A constant value of unsigned long integer type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantULong (ulong value)
        {
            return Constant (typeof (ulong), value);
        }

        /// <summary>
        /// A constant value of boolean type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantBool (bool value)
        {
            return Constant (typeof (bool), value);
        }

        /// <summary>
        /// A constant value of string type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantString (string value)
        {
            return Constant (typeof (string), value);
        }

        /// <summary>
        /// A constant value of bytes type.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression ConstantBytes (byte[] value)
        {
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            return Constant (typeof (byte[]), value);
        }

        /// <summary>
        /// A constant value of an enumeration a service defines.
        /// </summary>
        /// <param name="service">The name of the service the enumeration is defined in.</param>
        /// <param name="name">The name of the enumeration.</param>
        /// <param name="value">The value of the member of the enumeration.</param>
        [KRPCMethod]
        public static Expression ConstantEnum (string service, string name, int value)
        {
            Scanner.ServiceSignature signature;
            if (!Services.Instance.Signatures.TryGetValue (service, out signature))
                throw new ArgumentException ("Service \"" + service + "\" not found");
            Scanner.EnumerationSignature enumeration;
            if (!signature.Enumerations.TryGetValue (name, out enumeration))
                throw new ArgumentException (
                    "Enumeration \"" + name + "\" not found in service \"" + service + "\"");
            var type = enumeration.UnderlyingType;
            if (!System.Enum.IsDefined (type, value))
                throw new ArgumentException (
                    value + " is not a value of enumeration \"" + name + "\" " +
                    "in service \"" + service + "\"");
            return Constant (type, System.Enum.ToObject (type, value));
        }

        // Keyed on the spec, as specs that differ in nested nullability share a CLR type
        static readonly Dictionary<TypeSpec, Expression> nullConstants =
            new Dictionary<TypeSpec, Expression> ();

        /// <summary>
        /// A null constant of the given type.
        /// </summary>
        /// <remarks>
        /// The type is made nullable, as by <see cref="Type.NullableType"/>.
        /// </remarks>
        /// <param name="type">The type of the value.</param>
        [KRPCMethod]
        public static Expression ConstantNull (Type type)
        {
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            var spec = type.Spec.WithNullable (true);
            Expression constant;
            if (!nullConstants.TryGetValue (spec, out constant)) {
                constant = new Expression (LinqExpression.Constant (null, spec.DeclaredType), spec);
                nullConstants [spec] = constant;
            }
            return constant;
        }

        /// <summary>
        /// A constant object, an instance of a class a service defines.
        /// </summary>
        /// <remarks>
        /// The object is given by its object identifier.
        /// </remarks>
        /// <param name="value">The object identifier of the object.</param>
        [KRPCMethod]
        public static Expression ConstantObject (ulong value)
        {
            var instance = ObjectStore.Instance.GetInstance (value);
            var type = GetClassType (instance);
            // A function built from within a function would bypass the checks made on it
            if (TypeUtils.GetClassServiceName (type) == "KRPC")
                throw new ArgumentException (
                    "An object of the KRPC service, such as an Expression, cannot be a " +
                    "constant in a function");
            return new Expression (LinqExpression.Constant (instance, type));
        }

        /// <summary>
        /// The service-defined class type of an object, i.e. the closest type in its
        /// hierarchy annotated as a kRPC class.
        /// </summary>
        static System.Type GetClassType (object instance)
        {
            var type = instance.GetType ();
            while (type != null && !TypeUtils.IsAClassType (type))
                type = type.BaseType;
            if (type == null)
                throw new ArgumentException (
                    instance.GetType () + " is not an instance of a class defined by a service");
            return type;
        }

        /// <summary>
        /// Whether a value is null. Throws an exception if the type of the value cannot be null.
        /// </summary>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public static Expression IsNull (Expression value)
        {
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var type = value.Type;
            if (type == typeof (void) || !value.Spec.Nullable)
                throw new InvalidOperationException ("A value of type " + type + " cannot be null");
            var nullConstant = LinqExpression.Constant (null, type);
            if (type.IsValueType)
                return new Expression (LinqExpression.Equal (value, nullConstant));
            return new Expression (LinqExpression.ReferenceEqual (value, nullConstant));
        }

        /// <summary>
        /// An RPC call.
        /// </summary>
        /// <remarks>
        /// The instance the call is made on, and the values of its arguments, are fixed
        /// when the expression is created. A call to a procedure with no return value is
        /// evaluated for its effects, as a statement in a <see cref="Block"/>. A collection the
        /// procedure returns is copied, as a client would receive it.
        /// </remarks>
        /// <param name="call">The procedure call.</param>
        [KRPCMethod]
        public static Expression Call(ProcedureCall call)
        {
            return BuildCall (call, null);
        }

        /// <summary>
        /// An RPC call, where some or all of the arguments are computed by expressions.
        /// </summary>
        /// <remarks>
        /// Position 0 is the instance of a class method or property. A position with no
        /// expression takes the argument encoded in the call, or the parameter's default. A
        /// collection the procedure returns is copied, as a client would receive it.
        /// </remarks>
        /// <param name="call">The RPC to call.</param>
        /// <param name="args">Expressions computing the call's arguments, by position.</param>
        [KRPCMethod]
        public static Expression CallWithArguments (ProcedureCall call, IDictionary<int, Expression> args)
        {
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
            return BuildCall (call, args);
        }

        static Expression BuildCall (ProcedureCall call, IDictionary<int, Expression> args)
        {
            if (ReferenceEquals (call, null))
                throw new ArgumentNullException (nameof (call));
            var services = Services.Instance;
            var procedure = services.GetProcedureSignature(call);
            // The KRPC service builds and runs functions, and manages the client's streams
            if (services.GetServiceSignature (call).Name == "KRPC")
                throw new InvalidOperationException (
                    "A function cannot call " + procedure.FullyQualifiedName +
                    ", as procedures of the KRPC service cannot be called from a function");
            // Creating an event or a stream adds a stream to the client, and a function
            // can be evaluated while the server iterates over the client's streams
            if (procedure.HasReturnType && TypeUtils.IsAMessageType (procedure.ReturnSpec.Type))
                throw new InvalidOperationException (
                    "A function cannot call " + procedure.FullyQualifiedName +
                    ", as it returns a protocol buffer message of type " +
                    procedure.ReturnSpec.Type.Name);

            var parameters = procedure.Parameters;
            var numParameters = parameters.Count;
            var suppliedValues = new object [numParameters];
            var isSupplied = new bool [numParameters];
            foreach (var argument in call.Arguments) {
                if (argument.Position >= numParameters)
                    throw new ArgumentException (
                        "Argument position " + argument.Position + " out of range" +
                        " for " + procedure.FullyQualifiedName);
                suppliedValues [argument.Position] = argument.Value;
                isSupplied [argument.Position] = true;
            }

            if (args != null) {
                foreach (var position in args.Keys) {
                    if (position < 0 || position >= numParameters)
                        throw new ArgumentException (
                            "Argument position " + position + " out of range" +
                            " for " + procedure.FullyQualifiedName);
                }
            }

            // For each parameter, the argument is either an expression or a
            // constant value known when the expression is created
            var constValues = new object [numParameters];
            var exprValues = new LinqExpression [numParameters];
            for (int i = 0; i < numParameters; i++) {
                var parameter = parameters [i];
                Expression argument;
                if (args != null && args.TryGetValue (i, out argument) &&
                    !ReferenceEquals (argument, null)) {
                    var converted = ConvertArgumentExpression (argument, parameter, procedure, i);
                    converted = CheckNulls (
                        converted, SpecAs (argument, converted.Type), parameter.Spec,
                        "The argument for parameter " + parameter.Name + " of " +
                        procedure.FullyQualifiedName, false);
                    exprValues [i] = CheckNotNull (converted, parameter, procedure, i);
                } else if (isSupplied [i]) {
                    CheckArgumentValue (procedure, parameter, suppliedValues [i]);
                    constValues [i] = suppliedValues [i];
                } else if (parameter.HasDefaultValue) {
                    constValues [i] = parameter.DefaultValue;
                } else {
                    throw new ArgumentException (
                        "Argument not specified for parameter " + parameter.Name +
                        " in " + procedure.FullyQualifiedName);
                }
            }

            // Invoke the procedure's method directly, with typed arguments, so the
            // arguments stay unboxed and the JIT can inline the method. The game
            // scene and null return value checks match those made for ordinary RPCs
            var hasInstance = procedure.Handler.HasInstance;
            var method = procedure.Handler.Method;
            var firstArgument = hasInstance ? 1 : 0;
            var methodParameters = method.GetParameters ();
            var arguments = new LinqExpression [numParameters - firstArgument];
            for (int i = firstArgument; i < numParameters; i++) {
                // A nullable value-type parameter is declared as its underlying type T,
                // so build the argument as the method's own Nullable<T> parameter type
                var parameterType = methodParameters [i - firstArgument].ParameterType;
                var expr = exprValues [i];
                if (expr == null)
                    arguments [i - firstArgument] =
                        LinqExpression.Constant (constValues [i], parameterType);
                else
                    arguments [i - firstArgument] = expr.Type == parameterType
                        ? expr : LinqExpression.Convert (expr, parameterType);
            }
            LinqExpression instanceExpr = null;
            if (hasInstance)
                instanceExpr = exprValues [0] ??
                    LinqExpression.Constant (constValues [0], parameters [0].Spec.Type);

            var procedureExpr = LinqExpression.Constant (procedure);
            var sceneCheck = LinqExpression.Call (
                typeof (Services).GetMethod (nameof (Services.CheckExpressionGameScene)),
                procedureExpr);

            var callExpr = BuildMethodCall (instanceExpr, method, arguments);

            if (!procedure.HasReturnType)
                return new Expression (LinqExpression.Block (typeof (void), sceneCheck, callExpr));

            // The declared type of a nullable value-type return is Nullable<T>, which keeps a
            // null representable
            var returnType = procedure.ReturnSpec.DeclaredType;
            var returnSpec = TypeSpec.Create (callExpr.Type, procedure.ReturnSpec);
            if (returnType.IsValueType || procedure.ReturnSpec.Nullable)
                return new Expression (LinqExpression.Block (
                    sceneCheck, CopyOfResult (callExpr, procedure.ReturnSpec)), returnSpec);

            // The return type is a reference type that must not be null; check the
            // value on every evaluation
            var returnValue = LinqExpression.Variable (returnType, "returnValue");
            var returnValueCheck = LinqExpression.Call (
                typeof (Services).GetMethod (nameof (Services.CheckExpressionReturnValue)),
                procedureExpr, returnValue);
            return new Expression (LinqExpression.Block (
                new [] { returnValue },
                sceneCheck,
                LinqExpression.Assign (returnValue, callExpr),
                returnValueCheck,
                CopyOfResult (returnValue, procedure.ReturnSpec)), returnSpec);
        }

        /// <summary>
        /// The value a procedure returned, with each list, set and dictionary in it copied,
        /// as decoding the value would. A function then never changes a collection the
        /// procedure holds.
        /// </summary>
        static LinqExpression CopyOfResult (LinqExpression value, TypeSpec spec)
        {
            return CopyOf (value, spec) ?? value;
        }

        /// <summary>
        /// A copy of a value, of the value's type, with each list, set and dictionary in it
        /// rebuilt. Null when the value holds no collection, and needs no copy.
        /// </summary>
        static LinqExpression CopyOf (LinqExpression value, TypeSpec spec)
        {
            LinqExpression copy;
            switch (spec.Kind) {
            case TypeKind.List:
                copy = CopyCall (nameof (CopyList), value, spec.Types [0]);
                break;
            case TypeKind.Set:
                copy = CopyCall (nameof (CopySet), value, spec.Types [0]);
                break;
            case TypeKind.Dictionary:
                copy = CopyCall (nameof (CopyDictionary), value, spec.Types [1], spec.Types [0].DeclaredType);
                break;
            case TypeKind.Tuple:
                copy = CopyOfTuple (value, spec);
                break;
            case TypeKind.Struct:
                copy = CopyOfStruct (value, spec.Type);
                break;
            default:
                return null;
            }
            return copy == null || copy.Type == value.Type ? copy : LinqExpression.Convert (copy, value.Type);
        }

        /// <summary>
        /// A call of a helper copying a collection, given a function copying each of its
        /// values, or null when the values need no copy. A dictionary's helper also takes the
        /// type of its keys, which need no copy.
        /// </summary>
        static LinqExpression CopyCall (
            string helper, LinqExpression collection, TypeSpec element, System.Type keyType = null)
        {
            var type = element.DeclaredType;
            var x = LinqExpression.Parameter (type, "value");
            var copy = CopyOf (x, element);
            var function = typeof (Func<,>).MakeGenericType (type, type);
            var method = typeof (Expression)
                .GetMethod (helper, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (keyType == null ? new [] { type } : new [] { keyType, type });
            return LinqExpression.Call (
                method, collection,
                copy == null ? (LinqExpression)LinqExpression.Constant (null, function)
                    : LinqExpression.Lambda (function, copy, x));
        }

        static List<T> CopyList<T> (IList<T> values, Func<T, T> copy)
        {
            if (values == null)
                return null;
            return copy == null ? new List<T> (values) : values.Select (copy).ToList ();
        }

        static HashSet<T> CopySet<T> (ISet<T> values, Func<T, T> copy)
        {
            if (values == null)
                return null;
            return copy == null ? new HashSet<T> (values) : new HashSet<T> (values.Select (copy));
        }

        static Dictionary<TKey, TValue> CopyDictionary<TKey, TValue> (
            IDictionary<TKey, TValue> values, Func<TValue, TValue> copy)
        {
            if (values == null)
                return null;
            return copy == null ? new Dictionary<TKey, TValue> (values)
                : values.ToDictionary (x => x.Key, x => copy (x.Value));
        }

        /// <summary>
        /// A copy of a tuple with a collection in it, with a null kept. Null when the tuple
        /// holds no collection.
        /// </summary>
        static LinqExpression CopyOfTuple (LinqExpression tuple, TypeSpec spec)
        {
            var variable = LinqExpression.Variable (tuple.Type);
            var elements = new LinqExpression [spec.Types.Count];
            var copied = false;
            for (int i = 0; i < elements.Length; i++) {
                var element = LinqExpression.Property (variable, "Item" + (i + 1));
                var copy = CopyOf (element, spec.Types [i]);
                copied |= copy != null;
                elements [i] = copy ?? element;
            }
            if (!copied)
                return null;
            var create = typeof (Tuple)
                .GetMethods ()
                .First (m => m.Name == "Create" && m.GetGenericArguments ().Length == elements.Length)
                .MakeGenericMethod (tuple.Type.GetGenericArguments ());
            return LinqExpression.Block (
                new [] { variable },
                LinqExpression.Assign (variable, tuple),
                LinqExpression.Condition (
                    LinqExpression.Equal (variable, LinqExpression.Constant (null, tuple.Type)),
                    LinqExpression.Constant (null, tuple.Type), LinqExpression.Call (create, elements)));
        }

        /// <summary>
        /// A copy of a structure with a collection in a field, with a null kept. Null when no
        /// field holds a collection.
        /// </summary>
        static LinqExpression CopyOfStruct (LinqExpression structure, System.Type type)
        {
            var nullable = structure.Type != type;
            var variable = LinqExpression.Variable (structure.Type);
            LinqExpression value = nullable ? (LinqExpression)LinqExpression.Property (variable, "Value") : variable;
            IList<PropertyInfo> fields;
            IList<TypeSpec> specs;
            TypeUtils.GetStructFieldsAndSpecs (type, out fields, out specs);
            var bindings = new MemberBinding [fields.Count];
            var copied = false;
            for (int i = 0; i < fields.Count; i++) {
                var field = LinqExpression.Property (value, fields [i]);
                var copy = CopyOf (field, specs [i]);
                copied |= copy != null;
                bindings [i] = LinqExpression.Bind (fields [i], copy ?? field);
            }
            if (!copied)
                return null;
            LinqExpression result = LinqExpression.MemberInit (LinqExpression.New (type), bindings);
            if (nullable)
                result = LinqExpression.Condition (
                    LinqExpression.Property (variable, "HasValue"),
                    LinqExpression.Convert (result, structure.Type), variable);
            return LinqExpression.Block (
                new [] { variable }, LinqExpression.Assign (variable, structure), result);
        }

        static LinqExpression BuildMethodCall (
            LinqExpression instance, MethodInfo method, LinqExpression [] arguments)
        {
            return instance == null
                ? LinqExpression.Call (method, arguments)
                : LinqExpression.Call (instance, method, arguments);
        }

        // Check a reference passed to a parameter that is not nullable
        static LinqExpression CheckNotNull (
            LinqExpression expr, Scanner.ParameterSignature parameter,
            Scanner.ProcedureSignature procedure, int position)
        {
            var constant = expr as ConstantExpression;
            if (parameter.Spec.Nullable || expr.Type.IsValueType ||
                (constant != null && constant.Value != null))
                return expr;
            return LinqExpression.Call (
                typeof (Services).GetMethod (nameof (Services.CheckExpressionArgument))
                    .MakeGenericMethod (expr.Type),
                LinqExpression.Constant (procedure), LinqExpression.Constant (position), expr);
        }

        /// <summary>
        /// Convert an argument expression to the parameter's type, allowing
        /// upcasts and implicit numeric conversions.
        /// </summary>
        static LinqExpression ConvertArgumentExpression (
            Expression expression, Scanner.ParameterSignature parameter,
            Scanner.ProcedureSignature procedure, int position)
        {
            LinqExpression expr = expression;
            var type = parameter.Spec.Type;
            if (expr.Type == type)
                return expr;
            // A nullable value-type parameter is declared as its underlying type, and
            // takes a nullable value as it is
            if (parameter.Spec.Nullable && Nullable.GetUnderlyingType (expr.Type) == type)
                return expr;
            // A nullable number widens to a nullable parameter with its null kept
            var underlying = Nullable.GetUnderlyingType (expr.Type);
            if (parameter.Spec.Nullable && underlying != null &&
                IsNumericType (underlying) && IsNumericType (type) &&
                FindCommonNumericType (underlying, type) == type)
                return LinqExpression.Convert (expr, typeof (Nullable<>).MakeGenericType (type));
            if (underlying != null)
                expr = LinqExpression.Call (
                    typeof (Services).GetMethod (nameof (Services.CheckExpressionValueArgument))
                        .MakeGenericMethod (underlying),
                    LinqExpression.Constant (procedure), LinqExpression.Constant (position), expr);
            if (expr.Type == type)
                return expr;
            var widened = Widen (expr, type);
            if (widened != null)
                return widened;
            throw new InvalidOperationException (
                "Incorrect expression type for parameter " + parameter.Name +
                " in " + procedure.FullyQualifiedName + ". " +
                "Expected an expression of type " + type + ", got " + expr.Type +
                NestedNullabilityHint (expr.Type, type));
        }

        static void CheckArgumentValue (Scanner.ProcedureSignature procedure, Scanner.ParameterSignature parameter, object value)
        {
            var type = parameter.Spec.Type;
            if (value != null && !type.IsInstanceOfType (value))
                throw new ArgumentException (
                    "Incorrect argument type for parameter " + parameter.Name +
                    " in " + procedure.FullyQualifiedName + ". " +
                    "Expected an argument of type " + type + ", got " + value.GetType ());
            if (value == null && !parameter.Spec.Nullable)
                throw new ArgumentException (
                    "Incorrect argument type for parameter " + parameter.Name +
                    " in " + procedure.FullyQualifiedName + ". " +
                    "Expected an argument of type " + type + ", got null");
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <remarks>
        /// Collections, tuples, bytes and structures compare by their contents. Throws an
        /// exception if the operands are of unrelated types.
        /// </remarks>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression Equal(Expression arg0, Expression arg1)
        {
            return NullableBinaryOp(EqualityOp, arg0, arg1);
        }

        /// <summary>
        /// Inequality comparison.
        /// </summary>
        /// <remarks>
        /// Collections, tuples, bytes and structures compare by their contents. Throws an
        /// exception if the operands are of unrelated types.
        /// </remarks>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression NotEqual(Expression arg0, Expression arg1)
        {
            return NullableBinaryOp(
                (left, right) => LinqExpression.Not (EqualityOp (left, right)), arg0, arg1);
        }

        // Objects compare by Equals, as a service object getter returns a new object on each
        // call. Collections, tuples, bytes and structures compare by their contents
        static LinqExpression EqualityOp (LinqExpression left, LinqExpression right)
        {
            if (left.Type != right.Type) {
                var widened = WidenTuple (left, right.Type);
                if (widened != null)
                    left = widened;
                else
                    right = WidenTuple (right, left.Type) ?? right;
            }
            var type0 = left.Type;
            var type1 = right.Type;
            var related = type0.IsAssignableFrom (type1) || type1.IsAssignableFrom (type0);
            // A nullable structure has no equality operator to lift
            if (type0 == type1 && TypeUtils.IsAStructType (Nullable.GetUnderlyingType (type0) ?? type0))
                return CallValueEquals (left, right);
            if (type0.IsValueType || type1.IsValueType || type0 == typeof (string) ||
                type1 == typeof (string)) {
                if (!related)
                    throw new ArgumentException (
                        "Cannot compare a value of type " + type0 + " with a value of type " + type1);
                return LinqExpression.Equal (left, right);
            }
            if (!related && !(ComparesByContent (type0) && ComparesByContent (type1) && AreComparable (type0, type1)))
                throw new ArgumentException (
                    "Cannot compare a value of type " + type0 + " with a value of type " + type1);
            return CallValueEquals (left, right);
        }

        /// <summary>
        /// Whether values of two types can be equal. Two collections or tuples can be equal
        /// when they are of the same kind and their elements can be equal.
        /// </summary>
        static bool AreComparable (System.Type type0, System.Type type1)
        {
            type0 = CollectionInterfaceType (NullableValueType (type0) ?? type0);
            type1 = CollectionInterfaceType (NullableValueType (type1) ?? type1);
            if (type0.IsAssignableFrom (type1) || type1.IsAssignableFrom (type0) ||
                (IsNumericType (type0) && IsNumericType (type1)))
                return true;
            if (!IsAJoinableType (type0) || !IsAJoinableType (type1))
                return false;
            var tuple = TypeUtils.IsATupleCollectionType (type0);
            if (tuple != TypeUtils.IsATupleCollectionType (type1) ||
                (!tuple && type0.GetGenericTypeDefinition () != type1.GetGenericTypeDefinition ()))
                return false;
            var arguments0 = type0.GetGenericArguments ();
            var arguments1 = type1.GetGenericArguments ();
            return arguments0.Length == arguments1.Length &&
                arguments0.Zip (arguments1, AreComparable).All (x => x);
        }

        static LinqExpression CallValueEquals (LinqExpression left, LinqExpression right)
        {
            return LinqExpression.Call (
                typeof (Expression).GetMethod (
                    nameof (ValueEquals), BindingFlags.Static | BindingFlags.NonPublic),
                LinqExpression.Convert (left, typeof (object)),
                LinqExpression.Convert (right, typeof (object)));
        }

        static bool ComparesByContent (System.Type type)
        {
            return type == typeof (byte[]) || TypeUtils.IsACollectionType (type);
        }

        static bool ValueEquals (object left, object right)
        {
            if (ReferenceEquals (left, right))
                return true;
            if (left == null || right == null || left is string)
                return Equals (left, right);
            var leftBytes = left as byte[];
            var rightBytes = right as byte[];
            if (leftBytes != null || rightBytes != null)
                return leftBytes != null && rightBytes != null && leftBytes.SequenceEqual (rightBytes);
            var leftDictionary = left as IDictionary;
            var rightDictionary = right as IDictionary;
            if (leftDictionary != null || rightDictionary != null)
                return leftDictionary != null && rightDictionary != null &&
                    DictionariesEqual (leftDictionary, rightDictionary);
            var leftIsASet = TypeUtils.IsASetCollectionType (left.GetType ());
            if (leftIsASet || TypeUtils.IsASetCollectionType (right.GetType ()))
                return leftIsASet && TypeUtils.IsASetCollectionType (right.GetType ()) &&
                    SetsEqual ((IEnumerable)left, (IEnumerable)right);
            var leftList = left as IList;
            var rightList = right as IList;
            if (leftList != null || rightList != null)
                return leftList != null && rightList != null && ListsEqual (leftList, rightList);
            var leftType = left.GetType ();
            var rightType = right.GetType ();
            if (TypeUtils.IsATupleCollectionType (leftType) || TypeUtils.IsATupleCollectionType (rightType))
                return TuplesEqual (left, right);
            if (leftType != rightType && IsNumericType (leftType) && IsNumericType (rightType))
                return NumbersEqual (left, right);
            if (leftType == rightType && TypeUtils.IsAStructType (leftType))
                return TypeUtils.GetStructFields (leftType).All (
                    field => ValueEquals (field.GetValue (left, null), field.GetValue (right, null)));
            return Equals (left, right);
        }

        /// <summary>
        /// Compares the elements of two tuples with <see cref="ValueEquals"/>. The
        /// tuples can differ in type, such as a tuple of integers and one of doubles.
        /// </summary>
        static bool TuplesEqual (object left, object right)
        {
            var leftType = left.GetType ();
            var rightType = right.GetType ();
            if (!TypeUtils.IsATupleCollectionType (leftType) ||
                !TypeUtils.IsATupleCollectionType (rightType))
                return false;
            var count = leftType.GetGenericArguments ().Length;
            if (rightType.GetGenericArguments ().Length != count)
                return false;
            if (leftType == rightType)
                return ((IStructuralEquatable)left).Equals (right, ValueComparer<object>.Instance);
            for (int i = 1; i <= count; i++) {
                var name = "Item" + i;
                if (!ValueEquals (leftType.GetProperty (name).GetValue (left, null),
                                  rightType.GetProperty (name).GetValue (right, null)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Compares two numbers of differing types at their common type. An unsigned 64
        /// bit integer and a signed integer have no common type, and are equal when the
        /// signed one is not negative and has the same value.
        /// </summary>
        static bool NumbersEqual (object left, object right)
        {
            var common = FindCommonNumericType (left.GetType (), right.GetType ());
            if (common != null)
                return Convert.ChangeType (left, common).Equals (Convert.ChangeType (right, common));
            var unsigned = left is ulong ? (ulong)left : (ulong)right;
            var signed = Convert.ToInt64 (left is ulong ? right : left);
            return signed >= 0 && (ulong)signed == unsigned;
        }

        static bool DictionariesEqual (IDictionary left, IDictionary right)
        {
            if (left.Count != right.Count)
                return false;
            foreach (DictionaryEntry entry in left) {
                object value;
                if (!TryGetValue (right, entry.Key, out value) || !ValueEquals (entry.Value, value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Look up a key in a dictionary, comparing keys with <see cref="ValueEquals"/>
        /// when the dictionary's key type differs from the key's, such as an integer key
        /// in a dictionary with long keys.
        /// </summary>
        static bool TryGetValue (IDictionary dictionary, object key, out object value)
        {
            if (dictionary.Contains (key)) {
                value = dictionary [key];
                return true;
            }
            foreach (DictionaryEntry entry in dictionary) {
                if (entry.Key.GetType () != key.GetType () && ValueEquals (entry.Key, key)) {
                    value = entry.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        // A set a procedure returns compares its values by the default equality, so it can
        // hold two values that are equal here
        static bool SetsEqual (IEnumerable left, IEnumerable right)
        {
            var rightValues = right.Cast<object> ().ToList ();
            var leftValues = left.Cast<object> ().ToList ();
            return leftValues.All (x => rightValues.Any (y => ValueEquals (x, y))) &&
                rightValues.All (x => leftValues.Any (y => ValueEquals (x, y)));
        }

        static bool ListsEqual (IList left, IList right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++) {
                if (!ValueEquals (left [i], right [i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// A hash code that agrees with <see cref="ValueEquals"/>.
        /// </summary>
        static int ValueHash (object value)
        {
            if (value == null)
                return 0;
            var type = value.GetType ();
            // Equal numbers of differing types convert to the same double, unless one is a float
            // and the other an integer equal to it only once rounded, such as 16777217 and
            // 16777216f. The values hashed together share a type, so that case does not arise.
            // Zero and negative zero are equal, as are all NaNs.
            if (IsNumericType (type)) {
                var number = Convert.ToDouble (value);
                return number == 0 || double.IsNaN (number) ? 0 : number.GetHashCode ();
            }
            if (value is string)
                return value.GetHashCode ();
            var bytes = value as byte[];
            if (bytes != null)
                return CombineHashes (bytes.Select (x => (int)x));
            var dictionary = value as IDictionary;
            if (dictionary != null)
                return CombineUnorderedHashes (dictionary.Cast<DictionaryEntry> ().Select (
                    x => CombineHashes (new [] { ValueHash (x.Key), ValueHash (x.Value) })));
            if (TypeUtils.IsASetCollectionType (type))
                return CombineUnorderedHashes (((IEnumerable)value).Cast<object> ().Select (ValueHash));
            var list = value as IList;
            if (list != null)
                return CombineHashes (list.Cast<object> ().Select (ValueHash));
            if (TypeUtils.IsATupleCollectionType (type))
                return ((IStructuralEquatable)value).GetHashCode (ValueComparer<object>.Instance);
            if (TypeUtils.IsAStructType (type))
                return CombineHashes (TypeUtils.GetStructFields (type).Select (
                    field => ValueHash (field.GetValue (value, null))));
            return value.GetHashCode ();
        }

        static int CombineHashes (IEnumerable<int> hashes)
        {
            unchecked {
                var hash = 17;
                foreach (var x in hashes)
                    hash = hash * 31 + x;
                return hash;
            }
        }

        // Each hash counts once, as a set can hold two values that are equal here
        static int CombineUnorderedHashes (IEnumerable<int> hashes)
        {
            unchecked {
                var hash = 0;
                foreach (var x in hashes.Distinct ())
                    hash += x;
                return hash;
            }
        }

        /// <summary>
        /// Compares values with <see cref="ValueEquals"/>.
        /// </summary>
        sealed class ValueComparer<T> : IEqualityComparer<T>, IEqualityComparer
        {
            public static readonly ValueComparer<T> Instance = new ValueComparer<T> ();

            public bool Equals (T x, T y)
            {
                return ValueEquals (x, y);
            }

            public int GetHashCode (T obj)
            {
                return ValueHash (obj);
            }

            bool IEqualityComparer.Equals (object x, object y)
            {
                return ValueEquals (x, y);
            }

            int IEqualityComparer.GetHashCode (object obj)
            {
                return ValueHash (obj);
            }
        }

        /// <summary>
        /// The comparer for the values of a collection. A number, boolean, enumeration or
        /// string is equal by value under the default comparer.
        /// </summary>
        internal static IEqualityComparer<T> ValuesComparer<T> ()
        {
            var type = Nullable.GetUnderlyingType (typeof (T)) ?? typeof (T);
            if (type == typeof (string) || (type.IsValueType && !TypeUtils.IsAStructType (type)))
                return EqualityComparer<T>.Default;
            return ValueComparer<T>.Instance;
        }

        /// <summary>
        /// A call giving the comparer for values of the given type.
        /// </summary>
        static LinqExpression ValuesComparerCall (System.Type type)
        {
            return LinqExpression.Call (typeof (Expression)
                .GetMethod (nameof (ValuesComparer), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (type));
        }

        /// <summary>
        /// A set of the given values, compared with <see cref="ValueEquals"/>.
        /// </summary>
        internal static HashSet<T> SetOf<T> (IEnumerable<T> values)
        {
            return new HashSet<T> (values, ValuesComparer<T> ());
        }

        /// <summary>
        /// A call building a set of the given values.
        /// </summary>
        static LinqExpression SetOfCall (System.Type valueType, LinqExpression values)
        {
            return LinqExpression.Call (typeof (Expression)
                .GetMethod (nameof (SetOf), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (valueType), values);
        }

        /// <summary>
        /// Greater than numerical comparison.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression GreaterThan(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.GreaterThan, arg0, arg1, nameof (GreaterThan));
        }

        /// <summary>
        /// Greater than or equal numerical comparison.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression GreaterThanOrEqual(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.GreaterThanOrEqual, arg0, arg1, nameof (GreaterThanOrEqual));
        }

        /// <summary>
        /// Less than numerical comparison.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression LessThan(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.LessThan, arg0, arg1, nameof (LessThan));
        }

        /// <summary>
        /// Less than or equal numerical comparison.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression LessThanOrEqual(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.LessThanOrEqual, arg0, arg1, nameof (LessThanOrEqual));
        }

        /// <summary>
        /// Logical or bitwise and, which evaluates both operands.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression And(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.And, arg0, arg1, nameof (And));
        }

        /// <summary>
        /// Logical or bitwise or, which evaluates both operands.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression Or(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.Or, arg0, arg1, nameof (Or));
        }

        /// <summary>
        /// Logical and, which evaluates the second operand only when the first is true.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression ConditionalAnd (Expression arg0, Expression arg1)
        {
            CheckOperands (arg0, arg1);
            return new Expression (LinqExpression.AndAlso (
                UnwrapNullable (arg0, "The first operand of ConditionalAnd"),
                UnwrapNullable (arg1, "The second operand of ConditionalAnd")));
        }

        /// <summary>
        /// Logical or, which evaluates the second operand only when the first is false.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression ConditionalOr (Expression arg0, Expression arg1)
        {
            CheckOperands (arg0, arg1);
            return new Expression (LinqExpression.OrElse (
                UnwrapNullable (arg0, "The first operand of ConditionalOr"),
                UnwrapNullable (arg1, "The second operand of ConditionalOr")));
        }

        /// <summary>
        /// Logical or bitwise exclusive or.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression ExclusiveOr(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.ExclusiveOr, arg0, arg1, nameof (ExclusiveOr));
        }

        /// <summary>
        /// Logical or bitwise negation.
        /// </summary>
        /// <param name="arg">The operand.</param>
        [KRPCMethod]
        public static Expression Not(Expression arg)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            return new Expression (LinqExpression.Not (UnwrapNullable (arg, "The operand of Not")));
        }

        /// <summary>
        /// Numerical negation, which negates a uint as a long.
        /// </summary>
        /// <param name="arg">The operand.</param>
        [KRPCMethod]
        public static Expression Negate (Expression arg)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var operand = UnwrapNullable (arg, "The operand of Negate");
            if (operand.Type == typeof (uint))
                operand = LinqExpression.Convert (operand, typeof (long));
            else if (operand.Type == typeof (ulong) || !IsNumericType (operand.Type))
                throw new InvalidOperationException (
                    "Cannot negate a value of type " + operand.Type);
            return new Expression (LinqExpression.Negate (operand));
        }

        /// <summary>
        /// The second argument if the condition is true, and the third argument otherwise.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the two values have no common type.
        /// </remarks>
        /// <param name="condition">The boolean condition.</param>
        /// <param name="ifTrue">The value when the condition is true.</param>
        /// <param name="ifFalse">The value when the condition is false.</param>
        [KRPCMethod]
        public static Expression Conditional(Expression condition, Expression ifTrue, Expression ifFalse)
        {
            if (ReferenceEquals (condition, null))
                throw new ArgumentNullException (nameof (condition));
            if (ReferenceEquals (ifTrue, null))
                throw new ArgumentNullException (nameof (ifTrue));
            if (ReferenceEquals (ifFalse, null))
                throw new ArgumentNullException (nameof (ifFalse));
            LinqExpression left = ifTrue;
            LinqExpression right = ifFalse;
            // A branch that throws takes the type of the other branch
            left = ThrowAs (left, right);
            right = ThrowAs (right, left);
            PromoteNullableOperands (ref left, ref right);
            if (left.Type != right.Type) {
                // Tuples and collections join position by position
                var common = CommonType (new [] { left.Type, right.Type });
                if (common != null) {
                    left = Widen (left, common);
                    right = Widen (right, common);
                }
            }
            if (left.Type != right.Type)
                throw new InvalidOperationException (
                    "The branches of Conditional must be of the same type, or numbers with a " +
                    "common type, got " + left.Type + " and " + right.Type);
            var result = LinqExpression.Condition (UnwrapNullable (condition, "The condition"), left, right);
            TypeSpec resultSpec = null;
            if (result.Type != typeof (void)) {
                foreach (var branch in new [] { ifTrue, ifFalse }) {
                    // A branch that throws has no spec to join
                    if (branch.Type == typeof (void))
                        continue;
                    var branchSpec = SpecAs (branch, result.Type);
                    resultSpec = resultSpec == null ? branchSpec : TypeSpec.Join (resultSpec, branchSpec);
                }
            }
            return new Expression (result, resultSpec);
        }

        static LinqExpression ThrowAs (LinqExpression branch, LinqExpression other)
        {
            if (branch == null || other == null || branch.NodeType != ExpressionType.Throw ||
                branch.Type != typeof (void) || other.Type == typeof (void))
                return branch;
            return LinqExpression.Throw (((UnaryExpression)branch).Operand, other.Type);
        }

        /// <summary>
        /// Numerical addition.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression Add(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.Add, arg0, arg1, nameof (Add));
        }

        /// <summary>
        /// Numerical subtraction.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression Subtract(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.Subtract, arg0, arg1, nameof (Subtract));
        }

        /// <summary>
        /// Numerical multiplication.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression Multiply(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp (LinqExpression.Multiply, arg0, arg1, nameof (Multiply));
        }

        /// <summary>
        /// Numerical division.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression Divide(Expression arg0, Expression arg1)
        {
            return SignedDivisionOp (LinqExpression.Divide, false, arg0, arg1, nameof (Divide));
        }

        /// <summary>
        /// Numerical modulo operator.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        /// <returns>The remainder of arg0 divided by arg1</returns>
        [KRPCMethod]
        public static Expression Modulo(Expression arg0, Expression arg1)
        {
            return SignedDivisionOp (LinqExpression.Modulo, true, arg0, arg1, nameof (Modulo));
        }

        /// <summary>
        /// Integer division or remainder, where dividing the smallest signed integer by -1
        /// wraps as the other integer operators do. The division itself throws.
        /// </summary>
        static Expression SignedDivisionOp (
            Func<LinqExpression, LinqExpression, LinqExpression> op, bool remainder,
            Expression arg0, Expression arg1, string name)
        {
            CheckOperands (arg0, arg1);
            LinqExpression left = arg0;
            LinqExpression right = arg1;
            PromoteOperands (ref left, ref right, name);
            var type = left.Type;
            if (right.Type != type || (type != typeof (int) && type != typeof (long)))
                return new Expression (op (left, right));
            var dividend = LinqExpression.Variable (type, "dividend");
            var divisor = LinqExpression.Variable (type, "divisor");
            var minusOne = LinqExpression.Constant (Convert.ChangeType (-1, type), type);
            var wrapped = remainder
                ? (LinqExpression)LinqExpression.Constant (Convert.ChangeType (0, type), type)
                : LinqExpression.Negate (dividend);
            return new Expression (LinqExpression.Block (
                new [] { dividend, divisor },
                LinqExpression.Assign (dividend, left),
                LinqExpression.Assign (divisor, right),
                LinqExpression.Condition (
                    LinqExpression.Equal (divisor, minusOne), wrapped, op (dividend, divisor))));
        }

        /// <summary>
        /// Numerical power operator.
        /// </summary>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        /// <returns>arg0 raised to the power of arg1, at the common type of the operands</returns>
        [KRPCMethod]
        public static Expression Power(Expression arg0, Expression arg1)
        {
            CheckOperands (arg0, arg1);
            LinqExpression left = arg0;
            LinqExpression right = arg1;
            PromoteOperands (ref left, ref right, nameof (Power));
            var type = left.Type;
            if (type == typeof (double) || type == typeof (float) || !IsNumericType (type))
                return new Expression (DoublePower (left, right, type));
            // An integer power is exact, and wraps on overflow as the other integer
            // operators do. A negative exponent gives a fraction, truncated to an integer
            var value = LinqExpression.Variable (type, "value");
            var exponent = LinqExpression.Variable (type, "exponent");
            LinqExpression power = LinqExpression.Convert (
                LinqExpression.Call (
                    typeof (Expression).GetMethod (
                        nameof (WrappingPower), BindingFlags.Static | BindingFlags.NonPublic),
                    LinqExpression.Convert (value, typeof (ulong)),
                    LinqExpression.Convert (exponent, typeof (ulong))),
                type);
            if (type == typeof (int) || type == typeof (long))
                power = LinqExpression.Condition (
                    LinqExpression.LessThan (
                        exponent, LinqExpression.Constant (Convert.ChangeType (0, type), type)),
                    DoublePower (value, exponent, type), power);
            return new Expression (LinqExpression.Block (
                new [] { value, exponent },
                LinqExpression.Assign (value, left),
                LinqExpression.Assign (exponent, right),
                power));
        }

        // Math.Pow takes and returns a double, so the result is converted back to the
        // type the operands promoted to
        static LinqExpression DoublePower (LinqExpression left, LinqExpression right, System.Type type)
        {
            var power = LinqExpression.Power (
                LinqExpression.Convert (left, typeof (double)),
                LinqExpression.Convert (right, typeof (double)));
            return LinqExpression.Convert (power, type);
        }

        /// <summary>
        /// A power by repeated squaring, modulo 2^64. The low bits of the result are
        /// those of the power at a narrower or a signed integer type.
        /// </summary>
        static ulong WrappingPower (ulong value, ulong exponent)
        {
            ulong result = 1;
            unchecked {
                while (exponent != 0) {
                    if ((exponent & 1) != 0)
                        result *= value;
                    value *= value;
                    exponent >>= 1;
                }
            }
            return result;
        }

        /// <summary>
        /// Bitwise left shift.
        /// </summary>
        /// <remarks>
        /// Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
        /// for a 64-bit value.
        /// </remarks>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression LeftShift(Expression arg0, Expression arg1)
        {
            return ShiftOp (LinqExpression.LeftShift, arg0, arg1, nameof (LeftShift));
        }

        /// <summary>
        /// Bitwise right shift.
        /// </summary>
        /// <remarks>
        /// Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
        /// for a 64-bit value.
        /// </remarks>
        /// <param name="arg0">The first operand.</param>
        /// <param name="arg1">The second operand.</param>
        [KRPCMethod]
        public static Expression RightShift(Expression arg0, Expression arg1)
        {
            return ShiftOp (LinqExpression.RightShift, arg0, arg1, nameof (RightShift));
        }

        static Expression ShiftOp (
            Func<LinqExpression, LinqExpression, LinqExpression> op, Expression arg0, Expression arg1, string name)
        {
            CheckOperands (arg0, arg1);
            var value = UnwrapNullable (arg0, "The first operand of " + name);
            var count = UnwrapNullable (arg1, "The second operand of " + name);
            // The count of a shift is an int
            if (count.Type != typeof (int) && IsNumericType (count.Type) &&
                count.Type != typeof (double) && count.Type != typeof (float))
                count = LinqExpression.Convert (count, typeof (int));
            return new Expression (op (value, count));
        }

        /// <summary>
        /// A value cast to the given type.
        /// </summary>
        /// <remarks>
        /// A tuple or collection is cast element by element.
        /// </remarks>
        /// <param name="arg">The value to cast.</param>
        /// <param name="type">Type to cast the argument to.</param>
        [KRPCMethod]
        public static Expression Cast(Expression arg, Type type)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            var target = type.InternalType;
            if (IsALazySequence (arg.Type) && !target.IsAssignableFrom (arg.Type))
                throw new InvalidOperationException (
                    "Cannot cast a lazily evaluated collection to " + target + ". " +
                    "Use ToList or ToSet to convert it.");
            LinqExpression converted;
            if (IsATupleOrCollection (arg.Type) && !target.IsAssignableFrom (arg.Type)) {
                converted = Widen (arg, target, true);
                if (converted == null)
                    throw new InvalidOperationException (
                        "Cannot cast a value of type " + arg.Type + " to " + target +
                        NestedNullabilityHint (arg.Type, target));
            } else {
                LinqExpression value = arg;
                if (target.IsValueType && NullableValueType (target) == null)
                    value = UnwrapNullable (value, "The value cast to " + target);
                converted = LinqExpression.Convert (value, target);
            }
            return new Expression (
                CheckNulls (converted, SpecAs (arg, converted.Type), type.Spec, "The value cast to " + target),
                type.Spec);
        }

        /// <summary>
        /// A named parameter of a function.
        /// </summary>
        /// <remarks>
        /// A value is bound to it by name with <see cref="Invoke"/>, or by its position in
        /// the parameters of a <see cref="Lambda"/>.
        /// </remarks>
        /// <returns>A named parameter.</returns>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        [KRPCMethod]
        public static Expression Parameter (string name, Type type)
        {
            if (ReferenceEquals (name, null))
                throw new ArgumentNullException (nameof (name));
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            return new Expression (LinqExpression.Parameter (type.InternalType, name), type.Spec);
        }

        /// <summary>
        /// A function with the given parameters and body.
        /// </summary>
        /// <remarks>
        /// The body is an expression or a block of statements, and a body with no value is
        /// evaluated for its effects. A <see cref="Return"/> or <see cref="ReturnNothing"/>
        /// statement ends the evaluation.
        /// </remarks>
        /// <returns>A function.</returns>
        /// <param name="parameters">The parameters of the function.</param>
        /// <param name="body">The body of the function.</param>
        [KRPCMethod]
        public static Expression Lambda (IList<Expression> parameters, Expression body)
        {
            if (ReferenceEquals (parameters, null))
                throw new ArgumentNullException (nameof (parameters));
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            TypeSpec bodySpec;
            var boundBody = BindReturns (body, out bodySpec);
            var parameterNodes = new ParameterExpression [parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
                parameterNodes [i] = AsVariable (
                    parameters [i], "Expected a parameter, created with Parameter");
            // Invoke passes arguments by parameter name
            var duplicate = parameterNodes.GroupBy (x => x.Name).FirstOrDefault (x => x.Count () > 1);
            if (duplicate != null)
                throw new ArgumentException (
                    "The function has more than one parameter named " + duplicate.Key);
            var lambda = new Expression (LinqExpression.Lambda (boundBody, parameterNodes));
            lambda.parameterSpecs = parameters.Select (x => x.Spec).ToArray ();
            lambda.resultSpec = bodySpec;
            return lambda;
        }

        /// <summary>
        /// A function given to an operation that passes it values of the given specs. Where
        /// a value can hold a null that the parameter cannot, the function is wrapped in one
        /// that checks the value first.
        /// </summary>
        static LinqExpression AdaptFunction (Expression function, string position, params TypeSpec[] argumentSpecs)
        {
            var lambda = function.internalExpression as LambdaExpression;
            var parameterSpecs = function.parameterSpecs;
            if (lambda == null || parameterSpecs == null || parameterSpecs.Length != argumentSpecs.Length)
                return function;
            var adapted = false;
            var parameters = lambda.Parameters.Select (x => LinqExpression.Parameter (x.Type, x.Name)).ToArray ();
            var arguments = new LinqExpression [parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                arguments [i] = argumentSpecs [i] == null ? parameters [i] : CheckNulls (
                    parameters [i], TypeSpec.Create (parameters [i].Type, argumentSpecs [i]),
                    parameterSpecs [i], position);
                adapted |= arguments [i] != parameters [i];
            }
            if (!adapted)
                return function;
            return LinqExpression.Lambda (
                lambda.Type, LinqExpression.Invoke (lambda, arguments), parameters);
        }

        /// <summary>
        /// The spec of the value a function returns, as a value of the given type.
        /// </summary>
        static TypeSpec ResultSpec (Expression function, System.Type type)
        {
            return function.resultSpec == null
                ? TypeSpec.Create (type) : TypeSpec.Create (type, function.resultSpec);
        }

        /// <summary>
        /// A function call.
        /// </summary>
        /// <returns>A function call.</returns>
        /// <param name="function">The function to call.</param>
        /// <param name="args">The arguments to call the function with.</param>
        [KRPCMethod]
        public static Expression Invoke (Expression function, IDictionary<string, Expression> args)
        {
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
            var lambda = function.internalExpression as LambdaExpression;
            if (lambda == null)
                throw new ArgumentException ("Expected a function, created with Lambda");
            if (args.Count != lambda.Parameters.Count)
                throw new ArgumentException (ArgumentCountMessage (lambda.Parameters.Count, args.Count));
            var funcArgs = new LinqExpression [lambda.Parameters.Count];
            var i = 0;
            foreach (var param in lambda.Parameters) {
                Expression argument;
                if (!args.TryGetValue (param.Name, out argument) || ReferenceEquals (argument, null))
                    throw new ArgumentException (
                        "No argument given for the function's parameter " + param.Name);
                var position = "The argument for parameter " + param.Name;
                funcArgs [i] = function.parameterSpecs == null
                    ? ConvertElement (argument, param.Type, position)
                    : ConvertTo (argument, function.parameterSpecs [i], position);
                if (!param.Type.IsAssignableFrom (funcArgs [i].Type))
                    throw new InvalidOperationException (
                        "Incorrect expression type for parameter " + param.Name + ". " +
                        "Expected an expression of type " + param.Type + ", got " + funcArgs [i].Type);
                i++;
            }
            var invoke = LinqExpression.Invoke (function, funcArgs);
            return new Expression (invoke, invoke.Type == typeof (void) ? null : ResultSpec (function, invoke.Type));
        }

        static string ArgumentCountMessage (int parameters, int arguments)
        {
            return "The function takes " + parameters + (parameters == 1 ? " argument" : " arguments") +
                ", got " + arguments;
        }

        /// <summary>
        /// The type a collection is used through, which is the type <see cref="Type"/>
        /// gives a variable of it: <c>IList</c> for a list and <c>IDictionary</c> for a
        /// dictionary. A list of lists built as <c>List&lt;IList&lt;T&gt;&gt;</c> can be
        /// assigned to a variable, whereas a <c>List&lt;List&lt;T&gt;&gt;</c> cannot.
        /// </summary>
        static System.Type CollectionInterfaceType (System.Type type)
        {
            if (!type.IsGenericType)
                return type;
            var definition = type.GetGenericTypeDefinition ();
            if (definition == typeof (List<>))
                return typeof (IList<>).MakeGenericType (type.GetGenericArguments ());
            if (definition == typeof (Dictionary<,>))
                return typeof (IDictionary<,>).MakeGenericType (type.GetGenericArguments ());
            return type;
        }

        static LinqExpression AsCollectionInterface (LinqExpression expression)
        {
            var type = CollectionInterfaceType (expression.Type);
            return type == expression.Type ? expression : LinqExpression.Convert (expression, type);
        }

        /// <summary>
        /// Convert a value to the type of the position it is used in: a collection's
        /// element, a structure's field or the variable it is assigned to. A numeric
        /// conversion that widens is implicit; one that narrows requires a cast. Position
        /// describes the value, for the error when a null goes to a plain value type.
        /// </summary>
        static LinqExpression ConvertElement (Expression value, System.Type type, string position)
        {
            CheckHasValue (value, position);
            LinqExpression expression = value;
            // A nullable position takes a plain or nullable value that widens to it
            var nullableValueType = NullableValueType (type);
            if (nullableValueType != null && expression.Type != type) {
                var valueType = NullableValueType (expression.Type) ?? expression.Type;
                if (valueType == nullableValueType)
                    return LinqExpression.Convert (expression, type);
                if (IsNumericType (valueType) && IsNumericType (nullableValueType)) {
                    if (FindCommonNumericType (valueType, nullableValueType) != nullableValueType)
                        throw new InvalidOperationException (
                            "No implicit conversion from " + expression.Type + " to " + type + ". " +
                            "Use a cast to convert the value.");
                    return LinqExpression.Convert (expression, type);
                }
            }
            if (type.IsValueType && Nullable.GetUnderlyingType (type) == null)
                expression = UnwrapNullable (expression, position);
            if (expression.Type != type &&
                TypeUtils.IsATupleCollectionType (expression.Type) &&
                TypeUtils.IsATupleCollectionType (type)) {
                var widened = WidenTuple (expression, type);
                if (widened == null)
                    throw new InvalidOperationException (
                        "No implicit conversion from " + expression.Type + " to " + type +
                        NestedNullabilityHint (expression.Type, type));
                return widened;
            }
            if (IsATupleOrCollection (expression.Type) && !type.IsAssignableFrom (expression.Type)) {
                var widened = WidenCollection (expression, type);
                if (widened == null)
                    throw new InvalidOperationException (
                        "No implicit conversion from " + expression.Type + " to " + type +
                        NestedNullabilityHint (expression.Type, type));
                return widened;
            }
            if (expression.Type == type ||
                !IsNumericType (expression.Type) || !IsNumericType (type))
                return expression;
            if (FindCommonNumericType (expression.Type, type) != type)
                throw new InvalidOperationException (
                    "No implicit conversion from " + expression.Type + " to " + type + ". " +
                    "Use a cast to convert the value.");
            return LinqExpression.Convert (expression, type);
        }

        /// <summary>
        /// Throws an exception if an expression has no value, such as a statement.
        /// Position describes where the value is going, for the error.
        /// </summary>
        static void CheckHasValue (Expression value, string position)
        {
            if (value.Type == typeof (void))
                throw new ArgumentException (position + " is an expression with no value");
        }

        /// <summary>
        /// Convert a value to a type it widens to: a number to a wider or nullable number, a
        /// value to a type it is assignable to, or a tuple or collection to one whose
        /// elements it widens to. Null if the value does not widen to the type. With cast, a
        /// number converts to any number, as <see cref="Cast"/> converts it.
        /// </summary>
        static LinqExpression Widen (LinqExpression expression, System.Type type, bool cast = false)
        {
            if (expression.Type == type)
                return expression;
            var valueType = NullableValueType (expression.Type);
            var targetType = NullableValueType (type);
            var from = valueType ?? expression.Type;
            var to = targetType ?? type;
            // A nullable number widens only to a nullable number
            var widensNumerically =
                IsNumericType (from) && IsNumericType (to) && (cast || FindCommonNumericType (from, to) == to) &&
                (valueType == null || targetType != null);
            if (type.IsAssignableFrom (expression.Type) || widensNumerically)
                return LinqExpression.Convert (expression, type);
            return WidenTuple (expression, type, cast) ?? WidenCollection (expression, type, cast);
        }

        /// <summary>
        /// Whether a type is a tuple, or a list, set or dictionary as
        /// <see cref="CollectionInterfaceType"/> gives it.
        /// </summary>
        static bool IsAJoinableType (System.Type type)
        {
            if (!type.IsGenericType)
                return false;
            var definition = type.GetGenericTypeDefinition ();
            return TypeUtils.IsATupleCollectionType (type) ||
                definition == typeof (IList<>) ||
                definition == typeof (HashSet<>) ||
                definition == typeof (IDictionary<,>);
        }

        static bool IsATupleOrCollection (System.Type type)
        {
            return IsAJoinableType (CollectionInterfaceType (type));
        }

        /// <summary>
        /// Whether a type is a lazily evaluated sequence, such as the result of Select, which
        /// is enumerable and is not a list, set or dictionary.
        /// </summary>
        static bool IsALazySequence (System.Type type)
        {
            return TypeSpec.SequenceElementType (type) != null && !IsATupleOrCollection (type) &&
                !global::KRPC.Utils.Reflection.IsGenericType (type, typeof (IList<>)) &&
                !global::KRPC.Utils.Reflection.IsGenericType (type, typeof (ISet<>)) &&
                !global::KRPC.Utils.Reflection.IsGenericType (type, typeof (IDictionary<,>));
        }

        /// <summary>
        /// The end of the message for a tuple or collection that does not widen to a type,
        /// which explains the cause when a nullable value type inside it is going to a
        /// position that is not nullable. Empty otherwise.
        /// </summary>
        static string NestedNullabilityHint (System.Type from, System.Type to)
        {
            if (!HasNullableValueTypeWhereNotNullable (from, to))
                return string.Empty;
            return ". The nullability of a value type inside a collection or tuple must match. " +
                "Use Select to convert the values.";
        }

        static bool HasNullableValueTypeWhereNotNullable (System.Type from, System.Type to)
        {
            from = CollectionInterfaceType (NullableValueType (from) ?? from);
            to = CollectionInterfaceType (NullableValueType (to) ?? to);
            if (!IsAJoinableType (from) || !IsAJoinableType (to))
                return false;
            var fromArguments = from.GetGenericArguments ();
            var toArguments = to.GetGenericArguments ();
            if (fromArguments.Length != toArguments.Length)
                return false;
            for (int i = 0; i < fromArguments.Length; i++) {
                if ((NullableValueType (fromArguments [i]) != null &&
                     toArguments [i].IsValueType && NullableValueType (toArguments [i]) == null) ||
                    HasNullableValueTypeWhereNotNullable (fromArguments [i], toArguments [i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Rebuild a list, set or dictionary with each element widened to the matching
        /// element type of another collection type of the same kind. Null if either is not
        /// such a collection, or an element does not widen.
        /// </summary>
        static LinqExpression WidenCollection (LinqExpression collection, System.Type type, bool cast = false)
        {
            var source = CollectionInterfaceType (collection.Type);
            if (TypeUtils.IsATupleCollectionType (type) ||
                !IsAJoinableType (source) || !IsAJoinableType (type) ||
                source.GetGenericTypeDefinition () != type.GetGenericTypeDefinition ())
                return null;
            var sourceArguments = source.GetGenericArguments ();
            var arguments = type.GetGenericArguments ();
            var helperArguments = new List<LinqExpression> {
                collection.Type == source ? collection : LinqExpression.Convert (collection, source)
            };
            for (int i = 0; i < arguments.Length; i++) {
                var element = LinqExpression.Parameter (sourceArguments [i]);
                var widened = Widen (element, arguments [i], cast);
                if (widened == null)
                    return null;
                helperArguments.Add (LinqExpression.Lambda (widened, element));
            }
            var definition = type.GetGenericTypeDefinition ();
            var helper = definition == typeof (IDictionary<,>) ? nameof (WidenDictionary)
                : definition == typeof (HashSet<>) ? nameof (WidenSet) : nameof (WidenList);
            var method = typeof (Expression)
                .GetMethod (helper, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (sourceArguments.Concat (arguments).ToArray ());
            var call = LinqExpression.Call (method, helperArguments);
            return call.Type == type ? (LinqExpression)call : LinqExpression.Convert (call, type);
        }

        static IList<TTo> WidenList<TFrom, TTo> (IList<TFrom> values, Func<TFrom, TTo> widen)
        {
            return values == null ? null : values.Select (widen).ToList ();
        }

        static HashSet<TTo> WidenSet<TFrom, TTo> (HashSet<TFrom> values, Func<TFrom, TTo> widen)
        {
            return values == null ? null : SetOf (values.Select (widen));
        }

        static IDictionary<TKeyTo, TValueTo> WidenDictionary<TKeyFrom, TValueFrom, TKeyTo, TValueTo> (
            IDictionary<TKeyFrom, TValueFrom> values, Func<TKeyFrom, TKeyTo> widenKey,
            Func<TValueFrom, TValueTo> widenValue)
        {
            return values == null ? null : values.ToDictionary (
                x => widenKey (x.Key), x => widenValue (x.Value));
        }

        /// <summary>
        /// Rebuild a tuple with each element widened to the matching element type of
        /// another tuple type, with a null kept. Null if either is not a tuple, or an element
        /// does not widen.
        /// </summary>
        static LinqExpression WidenTuple (LinqExpression tuple, System.Type type, bool cast = false)
        {
            if (!TypeUtils.IsATupleCollectionType (tuple.Type) ||
                !TypeUtils.IsATupleCollectionType (type))
                return null;
            var elementTypes = type.GetGenericArguments ();
            if (tuple.Type.GetGenericArguments ().Length != elementTypes.Length)
                return null;
            var variable = LinqExpression.Variable (tuple.Type);
            var elements = new LinqExpression [elementTypes.Length];
            for (int i = 0; i < elementTypes.Length; i++) {
                elements [i] = Widen (
                    LinqExpression.Property (variable, "Item" + (i + 1)), elementTypes [i], cast);
                if (elements [i] == null)
                    return null;
            }
            var create = typeof (Tuple)
                .GetMethods ()
                .First (m => m.Name == "Create" && m.GetGenericArguments ().Length == elementTypes.Length)
                .MakeGenericMethod (elementTypes);
            var widened = LinqExpression.Call (create, elements);
            return LinqExpression.Block (
                new [] { variable },
                LinqExpression.Assign (variable, tuple),
                LinqExpression.Condition (
                    LinqExpression.Equal (variable, LinqExpression.Constant (null, tuple.Type)),
                    LinqExpression.Constant (null, widened.Type), widened));
        }

        /// <summary>
        /// The variable an expression created with Variable or Parameter stands for.
        /// </summary>
        static ParameterExpression AsVariable (Expression expression, string message)
        {
            var variable = ReferenceEquals (expression, null)
                ? null : expression.internalExpression as ParameterExpression;
            if (variable == null)
                throw new ArgumentException (message);
            return variable;
        }
    }
}
