using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A server side expression.
    /// </summary>
    [KRPCClass (Service = "KRPC")]
    public class Expression
    {
        readonly LinqExpression internalExpression;

        Func<object> evaluator;
        Action runner;
        bool markersChecked;

        internal Expression(LinqExpression expression)
        {
            internalExpression = expression;
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
        /// Reported when a procedure called by a function pauses execution to
        /// resume on a later tick. Resuming would mean evaluating the function
        /// again from the start, repeating everything it already did.
        /// </summary>
        internal const string YieldedMessage =
            "A procedure called by the function paused execution, to resume on a " +
            "later tick. A function is evaluated within a single tick, so a " +
            "procedure that does this cannot be called from one. Use a deferred " +
            "call to start it without waiting for it.";

        /// <summary>
        /// A delegate that evaluates the expression and returns its value.
        /// Compiled on first use and reused afterwards, so that evaluating the same
        /// expression repeatedly pays the cost of compiling it once.
        /// </summary>
        internal Func<object> Evaluator {
            get {
                if (evaluator == null)
                    evaluator = LinqExpression.Lambda<Func<object>> (
                        LinqExpression.Convert (internalExpression, typeof (object))).Compile ();
                return evaluator;
            }
        }

        /// <summary>
        /// A delegate that evaluates an expression that produces no value, for its
        /// effects. Compiled on first use and reused afterwards.
        /// </summary>
        internal Action Runner {
            get {
                if (runner == null)
                    runner = LinqExpression.Lambda<Action> (internalExpression).Compile ();
                return runner;
            }
        }

        /// <summary>
        /// The type of the value the expression evaluates to.
        /// </summary>
        /// <remarks>
        /// Throws if the expression evaluates to a value that cannot be sent to a
        /// client, for example the lazily evaluated collection produced by
        /// <see cref="Select"/> or <see cref="Where"/>. Use <see cref="ToList"/> or
        /// <see cref="ToSet"/> to convert such a collection to a concrete one.
        /// </remarks>
        [KRPCProperty]
        public Type ReturnType {
            get { return new Type (GetValidReturnType ()); }
        }

        /// <summary>
        /// Whether the expression evaluates to a value, rather than being evaluated
        /// only for its effects. An expression with no value has no
        /// <see cref="ReturnType"/>.
        /// </summary>
        /// <remarks>
        /// The value of an expression that evaluates to an empty collection encodes to
        /// an empty sequence of bytes, which is what an expression with no value
        /// produces as well, so this is what tells the two apart.
        /// </remarks>
        [KRPCProperty]
        public bool HasReturnType {
            get { return internalExpression.Type != typeof (void); }
        }

        /// <summary>
        /// The expression's type, checked to be a type that can be sent to a client.
        /// </summary>
        internal System.Type GetValidReturnType ()
        {
            var type = internalExpression.Type;
            if (!TypeUtils.IsAValidType (type))
                throw new InvalidOperationException (
                    "The expression evaluates to a value of type " + type + ", " +
                    "which cannot be sent to a client. If the value is a lazily " +
                    "evaluated collection, use ToList or ToSet to convert it.");
            return type;
        }

        /// <summary>
        /// Throws if the expression contains a break, continue or return marker that was
        /// never bound to an enclosing loop or function.
        /// </summary>
        /// <remarks>
        /// An unbound marker compiles successfully, because it is an ordinary call to a
        /// method that throws. Checking before compiling reports the mistake when the
        /// function is built rather than every time it is evaluated.
        /// </remarks>
        internal void CheckMarkersBound ()
        {
            if (markersChecked)
                return;
            new MarkerChecker ().Visit (internalExpression);
            markersChecked = true;
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
        /// one, which is the only pair of numeric types without one.
        /// </summary>
        static System.Type FindCommonNumericType (System.Type type0, System.Type type1)
        {
            if (type0 == typeof (double) || type1 == typeof (double))
                return typeof (double);
            if (type0 == typeof (float) || type1 == typeof (float))
                return typeof (float);
            if (type0 == typeof (ulong) || type1 == typeof (ulong)) {
                var other = type0 == typeof (ulong) ? type1 : type0;
                return other == typeof (uint) ? typeof (ulong) : null;
            }
            if (type0 == typeof (long) || type1 == typeof (long))
                return typeof (long);
            if (type0 == typeof (uint) || type1 == typeof (uint)) {
                var other = type0 == typeof (uint) ? type1 : type0;
                return other == typeof (int) ? typeof (long) : typeof (uint);
            }
            return typeof (int);
        }

        /// <summary>
        /// Convert the operands of a binary operation to a common numeric type,
        /// when they are numeric operands of differing types.
        /// </summary>
        static void PromoteOperands (ref LinqExpression arg0, ref LinqExpression arg1)
        {
            if (arg0 == null || arg1 == null)
                return;
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

        static Expression NumericBinaryOp (Func<LinqExpression, LinqExpression, LinqExpression> op, Expression arg0, Expression arg1)
        {
            LinqExpression left = arg0;
            LinqExpression right = arg1;
            PromoteOperands (ref left, ref right);
            return new Expression (op (left, right));
        }

        /// <summary>
        /// A constant value of double precision floating point type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantDouble(double value)
        {
            return Constant (typeof (double), value);
        }

        /// <summary>
        /// A constant value of single precision floating point type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantFloat(float value)
        {
            return Constant (typeof (float), value);
        }

        /// <summary>
        /// A constant value of integer type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantInt(int value)
        {
            return Constant (typeof (int), value);
        }

        /// <summary>
        /// A constant value of boolean type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantBool (bool value)
        {
            return Constant (typeof (bool), value);
        }

        /// <summary>
        /// A constant value of string type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantString (string value)
        {
            return Constant (typeof (string), value);
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

        /// <summary>
        /// A constant value of an object type, i.e. an instance of a class defined
        /// by a service. The object is given by its object identifier, the value
        /// used to reference the object over the communication protocol, which
        /// client libraries make available on their remote object wrappers.
        /// </summary>
        /// <param name="value">The object identifier of the object.</param>
        [KRPCMethod]
        public static Expression ConstantObject (ulong value)
        {
            var instance = ObjectStore.Instance.GetInstance (value);
            return new Expression (LinqExpression.Constant (instance, GetClassType (instance)));
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
        /// An RPC call.
        /// The instance the call is made on, and the values of its arguments,
        /// are fixed when the expression is created.
        /// A call to a procedure that does not return a value can be used as a
        /// statement, for example within a <see cref="Block"/>, for its effects.
        /// </summary>
        /// <param name="call"></param>
        [KRPCMethod]
        public static Expression Call(ProcedureCall call)
        {
            return BuildCall (call, null, false);
        }

        /// <summary>
        /// An RPC call, where some or all of the arguments are computed by expressions.
        /// The expressions in <paramref name="args"/> provide the call's arguments,
        /// keyed by the position of the parameter they supply, where position 0 is the
        /// instance the call is made on for class methods and properties. A position
        /// with no expression takes the argument encoded in the call, or the
        /// parameter's default value. This allows, for example, a call to be applied to
        /// each value of a collection, by passing a function parameter as the instance
        /// argument.
        /// </summary>
        /// <param name="call">The RPC to call.</param>
        /// <param name="args">Expressions computing the call's arguments, by position.</param>
        [KRPCMethod]
        public static Expression CallWithArguments (ProcedureCall call, IDictionary<int, Expression> args)
        {
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
            return BuildCall (call, args, false);
        }

        /// <summary>
        /// An RPC call that a function does not wait for, used as a statement.
        /// The call is started where it appears. If the procedure pauses execution,
        /// the server runs the rest of it on later ticks and the function carries
        /// on. Any value the procedure returns is discarded.
        /// </summary>
        /// <remarks>
        /// This is how a function calls a procedure that pauses execution, such as
        /// <c>SpaceCenter.WarpTo</c>. The function reads game state from before the
        /// call completes. A failure after the function has finished is written to
        /// the server's log, and the call is canceled if the client that started it
        /// disconnects.
        /// </remarks>
        /// <param name="call">The RPC to call.</param>
        [KRPCMethod]
        public static Expression DeferredCall (ProcedureCall call)
        {
            return BuildCall (call, null, true);
        }

        /// <summary>
        /// An RPC call that a function does not wait for, where some or all of the
        /// arguments are computed by expressions. Combines
        /// <see cref="DeferredCall"/> and <see cref="CallWithArguments"/>.
        /// </summary>
        /// <param name="call">The RPC to call.</param>
        /// <param name="args">Expressions computing the call's arguments, by position.</param>
        [KRPCMethod]
        public static Expression DeferredCallWithArguments (ProcedureCall call, IDictionary<int, Expression> args)
        {
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
            return BuildCall (call, args, true);
        }

        static Expression BuildCall (ProcedureCall call, IDictionary<int, Expression> args, bool deferred)
        {
            if (ReferenceEquals (call, null))
                throw new ArgumentNullException (nameof (call));
            var services = Services.Instance;
            var procedure = services.GetProcedureSignature(call);

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
                    exprValues [i] = ConvertArgumentExpression (argument, parameter, procedure);
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
            LinqExpression callExpr;
            if (hasInstance) {
                var instanceExpr = exprValues [0] ??
                    LinqExpression.Constant (constValues [0], parameters [0].Spec.Type);
                callExpr = LinqExpression.Call (instanceExpr, method, arguments);
            } else {
                callExpr = LinqExpression.Call (method, arguments);
            }

            var procedureExpr = LinqExpression.Constant (procedure);
            var sceneCheck = LinqExpression.Call (
                typeof (Services).GetMethod (nameof (Services.CheckExpressionGameScene)),
                procedureExpr);

            // The call is started inside the function and its value discarded.
            // Only a pause detaches the rest of it
            if (deferred)
                return new Expression (LinqExpression.Call (
                    typeof (Services).GetMethod (nameof (Services.ExecuteDeferredCall)),
                    procedureExpr,
                    LinqExpression.Lambda<Action> (
                        LinqExpression.Block (typeof (void), sceneCheck, callExpr))));

            if (!procedure.HasReturnType)
                return new Expression (LinqExpression.Block (typeof (void), sceneCheck, callExpr));

            // The declared type of a nullable value-type return is Nullable<T>, which keeps a
            // null representable
            var returnType = procedure.ReturnSpec.DeclaredType;
            if (returnType.IsValueType || procedure.ReturnSpec.Nullable)
                return new Expression (LinqExpression.Block (sceneCheck, callExpr));

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
                returnValue));
        }

        /// <summary>
        /// Convert an argument expression to the parameter's type, allowing
        /// upcasts and implicit numeric conversions.
        /// </summary>
        static LinqExpression ConvertArgumentExpression (Expression expression, Scanner.ParameterSignature parameter, Scanner.ProcedureSignature procedure)
        {
            LinqExpression expr = expression;
            var type = parameter.Spec.Type;
            if (expr.Type == type)
                return expr;
            if (type.IsAssignableFrom (expr.Type) ||
                (IsNumericType (expr.Type) && IsNumericType (type) &&
                 FindCommonNumericType (expr.Type, type) == type))
                return LinqExpression.Convert (expr, type);
            throw new InvalidOperationException (
                "Incorrect expression type for parameter " + parameter.Name +
                " in " + procedure.FullyQualifiedName + ". " +
                "Expected an expression of type " + type + ", got " + expr.Type);
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
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Equal(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.Equal, arg0, arg1);
        }

        /// <summary>
        /// Inequality comparison.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression NotEqual(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.NotEqual, arg0, arg1);
        }

        /// <summary>
        /// Greater than numerical comparison.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression GreaterThan(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.GreaterThan, arg0, arg1);
        }

        /// <summary>
        /// Greater than or equal numerical comparison.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression GreaterThanOrEqual(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.GreaterThanOrEqual, arg0, arg1);
        }

        /// <summary>
        /// Less than numerical comparison.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression LessThan(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.LessThan, arg0, arg1);
        }

        /// <summary>
        /// Less than or equal numerical comparison.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression LessThanOrEqual(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.LessThanOrEqual, arg0, arg1);
        }

        /// <summary>
        /// Boolean and operator. Both operands are evaluated.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression And(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.And(arg0, arg1));
        }

        /// <summary>
        /// Boolean or operator. Both operands are evaluated.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Or(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Or(arg0, arg1));
        }

        /// <summary>
        /// Conditional boolean and operator. The second operand is only evaluated
        /// when the first is true.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression ConditionalAnd (Expression arg0, Expression arg1)
        {
            return new Expression (LinqExpression.AndAlso (arg0, arg1));
        }

        /// <summary>
        /// Conditional boolean or operator. The second operand is only evaluated
        /// when the first is false.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression ConditionalOr (Expression arg0, Expression arg1)
        {
            return new Expression (LinqExpression.OrElse (arg0, arg1));
        }

        /// <summary>
        /// Boolean exclusive-or operator.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression ExclusiveOr(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.ExclusiveOr(arg0, arg1));
        }

        /// <summary>
        /// Boolean negation operator.
        /// </summary>
        /// <param name="arg"></param>
        [KRPCMethod]
        public static Expression Not(Expression arg)
        {
            return new Expression(LinqExpression.Not(arg));
        }

        /// <summary>
        /// A conditional value. Evaluates to the second argument if the condition is
        /// true, and the third argument otherwise.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="condition">The condition. Must evaluate to a boolean value.</param>
        /// <param name="ifTrue">The value when the condition is true.</param>
        /// <param name="ifFalse">The value when the condition is false.</param>
        [KRPCMethod]
        public static Expression Conditional(Expression condition, Expression ifTrue, Expression ifFalse)
        {
            LinqExpression left = ifTrue;
            LinqExpression right = ifFalse;
            PromoteOperands (ref left, ref right);
            return new Expression(LinqExpression.Condition(condition, left, right));
        }

        /// <summary>
        /// Numerical addition.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Add(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.Add, arg0, arg1);
        }

        /// <summary>
        /// Numerical subtraction.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Subtract(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.Subtract, arg0, arg1);
        }

        /// <summary>
        /// Numerical multiplication.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Multiply(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.Multiply, arg0, arg1);
        }

        /// <summary>
        /// Numerical division.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Divide(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.Divide, arg0, arg1);
        }

        /// <summary>
        /// Numerical modulo operator.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns>The remainder of arg0 divided by arg1</returns>
        [KRPCMethod]
        public static Expression Modulo(Expression arg0, Expression arg1)
        {
            return NumericBinaryOp(LinqExpression.Modulo, arg0, arg1);
        }

        /// <summary>
        /// Numerical power operator.
        /// Numeric operands of differing types are converted to a common type.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns>arg0 raised to the power of arg1, at the common type of the operands</returns>
        [KRPCMethod]
        public static Expression Power(Expression arg0, Expression arg1)
        {
            LinqExpression left = arg0;
            LinqExpression right = arg1;
            PromoteOperands (ref left, ref right);
            // The underlying Math.Pow takes and returns a double, so the result is
            // converted back to the type the operands promoted to
            var power = LinqExpression.Power (
                LinqExpression.Convert (left, typeof (double)),
                LinqExpression.Convert (right, typeof (double)));
            return new Expression (LinqExpression.Convert (power, left.Type));
        }

        /// <summary>
        /// Bitwise left shift.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression LeftShift(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.LeftShift(arg0, arg1));
        }

        /// <summary>
        /// Bitwise right shift.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression RightShift(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.RightShift(arg0, arg1));
        }

        /// <summary>
        /// Perform a cast to the given type.
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="type">Type to cast the argument to.</param>
        [KRPCMethod]
        public static Expression Cast(Expression arg, Type type)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            return new Expression(LinqExpression.Convert(arg, type.InternalType));
        }

        /// <summary>
        /// A named parameter, for use within a function.
        /// Bind a value to it by name using <see cref="Invoke"/>, or by using it
        /// as a parameter of a <see cref="Lambda"/>.
        /// </summary>
        /// <returns>A named parameter.</returns>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        [KRPCMethod]
        public static Expression Parameter (string name, Type type)
        {
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            return new Expression (LinqExpression.Parameter (type.InternalType, name));
        }

        /// <summary>
        /// A function.
        /// The body may be a single expression, or a block of statements; a
        /// function whose body does not produce a value performs its statements
        /// for their effects. Return and ReturnNothing statements within the
        /// body end the function's evaluation.
        /// </summary>
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
            var boundBody = BindReturns (body.internalExpression);
            var parameterNodes = new ParameterExpression [parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
                parameterNodes [i] = AsVariable (
                    parameters [i], "Expected a parameter, created with Parameter");
            return new Expression (LinqExpression.Lambda (boundBody, parameterNodes));
        }

        /// <summary>
        /// Replace return markers in a function body with jumps to a label at
        /// the end of the body.
        /// </summary>
        static LinqExpression BindReturns (LinqExpression body)
        {
            if (body.Type == typeof (void)) {
                var target = LinqExpression.Label ();
                var bound = new MarkerRewriter (null, null, target).Visit (body);
                return LinqExpression.Block (bound, LinqExpression.Label (target));
            } else {
                var target = LinqExpression.Label (body.Type);
                var bound = new MarkerRewriter (null, null, target).Visit (body);
                return LinqExpression.Label (target, bound);
            }
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
                throw new ArgumentException (
                    "The function takes " + lambda.Parameters.Count + " arguments, got " +
                    args.Count);
            var funcArgs = new LinqExpression [lambda.Parameters.Count];
            var i = 0;
            foreach (var param in lambda.Parameters) {
                Expression argument;
                if (!args.TryGetValue (param.Name, out argument) || ReferenceEquals (argument, null))
                    throw new ArgumentException (
                        "No argument given for the function's parameter " + param.Name);
                funcArgs [i] = ConvertElement (argument, param.Type);
                i++;
            }
            return new Expression (LinqExpression.Invoke (function, funcArgs));
        }

        /// <summary>
        /// Construct a tuple.
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
            var elementTypes = elements.Select (e => e.Type).ToArray ();
            var method = typeof (Tuple)
                .GetMethods ()
                .First (m => m.Name == "Create" && m.GetGenericArguments ().Length == elements.Count);
            method = method.MakeGenericMethod (elementTypes);
            return new Expression (LinqExpression.Call (method, elements.Select (x => x.internalExpression).ToArray ()));
        }

        /// <summary>
        /// Construct a structure.
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
            var structType = type.InternalType;
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
                var value = ConvertElement (fieldValues [i], field.PropertyType);
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
        /// Construct a list.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="values">The value. Should all be of the same type.</param>
        [KRPCMethod]
        public static Expression CreateList (IList<Expression> values)
        {
            CheckHasValues (values, "list", "CreateEmptyList");
            CheckNoNullElements (values, "values of a list");
            var valueType = CommonType (values, "values of a list");
            var listType = typeof (List<>).MakeGenericType (valueType);
            var ctor = listType.GetConstructor (new [] { typeof (IEnumerable<>).MakeGenericType (valueType) });
            var args = LinqExpression.NewArrayInit (valueType, values.Select (x => x.internalExpression));
            return new Expression (LinqExpression.New (ctor, args));
        }

        /// <summary>
        /// Construct a set.
        /// </summary>
        /// <returns>The set.</returns>
        /// <param name="values">The values. Should all be of the same type.</param>
        [KRPCMethod]
        public static Expression CreateSet (HashSet<Expression> values)
        {
            CheckHasValues (values, "set", "CreateEmptySet");
            CheckNoNullElements (values, "values of a set");
            var valueType = CommonType (values, "values of a set");
            var setType = typeof (HashSet<>).MakeGenericType (valueType);
            var ctor = setType.GetConstructor (new [] { typeof (IEnumerable<>).MakeGenericType (valueType) });
            var args = LinqExpression.NewArrayInit (valueType, values.Select (x => x.internalExpression));
            return new Expression (LinqExpression.New (ctor, args));
        }

        /// <summary>
        /// Construct a dictionary, from a list of corresponding keys and values.
        /// </summary>
        /// <returns>The dictionary.</returns>
        /// <param name="keys">The keys. Should all be of the same type.</param>
        /// <param name="values">The values. Should all be of the same type.</param>
        [KRPCMethod]
        public static Expression CreateDictionary (IList<Expression> keys, IList<Expression> values)
        {
            CheckHasValues (keys, "dictionary", "CreateEmptyDictionary");
            CheckHasValues (values, "dictionary", "CreateEmptyDictionary");
            if (keys.Count != values.Count)
                throw new ArgumentException (
                    "A dictionary needs as many values as keys, got " + keys.Count +
                    " keys and " + values.Count + " values");
            CheckNoNullElements (keys, "keys of a dictionary");
            CheckNoNullElements (values, "values of a dictionary");
            var keyType = CommonType (keys, "keys of a dictionary");
            var valueType = CommonType (values, "values of a dictionary");
            var method = typeof(Expression).GetMethod("CreateDictionaryHelper", BindingFlags.Static | BindingFlags.NonPublic);
            method = method.MakeGenericMethod (keyType, valueType);
            var keysArg = LinqExpression.NewArrayInit (keyType, keys.Select (x => x.internalExpression));
            var valuesArg = LinqExpression.NewArrayInit (valueType, values.Select (x => x.internalExpression));
            return new Expression (LinqExpression.Call (method, keysArg, valuesArg));
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

        /// <summary>
        /// Check that a collection is being constructed from at least one value. The
        /// element type is taken from the values, so an empty collection has to be
        /// constructed by the factory that is given the type instead.
        /// </summary>
        static void CheckHasValues (ICollection<Expression> values, string kind, string emptyFactory)
        {
            if (ReferenceEquals (values, null))
                throw new ArgumentNullException (nameof (values));
            if (values.Count == 0)
                throw new ArgumentException (
                    "Cannot determine the element type of an empty " + kind +
                    ". Use " + emptyFactory + " to construct one.");
        }

        /// <summary>
        /// The type of the values a collection is being constructed from, which must all
        /// be of the same type. A set's values arrive unordered, so which of them names
        /// the element type cannot be left to whichever one comes out first.
        /// </summary>
        static System.Type CommonType (IEnumerable<Expression> values, string what)
        {
            var type = values.First ().Type;
            foreach (var value in values)
                if (value.Type != type)
                    throw new ArgumentException (
                        "The " + what + " must all be of the same type, got " +
                        type + " and " + value.Type);
            return type;
        }

        static Dictionary<Key, Value> CreateDictionaryHelper<Key, Value> (Key[] keys, Value[] values)
        {
            var dictionary = new Dictionary<Key, Value> ();
            if (keys.Length != values.Length)
                throw new InvalidOperationException ("Number of keys and values differ");
            for (int i = 0; i < keys.Length; i++)
                dictionary [keys [i]] = values [i];
            return dictionary;
        }

        /// <summary>
        /// Construct an empty list that values of the given type can be added to.
        /// </summary>
        /// <returns>The empty list.</returns>
        /// <param name="valueType">The type of the values the list holds.</param>
        [KRPCMethod]
        public static Expression CreateEmptyList (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            var listType = typeof (List<>).MakeGenericType (valueType.InternalType);
            return new Expression (LinqExpression.New (listType.GetConstructor (System.Type.EmptyTypes)));
        }

        /// <summary>
        /// Construct an empty set that values of the given type can be added to.
        /// </summary>
        /// <returns>The empty set.</returns>
        /// <param name="valueType">The type of the values the set holds.</param>
        [KRPCMethod]
        public static Expression CreateEmptySet (Type valueType)
        {
            if (ReferenceEquals (valueType, null))
                throw new ArgumentNullException (nameof (valueType));
            var setType = typeof (HashSet<>).MakeGenericType (valueType.InternalType);
            return new Expression (LinqExpression.New (setType.GetConstructor (System.Type.EmptyTypes)));
        }

        /// <summary>
        /// Construct an empty dictionary that entries of the given types can be
        /// added to.
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
            if (!TypeUtils.IsAValidKeyType (keyType.InternalType))
                throw new ArgumentException (
                    keyType.InternalType + " is not a valid dictionary key type");
            var dictionaryType = typeof (Dictionary<,>).MakeGenericType (
                keyType.InternalType, valueType.InternalType);
            return new Expression (LinqExpression.New (dictionaryType.GetConstructor (System.Type.EmptyTypes)));
        }

        // Appending is named apart from Add, which is numerical addition

        /// <summary>
        /// A statement that adds a value to a list or a set. The value goes on the end
        /// of a list, and one a set already holds is not added again.
        /// </summary>
        /// <param name="collection">The list or set to add to.</param>
        /// <param name="value">The value to add.</param>
        [KRPCMethod]
        public static Expression Append (Expression collection, Expression value)
        {
            if (ReferenceEquals (collection, null))
                throw new ArgumentNullException (nameof (collection));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (IsADictionary (collection))
                throw new InvalidOperationException (
                    "An entry is added to a dictionary with Set, which takes its key.");
            var valueType = GetEnumerableValueType (collection);
            if (IsASet (collection)) {
                var setAdd = typeof (ISet<>).MakeGenericType (valueType).GetMethod ("Add");
                // Discard the added/already-present result so this is a statement
                return new Expression (LinqExpression.Block (typeof (void),
                    LinqExpression.Call (
                        collection, setAdd, ConvertElement (value, valueType))));
            }
            CheckIsAList (collection);
            var add = typeof (ICollection<>).MakeGenericType (valueType).GetMethod ("Add");
            return new Expression (LinqExpression.Call (
                collection, add, ConvertElement (value, valueType)));
        }

        /// <summary>
        /// A statement that sets the element at an index of a list, or the value
        /// stored under a key of a dictionary. A key the dictionary does not hold is
        /// added.
        /// </summary>
        /// <param name="collection">The list or dictionary to modify.</param>
        /// <param name="index">The zero indexed position of the element to set,
        /// or the key of the entry to set.</param>
        /// <param name="value">The value to set the element to.</param>
        [KRPCMethod]
        public static Expression Set (Expression collection, Expression index, Expression value)
        {
            if (ReferenceEquals (collection, null))
                throw new ArgumentNullException (nameof (collection));
            if (ReferenceEquals (index, null))
                throw new ArgumentNullException (nameof (index));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (IsADictionary (collection)) {
                var types = DictionaryTypes (collection);
                var entry = typeof (IDictionary<,>).MakeGenericType (types).GetProperty ("Item");
                return new Expression (LinqExpression.Assign (
                    LinqExpression.Property (
                        collection, entry, ConvertElement (index, types [0])),
                    ConvertElement (value, types [1])));
            }
            var valueType = GetEnumerableValueType (collection);
            CheckIsAList (collection);
            CheckIsAnInt (index, nameof (index));
            var item = typeof (IList<>).MakeGenericType (valueType).GetProperty ("Item");
            return new Expression (LinqExpression.Assign (
                LinqExpression.Property (collection, item, index),
                ConvertElement (value, valueType)));
        }

        /// <summary>
        /// Convert a value to the type of the position it is used in: a collection's
        /// element, a structure's field or the variable it is assigned to. A numeric
        /// conversion that widens is implicit; one that narrows requires a cast.
        /// </summary>
        static LinqExpression ConvertElement (Expression value, System.Type type)
        {
            LinqExpression expression = value;
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
        /// Convert a collection to a list.
        /// </summary>
        /// <returns>The collection as a list.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression ToList (Expression arg)
        {
            var valueType = GetEnumerableValueType (arg);
            var toList = typeof (Enumerable).GetMethod ("ToList");
            toList = toList.MakeGenericMethod (valueType);
            return new Expression (LinqExpression.Call (toList, arg));
        }

        /// <summary>
        /// Convert a collection to a set.
        /// </summary>
        /// <returns>The collection as a set.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression ToSet (Expression arg)
        {
            var valueType = GetEnumerableValueType (arg);
            var setType = typeof (HashSet<>).MakeGenericType (valueType);
            var ctor = setType.GetConstructor (new [] { typeof (IEnumerable<>).MakeGenericType (valueType) });
            return new Expression (LinqExpression.New (ctor, arg));
        }

        /// <summary>
        /// Access an element in a tuple, list or dictionary.
        /// </summary>
        /// <remarks>
        /// The elements of a tuple differ in type, so the index into one must be a
        /// constant integer rather than a computed value.
        /// </remarks>
        /// <returns>The element.</returns>
        /// <param name="arg">The tuple, list or dictionary.</param>
        /// <param name="index">The index of the element to access.
        /// A zero indexed integer for a tuple or list, or a key for a dictionary.</param>
        [KRPCMethod]
        public static Expression Get (Expression arg, Expression index)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (index, null))
                throw new ArgumentNullException (nameof (index));
            CheckIsNotAString (arg);
            var argType = arg.Type;
            if (argType.Name.StartsWith("Tuple`", StringComparison.Ordinal)) {
                // The elements of a tuple differ in type, so which one is being read has
                // to be known when the tree is built rather than when it is evaluated
                var constant = index.internalExpression as ConstantExpression;
                if (constant == null || !(constant.Value is int))
                    throw new ArgumentException (
                        "The index into a tuple must be a constant integer");
                var tupleIndex = (int)constant.Value;
                var property = argType.GetProperty ("Item" + (tupleIndex + 1));
                if (property == null)
                    throw new ArgumentOutOfRangeException (nameof (index));
                return new Expression (LinqExpression.Property (arg, property));
            }
            var method = argType.GetMethod ("get_Item");
            if (method == null)
                throw new InvalidOperationException (
                    argType + " does not have elements that can be accessed by index");
            // A list is indexed by position, and a dictionary by a key of its own type
            var indexType = method.GetParameters () [0].ParameterType;
            if (indexType == typeof (int))
                CheckIsAnInt (index, nameof (index));
            return new Expression (
                LinqExpression.Call (arg, method, ConvertElement (index, indexType)));
        }

        /// <summary>
        /// Access a field of a structure.
        /// </summary>
        /// <returns>The value of the field.</returns>
        /// <param name="arg">The structure.</param>
        /// <param name="name">The name of the field to access.</param>
        [KRPCMethod]
        public static Expression GetField (Expression arg, string name)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var argType = arg.Type;
            if (!TypeUtils.IsAStructType (argType))
                throw new ArgumentException (argType + " is not a structure type");
            var field = TypeUtils.GetStructFields (argType)
                .FirstOrDefault (property => property.Name == name);
            if (field == null)
                throw new ArgumentException (
                    argType + " does not have a field called \"" + name + "\"");
            return new Expression (LinqExpression.Property (arg, field));
        }

        /// <summary>
        /// Number of elements in a collection.
        /// </summary>
        /// <returns>The number of elements in the collection.</returns>
        /// <param name="arg">The list, set or dictionary.</param>
        [KRPCMethod]
        public static Expression Count (Expression arg)
        {
            CheckIsEnumerable (arg);
            var count = GetCountProperty (arg.Type);
            if (count == null)
                throw new InvalidOperationException (
                    "A lazily evaluated sequence does not have a count. " +
                    "Convert it to a list or a set first.");
            return new Expression (LinqExpression.Property (arg, count));
        }

        /// <summary>
        /// Sum all elements of a collection.
        /// </summary>
        /// <returns>The sum of the elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Sum (Expression arg)
        {
            CheckIsEnumerable (arg);
            var sum = typeof (Enumerable).GetMethod ("Sum", new [] { arg.Type });
            if (sum == null)
                throw new InvalidOperationException (
                    "Sum is not defined over a collection of type " + arg.Type);
            return new Expression (LinqExpression.Call (sum, arg));
        }

        /// <summary>
        /// Maximum of all elements in a collection.
        /// </summary>
        /// <returns>The maximum elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Max (Expression arg)
        {
            CheckIsEnumerable (arg);
            var max = typeof (Enumerable).GetMethod ("Max", new [] { arg.Type });
            if (max == null)
                throw new InvalidOperationException (
                    "Max is not defined over a collection of type " + arg.Type);
            return new Expression (LinqExpression.Call (max, arg));
        }

        /// <summary>
        /// Minimum of all elements in a collection.
        /// </summary>
        /// <returns>The minimum elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Min (Expression arg)
        {
            CheckIsEnumerable (arg);
            var min = typeof (Enumerable).GetMethod ("Min", new [] { arg.Type });
            if (min == null)
                throw new InvalidOperationException (
                    "Min is not defined over a collection of type " + arg.Type);
            return new Expression (LinqExpression.Call (min, arg));
        }

        /// <summary>
        /// Average of all elements in a collection.
        /// </summary>
        /// <returns>The average of the elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Average (Expression arg)
        {
            CheckIsEnumerable (arg);
            var average = typeof (Enumerable).GetMethod ("Average", new [] { arg.Type });
            if (average == null)
                throw new InvalidOperationException (
                    "Average is not defined over a collection of type " + arg.Type);
            return new Expression (LinqExpression.Call (average, arg));
        }

        /// <summary>
        /// Run a function on every element in the collection.
        /// The result is a lazily evaluated sequence; use <see cref="ToList"/> or
        /// <see cref="ToSet"/> to convert it to a concrete collection.
        /// </summary>
        /// <returns>The collection of function results.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The function, taking an element of the collection.</param>
        [KRPCMethod]
        public static Expression Select (Expression arg, Expression func)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var sourceType = GetEnumerableValueType (arg);
            var resultType = GetFunctionResultType (func, nameof (func), 1);
            CheckIsFunction (func, sourceType, resultType);
            var select = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "Select" &&
                         x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2);
            select = select.MakeGenericMethod (sourceType, resultType);
            return new Expression (LinqExpression.Call (select, arg, func));
        }

        /// <summary>
        /// Keep the elements of a collection for which a boolean predicate function
        /// returns true.
        /// The result is a lazily evaluated sequence; use <see cref="ToList"/> or
        /// <see cref="ToSet"/> to convert it to a concrete collection.
        /// </summary>
        /// <returns>The filtered collection.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The predicate function, taking an element of the collection.</param>
        [KRPCMethod]
        public static Expression Where (Expression arg, Expression func)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (func, sourceType, typeof(bool));
            var where = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "Where" &&
                         x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2);
            where = where.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (where, arg, func));
        }

        /// <summary>
        /// Skip the first count values of a collection.
        /// The result is a lazily evaluated sequence; use <see cref="ToList"/> or
        /// <see cref="ToSet"/> to convert it to a concrete collection.
        /// </summary>
        /// <returns>The collection without its first count values.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="count">The number of values to skip.</param>
        [KRPCMethod]
        public static Expression Skip (Expression arg, Expression count)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (count, null))
                throw new ArgumentNullException (nameof (count));
            var sourceType = GetEnumerableValueType (arg);
            var skip = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "Skip" && x.GetParameters ().Length == 2);
            skip = skip.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (skip, arg, count));
        }

        /// <summary>
        /// Take only the first count values of a collection.
        /// The result is a lazily evaluated sequence; use <see cref="ToList"/> or
        /// <see cref="ToSet"/> to convert it to a concrete collection.
        /// </summary>
        /// <returns>The first count values of the collection.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="count">The number of values to take.</param>
        [KRPCMethod]
        public static Expression Take (Expression arg, Expression count)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (count, null))
                throw new ArgumentNullException (nameof (count));
            var sourceType = GetEnumerableValueType (arg);
            var take = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "Take" && x.GetParameters () [1].ParameterType == typeof (int));
            take = take.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (take, arg, count));
        }

        /// <summary>
        /// Run a function returning a collection on every element in the
        /// collection, and flatten the results into a single collection.
        /// The result is a lazily evaluated sequence; use <see cref="ToList"/> or
        /// <see cref="ToSet"/> to convert it to a concrete collection.
        /// </summary>
        /// <returns>The flattened collection of function results.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The function, taking an element of the collection
        /// and returning a collection.</param>
        [KRPCMethod]
        public static Expression SelectMany (Expression arg, Expression func)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var sourceType = GetEnumerableValueType (arg);
            var funcResultType = GetFunctionResultType (func, nameof (func), 1);
            CheckIsFunction (func, sourceType, funcResultType);
            if (!typeof (IEnumerable).IsAssignableFrom (funcResultType) ||
                !funcResultType.IsGenericType)
                throw new InvalidOperationException ("The function must return a collection");
            var resultType = funcResultType.GetGenericArguments () [0];
            var selectMany = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "SelectMany" &&
                         x.GetParameters ().Length == 2 &&
                         x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2);
            selectMany = selectMany.MakeGenericMethod (sourceType, resultType);
            return new Expression (LinqExpression.Call (selectMany, arg, func));
        }

        /// <summary>
        /// Build a dictionary from a collection, by running a function computing
        /// the key and a function computing the value on every element.
        /// </summary>
        /// <returns>The dictionary.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="keyFunc">The function computing an element's key.</param>
        /// <param name="valueFunc">The function computing an element's value.</param>
        [KRPCMethod]
        public static Expression BuildDictionary (Expression arg, Expression keyFunc, Expression valueFunc)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var sourceType = GetEnumerableValueType (arg);
            var keyType = GetFunctionResultType (keyFunc, nameof (keyFunc), 1);
            var valueType = GetFunctionResultType (valueFunc, nameof (valueFunc), 1);
            CheckIsFunction (keyFunc, sourceType, keyType);
            CheckIsFunction (valueFunc, sourceType, valueType);
            var toDictionary = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "ToDictionary" &&
                         x.GetParameters ().Length == 3 &&
                         x.GetParameters () [2].ParameterType.Name.StartsWith ("Func`", StringComparison.Ordinal));
            toDictionary = toDictionary.MakeGenericMethod (sourceType, keyType, valueType);
            return new Expression (LinqExpression.Call (toDictionary, arg, keyFunc, valueFunc));
        }

        internal static string ConvertToStringHelper (object value)
        {
            return Convert.ToString (value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert a value to its string representation.
        /// </summary>
        /// <param name="arg">The value to convert.</param>
        [KRPCMethod]
        public static Expression ConvertToString (Expression arg)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var method = typeof (Expression).GetMethod (
                nameof (ConvertToStringHelper), BindingFlags.Static | BindingFlags.NonPublic);
            return new Expression (LinqExpression.Call (
                method, LinqExpression.Convert (arg, typeof (object))));
        }

        /// <summary>
        /// Concatenate strings.
        /// Use <see cref="ConvertToString"/> to convert other values to strings.
        /// </summary>
        /// <returns>The concatenated string.</returns>
        /// <param name="args">The strings to concatenate, in order.</param>
        [KRPCMethod]
        public static Expression StringConcat (IList<Expression> args)
        {
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
            CheckNoNullElements (args, "values to concatenate");
            foreach (var arg in args)
                if (arg.Type != typeof (string))
                    throw new InvalidOperationException (
                        "All values to concatenate must be strings; " +
                        "use ConvertToString to convert them");
            var concat = typeof (string).GetMethod ("Concat", new [] { typeof (string []) });
            return new Expression (LinqExpression.Call (
                concat,
                LinqExpression.NewArrayInit (
                    typeof (string), args.Select (x => x.internalExpression))));
        }

        // A string is its own kind of value on the server, so the string operations
        // are named apart from the collection ones they share a concept with.
        // Comparison and case conversion ignore the ambient culture, so a function
        // produces the same result whatever locale the game is running in

        /// <summary>
        /// The number of characters in a string.
        /// </summary>
        /// <returns>The length of the string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringLength (Expression arg)
        {
            CheckIsAString (arg, nameof (arg));
            return new Expression (LinqExpression.Property (
                arg, typeof (string).GetProperty ("Length")));
        }

        /// <summary>
        /// The character at the given position in a string, as a string of length one.
        /// </summary>
        /// <returns>The character at the given position.</returns>
        /// <param name="arg">The string.</param>
        /// <param name="index">The position of the character, counting from zero.</param>
        [KRPCMethod]
        public static Expression StringGet (Expression arg, Expression index)
        {
            CheckIsAString (arg, nameof (arg));
            CheckIsAnInt (index, nameof (index));
            return new Expression (LinqExpression.Call (
                arg, typeof (string).GetMethod ("Substring", new [] { typeof (int), typeof (int) }),
                index, LinqExpression.Constant (1)));
        }

        /// <summary>
        /// Part of a string.
        /// </summary>
        /// <returns>The substring.</returns>
        /// <param name="arg">The string.</param>
        /// <param name="start">The position to start at, counting from zero.</param>
        /// <param name="length">The number of characters to take.</param>
        [KRPCMethod]
        public static Expression StringSubstring (Expression arg, Expression start, Expression length)
        {
            CheckIsAString (arg, nameof (arg));
            CheckIsAnInt (start, nameof (start));
            CheckIsAnInt (length, nameof (length));
            return new Expression (LinqExpression.Call (
                arg, typeof (string).GetMethod ("Substring", new [] { typeof (int), typeof (int) }),
                start, length));
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
            return new Expression (OrdinalIndexOf (arg, value));
        }

        /// <summary>
        /// Determine whether one string occurs within another.
        /// </summary>
        /// <returns>Whether the value occurs in the string.</returns>
        /// <param name="arg">The string to search.</param>
        /// <param name="value">The string to search for.</param>
        [KRPCMethod]
        public static Expression StringContains (Expression arg, Expression value)
        {
            return new Expression (LinqExpression.GreaterThanOrEqual (
                OrdinalIndexOf (arg, value), LinqExpression.Constant (0)));
        }

        /// <summary>
        /// Determine whether a string starts with another.
        /// </summary>
        /// <returns>Whether the string starts with the value.</returns>
        /// <param name="arg">The string to test.</param>
        /// <param name="value">The string to test for.</param>
        [KRPCMethod]
        public static Expression StringStartsWith (Expression arg, Expression value)
        {
            return new Expression (OrdinalStringCall ("StartsWith", arg, value));
        }

        /// <summary>
        /// Determine whether a string ends with another.
        /// </summary>
        /// <returns>Whether the string ends with the value.</returns>
        /// <param name="arg">The string to test.</param>
        /// <param name="value">The string to test for.</param>
        [KRPCMethod]
        public static Expression StringEndsWith (Expression arg, Expression value)
        {
            return new Expression (OrdinalStringCall ("EndsWith", arg, value));
        }

        /// <summary>
        /// Convert a string to upper case, independently of the game's language.
        /// </summary>
        /// <returns>The string in upper case.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringToUpper (Expression arg)
        {
            CheckIsAString (arg, nameof (arg));
            return new Expression (LinqExpression.Call (
                arg, typeof (string).GetMethod ("ToUpperInvariant", System.Type.EmptyTypes)));
        }

        /// <summary>
        /// Convert a string to lower case, independently of the game's language.
        /// </summary>
        /// <returns>The string in lower case.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringToLower (Expression arg)
        {
            CheckIsAString (arg, nameof (arg));
            return new Expression (LinqExpression.Call (
                arg, typeof (string).GetMethod ("ToLowerInvariant", System.Type.EmptyTypes)));
        }

        /// <summary>
        /// Remove white space from both ends of a string.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringTrim (Expression arg)
        {
            return new Expression (TrimCall ("Trim", arg));
        }

        /// <summary>
        /// Remove white space from the start of a string.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringTrimStart (Expression arg)
        {
            return new Expression (TrimCall ("TrimStart", arg));
        }

        /// <summary>
        /// Remove white space from the end of a string.
        /// </summary>
        /// <returns>The trimmed string.</returns>
        /// <param name="arg">The string.</param>
        [KRPCMethod]
        public static Expression StringTrimEnd (Expression arg)
        {
            return new Expression (TrimCall ("TrimEnd", arg));
        }

        /// <summary>
        /// Replace every occurrence of one string within another.
        /// </summary>
        /// <returns>The string with the replacements made.</returns>
        /// <param name="arg">The string to search.</param>
        /// <param name="oldValue">The string to replace.</param>
        /// <param name="newValue">The string to replace it with.</param>
        [KRPCMethod]
        public static Expression StringReplace (Expression arg, Expression oldValue, Expression newValue)
        {
            CheckIsAString (arg, nameof (arg));
            CheckIsAString (oldValue, nameof (oldValue));
            CheckIsAString (newValue, nameof (newValue));
            return new Expression (LinqExpression.Call (
                arg, typeof (string).GetMethod ("Replace", new [] { typeof (string), typeof (string) }),
                oldValue, newValue));
        }

        internal static IList<string> StringSplitHelper (string value, string separator)
        {
            return value.Split (new [] { separator }, StringSplitOptions.None);
        }

        /// <summary>
        /// Split a string into the parts separated by another string.
        /// </summary>
        /// <returns>The parts of the string.</returns>
        /// <param name="arg">The string to split.</param>
        /// <param name="separator">The string that separates the parts.</param>
        [KRPCMethod]
        public static Expression StringSplit (Expression arg, Expression separator)
        {
            CheckIsAString (arg, nameof (arg));
            CheckIsAString (separator, nameof (separator));
            var method = typeof (Expression).GetMethod (
                nameof (StringSplitHelper), BindingFlags.Static | BindingFlags.NonPublic);
            return new Expression (LinqExpression.Call (method, arg, separator));
        }

        /// <summary>
        /// Join strings together, separated by another string.
        /// </summary>
        /// <returns>The joined string.</returns>
        /// <param name="separator">The string to put between the values.</param>
        /// <param name="values">The strings to join.</param>
        [KRPCMethod]
        public static Expression StringJoin (Expression separator, Expression values)
        {
            CheckIsAString (separator, nameof (separator));
            if (ReferenceEquals (values, null))
                throw new ArgumentNullException (nameof (values));
            if (!typeof (IEnumerable<string>).IsAssignableFrom (values.Type))
                throw new InvalidOperationException ("Expected a collection of strings to join");
            var join = typeof (string).GetMethod (
                "Join", new [] { typeof (string), typeof (IEnumerable<string>) });
            return new Expression (LinqExpression.Call (join, separator, values));
        }

        // Removing from a collection and emptying one. They pair with Add and Set

        /// <summary>
        /// Remove a value from a list or a set, or the entry stored under a key from
        /// a dictionary. The first occurrence is removed from a list.
        /// </summary>
        /// <returns>Whether the collection held the value.</returns>
        /// <param name="collection">The list, set or dictionary.</param>
        /// <param name="value">The value to remove, or the key of the entry to
        /// remove.</param>
        [KRPCMethod]
        public static Expression Remove (Expression collection, Expression value)
        {
            if (ReferenceEquals (collection, null))
                throw new ArgumentNullException (nameof (collection));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (IsADictionary (collection)) {
                var types = DictionaryTypes (collection);
                var removeEntry = typeof (IDictionary<,>).MakeGenericType (types).GetMethod ("Remove");
                return new Expression (LinqExpression.Call (
                    collection, removeEntry, ConvertElement (value, types [0])));
            }
            var valueType = GetEnumerableValueType (collection);
            CheckIsAListOrASet (collection);
            var remove = typeof (ICollection<>).MakeGenericType (valueType).GetMethod ("Remove");
            return new Expression (LinqExpression.Call (
                collection, remove, ConvertElement (value, valueType)));
        }

        /// <summary>
        /// Remove the value at the given position from a list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The position of the value, counting from zero.</param>
        [KRPCMethod]
        public static Expression RemoveAt (Expression list, Expression index)
        {
            var valueType = GetEnumerableValueType (list);
            CheckIsAList (list);
            CheckIsAnInt (index, nameof (index));
            var removeAt = typeof (IList<>).MakeGenericType (valueType).GetMethod ("RemoveAt");
            return new Expression (LinqExpression.Call (list, removeAt, index));
        }

        /// <summary>
        /// Remove every value from a list or a set, or every entry from a dictionary.
        /// </summary>
        /// <param name="collection">The list, set or dictionary.</param>
        [KRPCMethod]
        public static Expression Clear (Expression collection)
        {
            if (ReferenceEquals (collection, null))
                throw new ArgumentNullException (nameof (collection));
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
        /// The keys of a dictionary, as a list. A dictionary cannot be iterated over
        /// directly; iterate over its keys or its values instead.
        /// </summary>
        /// <returns>The keys of the dictionary.</returns>
        /// <param name="dictionary">The dictionary.</param>
        [KRPCMethod]
        public static Expression DictionaryKeys (Expression dictionary)
        {
            return new Expression (DictionaryPartCall (
                dictionary, nameof (DictionaryKeysHelper)));
        }

        /// <summary>
        /// The values of a dictionary, as a list.
        /// </summary>
        /// <returns>The values of the dictionary.</returns>
        /// <param name="dictionary">The dictionary.</param>
        [KRPCMethod]
        public static Expression DictionaryValues (Expression dictionary)
        {
            return new Expression (DictionaryPartCall (
                dictionary, nameof (DictionaryValuesHelper)));
        }

        // Selecting a single value out of a collection

        /// <summary>
        /// The first value of a collection. Fails if the collection is empty.
        /// </summary>
        /// <returns>The first value.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression First (Expression arg)
        {
            return new Expression (EnumerableCall ("First", 1, arg));
        }

        /// <summary>
        /// The last value of a collection. Fails if the collection is empty.
        /// </summary>
        /// <returns>The last value.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression Last (Expression arg)
        {
            return new Expression (EnumerableCall ("Last", 1, arg));
        }

        /// <summary>
        /// The value at the given position in a collection.
        /// </summary>
        /// <returns>The value at the given position.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="index">The position of the value, counting from zero.</param>
        [KRPCMethod]
        public static Expression ElementAt (Expression arg, Expression index)
        {
            CheckIsAnInt (index, nameof (index));
            var sourceType = GetEnumerableValueType (arg);
            // Newer frameworks also offer an overload taking a System.Index
            var elementAt = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == "ElementAt" && x.GetParameters ().Length == 2 &&
                x.GetParameters () [1].ParameterType == typeof (int));
            return new Expression (LinqExpression.Call (
                elementAt.MakeGenericMethod (sourceType), arg, index));
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
        /// The value of a collection whose key is the smallest or the largest,
        /// selected in a single pass.
        /// </summary>
        static TSource ByKeyHelper<TSource, TKey> (
            IEnumerable<TSource> source, Func<TSource, TKey> selector, int wanted)
        {
            var comparer = Comparer<TKey>.Default;
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
        /// The value of a collection for which the given function produces the
        /// smallest value. Fails if the collection is empty.
        /// </summary>
        /// <returns>The value with the smallest key.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="key">A function producing the key to compare values by.</param>
        [KRPCMethod]
        public static Expression MinBy (Expression arg, Expression key)
        {
            return new Expression (ByKeyCall (nameof (MinByHelper), arg, key));
        }

        /// <summary>
        /// The value of a collection for which the given function produces the
        /// largest value. Fails if the collection is empty.
        /// </summary>
        /// <returns>The value with the largest key.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="key">A function producing the key to compare values by.</param>
        [KRPCMethod]
        public static Expression MaxBy (Expression arg, Expression key)
        {
            return new Expression (ByKeyCall (nameof (MaxByHelper), arg, key));
        }

        // Reshaping a collection. Each produces a lazily evaluated sequence unless
        // stated otherwise. Use ToList or ToSet to convert one to a concrete
        // collection before it is returned to a client

        /// <summary>
        /// The values of a collection with duplicates removed, keeping the first
        /// occurrence of each.
        /// </summary>
        /// <returns>The distinct values of the collection.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression Distinct (Expression arg)
        {
            return new Expression (EnumerableCall ("Distinct", 1, arg));
        }

        /// <summary>
        /// The values of a collection in the opposite order.
        /// </summary>
        /// <returns>The reversed collection.</returns>
        /// <param name="arg">The collection.</param>
        [KRPCMethod]
        public static Expression Reverse (Expression arg)
        {
            return new Expression (EnumerableCall ("Reverse", 1, arg));
        }

        /// <summary>
        /// The values in either of two collections, with duplicates removed.
        /// </summary>
        /// <returns>The union of the two collections.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Union (Expression arg1, Expression arg2)
        {
            return new Expression (SetOperationCall ("Union", arg1, arg2));
        }

        /// <summary>
        /// The values in both of two collections, with duplicates removed.
        /// </summary>
        /// <returns>The intersection of the two collections.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Intersect (Expression arg1, Expression arg2)
        {
            return new Expression (SetOperationCall ("Intersect", arg1, arg2));
        }

        /// <summary>
        /// The values of the first collection that are not in the second, with
        /// duplicates removed.
        /// </summary>
        /// <returns>The difference between the two collections.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        [KRPCMethod]
        public static Expression Except (Expression arg1, Expression arg2)
        {
            return new Expression (SetOperationCall ("Except", arg1, arg2));
        }

        /// <summary>
        /// Combine two collections by applying a function to their values in pairs.
        /// The result is as long as the shorter of the two.
        /// </summary>
        /// <returns>The combined collection.</returns>
        /// <param name="arg1">The first collection.</param>
        /// <param name="arg2">The second collection.</param>
        /// <param name="func">A function combining a value from each collection.</param>
        [KRPCMethod]
        public static Expression Zip (Expression arg1, Expression arg2, Expression func)
        {
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
            return new Expression (LinqExpression.Call (
                zip.MakeGenericMethod (sourceType1, sourceType2, resultType),
                arg1, arg2, func));
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
        /// Group the values of a collection by the key the given function produces
        /// for each of them.
        /// </summary>
        /// <returns>A dictionary of each key to the values that produced it.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="key">A function producing the key to group values by.</param>
        [KRPCMethod]
        public static Expression GroupBy (Expression arg, Expression key)
        {
            var call = (MethodCallExpression)ByKeyCall (nameof (GroupByHelper), arg, key);
            var keyType = call.Method.GetGenericArguments () [1];
            if (!TypeUtils.IsAValidKeyType (keyType))
                throw new InvalidOperationException (
                    keyType + " is not a valid dictionary key type, so it cannot be grouped by");
            return new Expression (call);
        }

        static System.Type[] DictionaryTypes (Expression dictionary)
        {
            if (ReferenceEquals (dictionary, null))
                throw new ArgumentNullException (nameof (dictionary));
            var types = dictionary.Type.GetGenericArguments ();
            if (types.Length != 2)
                throw new InvalidOperationException ("Expected a dictionary");
            return types;
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
                x => x.Name == name && x.GetParameters ().Length == parameters);
            return LinqExpression.Call (method.MakeGenericMethod (sourceType), arg);
        }

        /// <summary>
        /// A call of an Enumerable method combining two collections of the same type.
        /// </summary>
        static LinqExpression SetOperationCall (string name, Expression arg1, Expression arg2)
        {
            var sourceType1 = GetEnumerableValueType (arg1);
            var sourceType2 = GetEnumerableValueType (arg2);
            if (sourceType1 != sourceType2)
                throw new InvalidOperationException (
                    "Cannot combine collections with different value types");
            var method = typeof (Enumerable).GetMethods ().Single (
                x => x.Name == name && x.GetParameters ().Length == 2);
            return LinqExpression.Call (
                method.MakeGenericMethod (sourceType1), arg1, arg2);
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
            var method = typeof (Expression)
                .GetMethod (helper, BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (sourceType, keyType);
            return LinqExpression.Call (method, arg, key);
        }


        static void CheckIsAString (Expression expression, string name)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (name);
            if (expression.Type != typeof (string))
                throw new InvalidOperationException (
                    "Expected a string for " + name + "; " +
                    "use ConvertToString to convert a value to one");
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
        /// A call of a string method comparing with another string, done by ordinal
        /// value so that the result does not depend on the game's language.
        /// </summary>
        static LinqExpression OrdinalStringCall (string name, Expression arg, Expression value)
        {
            CheckIsAString (arg, nameof (arg));
            CheckIsAString (value, nameof (value));
            var method = typeof (string).GetMethod (
                name, new [] { typeof (string), typeof (StringComparison) });
            return LinqExpression.Call (
                arg, method, value, LinqExpression.Constant (StringComparison.Ordinal));
        }

        static LinqExpression OrdinalIndexOf (Expression arg, Expression value)
        {
            return OrdinalStringCall ("IndexOf", arg, value);
        }

        /// <summary>
        /// A call of one of the trim methods. Each takes the characters to trim, and
        /// trims white space when given none.
        /// </summary>
        static LinqExpression TrimCall (string name, Expression arg)
        {
            CheckIsAString (arg, nameof (arg));
            var method = typeof (string).GetMethod (name, new [] { typeof (char []) });
            return LinqExpression.Call (
                arg, method, LinqExpression.Constant (new char [0]));
        }

        /// <summary>
        /// Determine if a collection contains a value.
        /// </summary>
        /// <returns>Whether the collection contains a value.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="value">The value to look for.</param>
        [KRPCMethod]
        public static Expression Contains (Expression arg, Expression value)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var sourceType = GetEnumerableValueType (arg);
            var contains = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Contains" && x.GetParameters ().Length == 2);
            contains = contains.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (
                contains, arg, ConvertElement (value, sourceType)));
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <returns>The accumulated value.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="func">The accumulator function.</param>
        [KRPCMethod]
        public static Expression Aggregate (Expression arg, Expression func)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (func, sourceType, sourceType, sourceType);
            var aggregate = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Aggregate" && x.GetParameters ().Length == 2);
            aggregate = aggregate.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (aggregate, arg, func));
        }

        /// <summary>
        /// Applies an accumulator function over a sequence, with a given seed.
        /// </summary>
        /// <returns>The accumulated value.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="seed">The seed value.</param>
        /// <param name="func">The accumulator function.</param>
        [KRPCMethod]
        public static Expression AggregateWithSeed (Expression arg, Expression seed, Expression func)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (seed, null))
                throw new ArgumentNullException (nameof (seed));
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            var accumulateType = seed.Type;
            CheckIsFunction (func, accumulateType, sourceType, accumulateType);
            var aggregate = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Aggregate" && x.GetParameters ().Length == 3);
            aggregate = aggregate.MakeGenericMethod (sourceType, accumulateType);
            return new Expression (LinqExpression.Call (aggregate, arg, seed, func));
        }

        /// <summary>
        /// Concatenate two sequences.
        /// </summary>
        /// <returns>The first sequence followed by the second sequence.</returns>
        /// <param name="arg1">The first sequence.</param>
        /// <param name="arg2">The second sequence.</param>
        [KRPCMethod]
        public static Expression Concat (Expression arg1, Expression arg2)
        {
            var sourceType1 = GetEnumerableValueType (arg1);
            var sourceType2 = GetEnumerableValueType (arg2);
            if (!sourceType1.IsAssignableFrom (sourceType2) || !sourceType2.IsAssignableFrom (sourceType1))
                throw new InvalidOperationException ("Cannot concatenate sequences with different value types");
            var concat = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Concat");
            concat = concat.MakeGenericMethod (sourceType1);
            return new Expression (LinqExpression.Call (concat, arg1, arg2));
        }

        /// <summary>
        /// Order a collection using a key function.
        /// </summary>
        /// <returns>The ordered collection.</returns>
        /// <param name="arg">The collection to order.</param>
        /// <param name="key">A function that takes a value from the collection and generates a key to sort on.</param>
        [KRPCMethod]
        public static Expression OrderBy (Expression arg, Expression key)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var sourceType = GetEnumerableValueType (arg);
            var keyType = GetFunctionResultType (key, nameof (key), 1);
            CheckIsFunction (key, sourceType, keyType);
            var orderBy = typeof (Enumerable).GetMethods ().Single (x => x.Name == "OrderBy" && x.GetParameters ().Length == 2);
            orderBy = orderBy.MakeGenericMethod (sourceType, keyType);
            return new Expression (LinqExpression.Call (orderBy, arg, key));
        }

        /// <summary>
        /// Determine whether all items in a collection satisfy a boolean predicate.
        /// </summary>
        /// <returns>Whether all items satisfy the predicate.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="predicate">The predicate function.</param>
        [KRPCMethod]
        public static Expression All (Expression arg, Expression predicate)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (predicate, null))
                throw new ArgumentNullException (nameof (predicate));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (predicate, sourceType, typeof (bool));
            var all = typeof (Enumerable).GetMethods ().Single (x => x.Name == "All");
            all = all.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (all, arg, predicate));
        }

        /// <summary>
        /// Determine whether any item in a collection satisfies a boolean predicate.
        /// </summary>
        /// <returns>Whether any item satisfies the predicate.</returns>
        /// <param name="arg">The collection.</param>
        /// <param name="predicate">The predicate function.</param>
        [KRPCMethod]
        public static Expression Any (Expression arg, Expression predicate)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (predicate, null))
                throw new ArgumentNullException (nameof (predicate));
            var sourceType = GetEnumerableValueType (arg);
            CheckIsFunction (predicate, sourceType, typeof (bool));
            var any = typeof (Enumerable).GetMethods ().Single (x => x.Name == "Any" && x.GetParameters ().Length == 2);
            any = any.MakeGenericMethod (sourceType);
            return new Expression (LinqExpression.Call (any, arg, predicate));
        }

        /// <summary>
        /// A local variable, for use within a block.
        /// Declare it in the enclosing block's variable list, and set its value
        /// using <see cref="Assign"/>.
        /// </summary>
        /// <returns>A local variable.</returns>
        /// <param name="name">The name of the variable.</param>
        /// <param name="type">The type of the variable.</param>
        [KRPCMethod]
        public static Expression Variable (string name, Type type)
        {
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            return new Expression (LinqExpression.Variable (type.InternalType, name));
        }

        /// <summary>
        /// Assign a value to a local variable or function parameter.
        /// The value's type must be assignable to the variable's type. A numeric
        /// value of a different type is widened, and narrowing it requires a cast.
        /// </summary>
        /// <param name="variable">The variable to assign to.</param>
        /// <param name="value">The value to assign.</param>
        [KRPCMethod]
        public static Expression Assign (Expression variable, Expression value)
        {
            if (ReferenceEquals (variable, null))
                throw new ArgumentNullException (nameof (variable));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (!(variable.internalExpression is ParameterExpression))
                throw new ArgumentException ("The assignment target must be a variable or parameter");
            return new Expression (LinqExpression.Assign (
                variable, ConvertElement (value, variable.Type)));
        }

        /// <summary>
        /// A block of statements, evaluated in order. The value of the block is
        /// the value of its last statement.
        /// </summary>
        /// <param name="statements">The statements.</param>
        [KRPCMethod]
        public static Expression Block (IList<Expression> statements)
        {
            CheckStatements (statements);
            return new Expression (LinqExpression.Block (
                statements.Select (x => x.internalExpression)));
        }

        /// <summary>
        /// A block of statements with local variables, evaluated in order.
        /// The value of the block is the value of its last statement. The
        /// variables, created with <see cref="Variable"/>, are in scope for
        /// the statements of the block, including within nested functions.
        /// </summary>
        /// <param name="variables">The local variables of the block.</param>
        /// <param name="statements">The statements.</param>
        [KRPCMethod]
        public static Expression BlockWithVariables (IList<Expression> variables, IList<Expression> statements)
        {
            if (ReferenceEquals (variables, null))
                throw new ArgumentNullException (nameof (variables));
            CheckStatements (statements);
            var variableNodes = variables.Select (x => AsVariable (
                x, "Expected a variable, created with Variable")).ToArray ();
            return new Expression (LinqExpression.Block (
                variableNodes, statements.Select (x => x.internalExpression)));
        }

        static void CheckStatements (IList<Expression> statements)
        {
            if (ReferenceEquals (statements, null))
                throw new ArgumentNullException (nameof (statements));
            if (statements.Count == 0)
                throw new ArgumentException ("A block must contain at least one statement");
            CheckNoNullElements (statements, "statements of a block");
        }

        /// <summary>
        /// An if statement. Evaluates the body when the condition is true.
        /// Use <see cref="Conditional"/> for an if-then-else that produces a value.
        /// </summary>
        /// <param name="condition">The condition. Must evaluate to a boolean value.</param>
        /// <param name="body">The statement to evaluate when the condition is true.</param>
        [KRPCMethod]
        public static Expression IfThen (Expression condition, Expression body)
        {
            return new Expression (LinqExpression.IfThen (condition, AsStatement (body)));
        }

        /// <summary>
        /// An if-else statement. Evaluates the first body when the condition is
        /// true, and the second body otherwise.
        /// Use <see cref="Conditional"/> for an if-then-else that produces a value.
        /// </summary>
        /// <param name="condition">The condition. Must evaluate to a boolean value.</param>
        /// <param name="body">The statement to evaluate when the condition is true.</param>
        /// <param name="elseBody">The statement to evaluate when the condition is false.</param>
        [KRPCMethod]
        public static Expression IfThenElse (Expression condition, Expression body, Expression elseBody)
        {
            return new Expression (LinqExpression.IfThenElse (
                condition, AsStatement (body), AsStatement (elseBody)));
        }

        /// <summary>
        /// Discard a statement's value, so that differently typed statements can
        /// be used as the branches of an if statement.
        /// </summary>
        static LinqExpression AsStatement (Expression statement)
        {
            if (ReferenceEquals (statement, null))
                throw new ArgumentNullException (nameof (statement));
            var expression = statement.internalExpression;
            if (expression.Type == typeof (void))
                return expression;
            return LinqExpression.Block (typeof (void), expression);
        }

        // Markers for break, continue and return statements. They are replaced with
        // jumps when the enclosing loop or function is created. Expressions are built
        // from the innermost node outwards, so a marker binds to the nearest
        // enclosing construct
        internal static void BreakMarker ()
        {
            throw new InvalidOperationException ("break used outside of a loop");
        }

        internal static void ContinueMarker ()
        {
            throw new InvalidOperationException ("continue used outside of a loop");
        }

        internal static T ReturnMarker<T> (T value)
        {
            throw new InvalidOperationException ("return used outside of a function");
        }

        internal static void ReturnVoidMarker ()
        {
            throw new InvalidOperationException ("return used outside of a function");
        }

        /// <summary>
        /// A break statement. Ends the evaluation of the enclosing loop.
        /// </summary>
        [KRPCMethod]
        public static Expression Break ()
        {
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (BreakMarker), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        /// <summary>
        /// A continue statement. Skips to the next iteration of the enclosing loop.
        /// </summary>
        [KRPCMethod]
        public static Expression Continue ()
        {
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (ContinueMarker), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        /// <summary>
        /// A return statement. Ends the evaluation of the enclosing function,
        /// which must be created with <see cref="Lambda"/>, with the given
        /// value as its result.
        /// </summary>
        /// <param name="value">The value to return.</param>
        [KRPCMethod]
        public static Expression Return (Expression value)
        {
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (value.Type == typeof (void))
                throw new InvalidOperationException (
                    "A return statement must be given a value. Use ReturnNothing to " +
                    "return from a function that produces none.");
            var method = typeof (Expression)
                .GetMethod (nameof (ReturnMarker), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (value.Type);
            return new Expression (LinqExpression.Call (method, value.internalExpression));
        }

        /// <summary>
        /// A return statement with no value. Ends the evaluation of the
        /// enclosing function, which must be created with <see cref="Lambda"/>
        /// and must not produce a value.
        /// </summary>
        [KRPCMethod]
        public static Expression ReturnNothing ()
        {
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (ReturnVoidMarker), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        /// <summary>
        /// Replaces break, continue and return markers with jumps to the given
        /// labels. Does not descend into nested functions, whose markers bind to
        /// their own function and loops, and leaves already-bound jumps intact.
        /// </summary>
        sealed class MarkerRewriter : ExpressionVisitor
        {
            readonly LabelTarget breakTarget;
            readonly LabelTarget continueTarget;
            readonly LabelTarget returnTarget;

            public MarkerRewriter (LabelTarget breakLabel, LabelTarget continueLabel, LabelTarget returnLabel)
            {
                breakTarget = breakLabel;
                continueTarget = continueLabel;
                returnTarget = returnLabel;
            }

            protected override LinqExpression VisitLambda<T> (Expression<T> node)
            {
                return node;
            }

            protected override LinqExpression VisitMethodCall (MethodCallExpression node)
            {
                var method = node.Method;
                if (method.DeclaringType == typeof (Expression)) {
                    if (breakTarget != null && method.Name == nameof (BreakMarker))
                        return LinqExpression.Break (breakTarget);
                    if (continueTarget != null && method.Name == nameof (ContinueMarker))
                        return LinqExpression.Continue (continueTarget);
                    if (returnTarget != null && method.IsGenericMethod && method.Name == nameof (ReturnMarker)) {
                        if (returnTarget.Type != method.GetGenericArguments () [0])
                            throw new InvalidOperationException (
                                "return value of type " + method.GetGenericArguments () [0] +
                                " does not match the function's result type " + returnTarget.Type);
                        return LinqExpression.Return (returnTarget, Visit (node.Arguments [0]), typeof (void));
                    }
                    if (returnTarget != null && method.Name == nameof (ReturnVoidMarker)) {
                        if (returnTarget.Type != typeof (void))
                            throw new InvalidOperationException (
                                "return must have a value in a function that produces a value");
                        return LinqExpression.Return (returnTarget, typeof (void));
                    }
                }
                return base.VisitMethodCall (node);
            }
        }

        /// <summary>
        /// Finds break, continue and return markers that no enclosing loop or function
        /// bound to a jump, and reports them with the same message the marker itself
        /// would have thrown when evaluated.
        /// </summary>
        sealed class MarkerChecker : ExpressionVisitor
        {
            protected override LinqExpression VisitMethodCall (MethodCallExpression node)
            {
                var method = node.Method;
                if (method.DeclaringType == typeof (Expression)) {
                    if (method.Name == nameof (BreakMarker))
                        throw new InvalidOperationException ("break used outside of a loop");
                    if (method.Name == nameof (ContinueMarker))
                        throw new InvalidOperationException ("continue used outside of a loop");
                    if (method.Name == nameof (ReturnMarker) || method.Name == nameof (ReturnVoidMarker))
                        throw new InvalidOperationException ("return used outside of a function");
                }
                return base.VisitMethodCall (node);
            }
        }

        /// <summary>
        /// A while loop. Evaluates the body repeatedly, for as long as the
        /// condition evaluates to true. Break and Continue statements within
        /// the body apply to this loop.
        /// </summary>
        /// <param name="condition">The condition. Must evaluate to a boolean value.</param>
        /// <param name="body">The statement to evaluate on each iteration.</param>
        [KRPCMethod]
        public static Expression While (Expression condition, Expression body)
        {
            if (ReferenceEquals (condition, null))
                throw new ArgumentNullException (nameof (condition));
            if (condition.Type != typeof (bool))
                throw new ArgumentException ("The loop condition must evaluate to a boolean value");
            var breakTarget = LinqExpression.Label ();
            var continueTarget = LinqExpression.Label ();
            var boundBody = new MarkerRewriter (breakTarget, continueTarget, null)
                .Visit (AsStatement (body));
            return new Expression (LinqExpression.Loop (
                LinqExpression.IfThenElse (
                    condition, boundBody, LinqExpression.Break (breakTarget)),
                breakTarget, continueTarget));
        }

        /// <summary>
        /// A loop over the values of a collection. Evaluates the body once per
        /// value, with the variable set to the value. Break and Continue
        /// statements within the body apply to this loop.
        /// </summary>
        /// <param name="variable">The loop variable, created with <see cref="Variable"/>.
        /// Must also be declared in an enclosing block.</param>
        /// <param name="collection">The collection to iterate over.</param>
        /// <param name="body">The statement to evaluate on each iteration.</param>
        [KRPCMethod]
        public static Expression ForEach (Expression variable, Expression collection, Expression body)
        {
            if (ReferenceEquals (variable, null))
                throw new ArgumentNullException (nameof (variable));
            if (ReferenceEquals (collection, null))
                throw new ArgumentNullException (nameof (collection));
            if (!(variable.internalExpression is ParameterExpression))
                throw new ArgumentException ("The loop variable must be a variable or parameter");
            var valueType = GetEnumerableValueType (collection);
            if (!variable.Type.IsAssignableFrom (valueType))
                throw new ArgumentException (
                    "The loop variable type " + variable.Type +
                    " does not match the collection's value type " + valueType);
            var enumeratorType = typeof (IEnumerator<>).MakeGenericType (valueType);
            var enumerator = LinqExpression.Variable (enumeratorType, "enumerator");
            var getEnumerator = typeof (IEnumerable<>).MakeGenericType (valueType).GetMethod ("GetEnumerator");
            var moveNext = typeof (IEnumerator).GetMethod ("MoveNext");
            var current = enumeratorType.GetProperty ("Current");
            var breakTarget = LinqExpression.Label ();
            var continueTarget = LinqExpression.Label ();
            var boundBody = new MarkerRewriter (breakTarget, continueTarget, null)
                .Visit (AsStatement (body));
            var loopBody = LinqExpression.Block (
                LinqExpression.Assign (variable, LinqExpression.Property (enumerator, current)),
                boundBody);
            var loop = LinqExpression.Loop (
                LinqExpression.IfThenElse (
                    LinqExpression.Call (enumerator, moveNext),
                    loopBody,
                    LinqExpression.Break (breakTarget)),
                breakTarget, continueTarget);
            var dispose = LinqExpression.Call (
                enumerator, typeof (IDisposable).GetMethod ("Dispose"));
            // The enumerator is created outside the try, so the finally only runs
            // once there is one to dispose of
            return new Expression (LinqExpression.Block (
                new [] { enumerator },
                LinqExpression.Assign (
                    enumerator, LinqExpression.Call (collection, getEnumerator)),
                LinqExpression.TryFinally (loop, dispose)));
        }


        /// <summary>
        /// A throw statement. Raises the named exception, which the client receives
        /// as an error from the function, or as an error on the stream or event
        /// evaluating it.
        /// </summary>
        /// <param name="service">The name of the service the exception is defined in.</param>
        /// <param name="name">The name of the exception.</param>
        /// <param name="message">The message to raise the exception with.</param>
        [KRPCMethod]
        public static Expression Throw (string service, string name, Expression message)
        {
            if (ReferenceEquals (message, null))
                throw new ArgumentNullException (nameof (message));
            if (message.Type != typeof (string))
                throw new InvalidOperationException (
                    "The message of an exception must be a string; " +
                    "use ConvertToString to convert a value to one");
            var type = Services.Instance.GetExceptionTypes (service, name) [0];
            var constructor = type.GetConstructor (new [] { typeof (string) });
            if (constructor == null)
                throw new InvalidOperationException (
                    "Exception \"" + name + "\" in service \"" + service + "\" " +
                    "cannot be raised, as it cannot be constructed from a message");
            return new Expression (LinqExpression.Throw (
                LinqExpression.New (constructor, message.internalExpression)));
        }

        /// <summary>
        /// A try-catch statement. Evaluates the body, and evaluates the handler
        /// instead if the body raises the named exception.
        /// </summary>
        /// <remarks>
        /// Only an exception a service declares can be named, which is the same set
        /// <see cref="Throw"/> can raise. Use <see cref="TryCatchAll"/> to handle any
        /// exception, including the ones an expression itself produces.
        /// </remarks>
        /// <param name="body">The statement to evaluate.</param>
        /// <param name="service">The name of the service the exception is defined in.</param>
        /// <param name="name">The name of the exception.</param>
        /// <param name="message">
        /// A string variable, created with <see cref="Variable"/>, that the caught
        /// exception's message is assigned to before the handler is evaluated. May be
        /// null, when the handler does not use the message.
        /// </param>
        /// <param name="handler">The statement to evaluate when the exception is caught.</param>
        [KRPCMethod]
        public static Expression TryCatch (Expression body, string service, string name, [KRPCNullable] Expression message, Expression handler)
        {
            var types = Services.Instance.GetExceptionTypes (service, name);
            // A service throws the CLR exception types, which are mapped onto the
            // kRPC type as the exception leaves. Catching by the kRPC name therefore
            // catches every type the client would see under it, and only those
            return new Expression (LinqExpression.TryCatch (
                AsStatement (body),
                types.Select (x => BuildCatchBlock (x, types [0], message, handler)).ToArray ()));
        }

        /// <summary>
        /// A try-catch statement that handles any exception. Evaluates the body, and
        /// evaluates the handler instead if the body raises an exception.
        /// </summary>
        /// <param name="body">The statement to evaluate.</param>
        /// <param name="message">
        /// A string variable, created with <see cref="Variable"/>, that the caught
        /// exception's message is assigned to before the handler is evaluated. May be
        /// null, when the handler does not use the message.
        /// </param>
        /// <param name="handler">The statement to evaluate when an exception is caught.</param>
        [KRPCMethod]
        public static Expression TryCatchAll (Expression body, [KRPCNullable] Expression message, Expression handler)
        {
            // A procedure that pauses execution unwinds by throwing YieldException.
            // This handler runs ahead of the catch-all and rethrows it, so the stream
            // or event evaluating the expression still reports the pause
            var yielded = LinqExpression.Catch (
                LinqExpression.Parameter (typeof (YieldException), "yielded"),
                LinqExpression.Rethrow (typeof (void)));
            return new Expression (LinqExpression.TryCatch (
                AsStatement (body),
                yielded,
                BuildCatchBlock (typeof (System.Exception), null, message, handler)));
        }

        /// <summary>
        /// A try-finally statement. Evaluates the body, and evaluates the finalizer
        /// afterwards whether the body completed or raised an exception.
        /// </summary>
        /// <param name="body">The statement to evaluate.</param>
        /// <param name="finalizer">The statement to evaluate afterwards.</param>
        [KRPCMethod]
        public static Expression TryFinally (Expression body, Expression finalizer)
        {
            return new Expression (LinqExpression.TryFinally (
                AsStatement (body), AsStatement (finalizer)));
        }

        /// <summary>
        /// A catch block for the given exception type, assigning the caught
        /// exception's message to the given variable before evaluating the handler.
        /// The exception itself is never exposed, which keeps exceptions out of the
        /// value algebra entirely.
        /// </summary>
        /// <param name="type">The CLR exception type to catch.</param>
        /// <param name="name">The kRPC exception type the catch names, or null to
        /// handle every exception. A catch for a CLR type also catches its subclasses,
        /// which a client sees under their own names, so those are rethrown.</param>
        /// <param name="message">A string variable to assign the message to, or null.</param>
        /// <param name="handler">The statement to evaluate.</param>
        static CatchBlock BuildCatchBlock (System.Type type, System.Type name, Expression message, Expression handler)
        {
            var body = AsStatement (handler);
            if (ReferenceEquals (message, null) && name == null)
                return LinqExpression.Catch (type, body);
            if (!ReferenceEquals (message, null) &&
                (!(message.internalExpression is ParameterExpression) ||
                 message.Type != typeof (string)))
                throw new ArgumentException (
                    "The message of a catch must be a string variable");
            var caught = LinqExpression.Parameter (type, "caught");
            var statements = new List<LinqExpression> ();
            if (name != null)
                statements.Add (LinqExpression.IfThen (
                    LinqExpression.Not (LinqExpression.Call (
                        typeof (Services).GetMethod (nameof (Services.ExpressionExceptionIsNamed)),
                        caught, LinqExpression.Constant (name, typeof (System.Type)))),
                    LinqExpression.Rethrow (typeof (void))));
            if (!ReferenceEquals (message, null))
                statements.Add (LinqExpression.Assign (
                    message, LinqExpression.Property (
                        caught, typeof (System.Exception).GetProperty ("Message"))));
            statements.Add (body);
            return LinqExpression.Catch (
                caught, LinqExpression.Block (typeof (void), statements));
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

        /// <summary>
        /// A string satisfies IEnumerable but is not one of the collection types the
        /// algebra works over, and the collection operations fail inside themselves
        /// when given one. Reject it where it is passed instead.
        /// </summary>
        static void CheckIsNotAString (Expression expression)
        {
            if (expression.Type == typeof (string))
                throw new InvalidOperationException (
                    "A string is not a collection. Use the string operations instead.");
        }

        /// <summary>
        /// A bytes value satisfies IEnumerable and has no type argument to read a
        /// value type from. The algebra works over it as a single value, so reject
        /// it where a collection is passed.
        /// </summary>
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

        /// <summary>
        /// A dictionary enumerates as key-value pairs, which the algebra has no type for,
        /// so a collection operation given one would work over its keys. Reject it where
        /// it is passed, and name the operations that produce a list from it.
        /// </summary>
        static void CheckIsNotADictionary (Expression expression)
        {
            if (IsADictionary (expression))
                throw new InvalidOperationException (
                    "A dictionary cannot be used as a collection of values. Use " +
                    "DictionaryKeys or DictionaryValues to obtain a list of them.");
        }

        // Which kind of collection an expression produces. A list and a set are both
        // ICollection, so the operations that only make sense for one of them ask
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

        static bool IsADictionary (Expression expression)
        {
            return global::KRPC.Utils.Reflection.IsGenericType (
                expression.Type, typeof (IDictionary<,>));
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

        /// <summary>
        /// The type of the value a function of one or two arguments produces, which
        /// is the last of its type arguments. The arity is checked before reading it,
        /// so a value that is not such a function is reported rather than indexed into.
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
