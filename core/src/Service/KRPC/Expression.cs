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
        /// Reported when a procedure called by an expression pauses execution to
        /// resume on a later tick. The expression's evaluation is abandoned by the
        /// unwind, so the only way to make progress would be to evaluate it again
        /// from the start, which repeats everything it already did.
        /// </summary>
        internal const string YieldedMessage =
            "A procedure called by the expression paused execution, to resume on a " +
            "later tick. An expression is evaluated within a single tick, so a " +
            "procedure that does this cannot be called from one.";

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
            if (type0 == typeof (double) || type1 == typeof (double))
                return typeof (double);
            if (type0 == typeof (float) || type1 == typeof (float))
                return typeof (float);
            if (type0 == typeof (ulong) || type1 == typeof (ulong)) {
                var other = type0 == typeof (ulong) ? type1 : type0;
                if (other == typeof (uint))
                    return typeof (ulong);
                throw new InvalidOperationException (
                    "No implicit conversion between " + type0 + " and " + type1 + ". " +
                    "Use a cast to convert one of the operands.");
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
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// A constant value of single precision floating point type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantFloat(float value)
        {
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// A constant value of integer type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantInt(int value)
        {
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// A constant value of boolean type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantBool (bool value)
        {
            return new Expression (LinqExpression.Constant (value));
        }

        /// <summary>
        /// A constant value of string type.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantString (string value)
        {
            return new Expression (LinqExpression.Constant (value));
        }

        /// <summary>
        /// A constant value of an object type, i.e. an instance of a class defined
        /// by a service. The object is given by its object identifier — the value
        /// used to reference the object over the communication protocol, which
        /// client libraries make available on their remote object wrappers.
        /// </summary>
        /// <param name="value">The object identifier of the object.</param>
        [KRPCMethod]
        public static Expression ConstantObject (ulong value)
        {
            if (value == 0)
                throw new ArgumentNullException (nameof (value));
            var instance = ObjectStore.Instance.GetInstance (value);
            if (instance == null)
                throw new ArgumentException ("No object with identifier " + value);
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
            return BuildCall (call, null);
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
            return BuildCall (call, args);
        }

        static Expression BuildCall (ProcedureCall call, IDictionary<int, Expression> args)
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
            // constant value known when the expression is created.
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

            var hasInstance = procedure.Handler.HasInstance;
            LinqExpression instanceExpr;
            if (!hasInstance)
                instanceExpr = LinqExpression.Constant (null, typeof (object));
            else if (exprValues [0] != null)
                instanceExpr = LinqExpression.Convert (exprValues [0], typeof (object));
            else
                instanceExpr = LinqExpression.Constant (constValues [0], typeof (object));

            var firstArgument = hasInstance ? 1 : 0;
            var numArguments = numParameters - firstArgument;
            LinqExpression argumentsExpr;
            if (exprValues.Skip (firstArgument).All (x => x == null)) {
                // All argument values are known now, so embed the argument array as a
                // constant to avoid constructing it on every evaluation
                argumentsExpr = LinqExpression.Constant (
                    constValues.Skip (firstArgument).ToArray ());
            } else {
                var elements = new LinqExpression [numArguments];
                for (int i = 0; i < numArguments; i++) {
                    var expr = exprValues [firstArgument + i];
                    elements [i] = expr != null
                        ? (LinqExpression)LinqExpression.Convert (expr, typeof (object))
                        : LinqExpression.Constant (constValues [firstArgument + i], typeof (object));
                }
                argumentsExpr = LinqExpression.NewArrayInit (typeof (object), elements);
            }

            var servicesExpr = LinqExpression.Constant(services);
            var executeCallMethod = typeof(Services).GetMethod ("ExecuteExpressionCall");
            var procedureExpr = LinqExpression.Constant(procedure);
            var result = LinqExpression.Call(
                servicesExpr, executeCallMethod,
                new[] { procedureExpr, instanceExpr, argumentsExpr });
            if (!procedure.HasReturnType)
                return new Expression (LinqExpression.Block (typeof (void), result));
            var returnType = procedure.ReturnType;
            // A nullable value-type return may be null, so evaluate it as Nullable<T> so the
            // null is representable rather than faulting the conversion.
            if (procedure.ReturnIsNullable && returnType.IsValueType)
                returnType = typeof(System.Nullable<>).MakeGenericType(returnType);
            return new Expression(LinqExpression.Convert(result, returnType));
        }

        /// <summary>
        /// Convert an argument expression to the parameter's type, allowing
        /// upcasts and implicit numeric conversions.
        /// </summary>
        static LinqExpression ConvertArgumentExpression (Expression expression, Scanner.ParameterSignature parameter, Scanner.ProcedureSignature procedure)
        {
            LinqExpression expr = expression;
            var type = parameter.Type;
            if (expr.Type == type)
                return expr;
            if (type.IsAssignableFrom (expr.Type) ||
                (IsNumericType (expr.Type) && IsNumericType (type) && CommonNumericType (expr.Type, type) == type))
                return LinqExpression.Convert (expr, type);
            throw new InvalidOperationException (
                "Incorrect expression type for parameter " + parameter.Name +
                " in " + procedure.FullyQualifiedName + ". " +
                "Expected an expression of type " + type + ", got " + expr.Type);
        }

        static void CheckArgumentValue (Scanner.ProcedureSignature procedure, Scanner.ParameterSignature parameter, object value)
        {
            var type = parameter.Type;
            if (value != null && !type.IsInstanceOfType (value))
                throw new ArgumentException (
                    "Incorrect argument type for parameter " + parameter.Name +
                    " in " + procedure.FullyQualifiedName + ". " +
                    "Expected an argument of type " + type + ", got " + value.GetType ());
            if (value == null && !parameter.Nullable)
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
        /// Boolean and operator.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression And(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.And(arg0, arg1));
        }

        /// <summary>
        /// Boolean or operator.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Or(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Or(arg0, arg1));
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
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns>arg0 raised to the power of arg1, with type of arg0</returns>
        [KRPCMethod]
        public static Expression Power(Expression arg0, Expression arg1)
        {
            var arg0b = LinqExpression.Convert (arg0, typeof(double));
            var arg1b = LinqExpression.Convert (arg1, typeof (double));
            return new Expression (LinqExpression.Convert (LinqExpression.Power (arg0b, arg1b), arg0.Type));
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
        /// A named parameter of type double.
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
        /// for their effects. <see cref="Return"/> and <see cref="ReturnNothing"/>
        /// statements within the body end the function's evaluation.
        /// </summary>
        /// <returns>A function.</returns>
        /// <param name="parameters">The parameters of the function.</param>
        /// <param name="body">The body of the function.</param>
        [KRPCMethod]
        public static Expression Function (IList<Expression> parameters, Expression body)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            var boundBody = BindReturns (body.internalExpression);
            return new Expression (LinqExpression.Lambda (boundBody, parameters.Select(x => (ParameterExpression)(x.internalExpression)).ToArray()));
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
            var funcArgs = new LinqExpression [args.Count];
            var i = 0;
            foreach (var param in ((LambdaExpression)function.internalExpression).Parameters) {
                funcArgs [i] = args [param.Name].internalExpression;
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
            var elementTypes = elements.Select (e => e.Type).ToArray ();
            var method = typeof (Tuple)
                .GetMethods ()
                .Single (m => m.Name == "Create" && m.GetGenericArguments ().Length == elements.Count);
            if (method == null)
                throw new ArgumentException ("Tuple constructor not found for these element types");
            method = method.MakeGenericMethod (elementTypes);
            return new Expression (LinqExpression.Call (method, elements.Select (x => x.internalExpression).ToArray ()));
        }

        /// <summary>
        /// Construct a list.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="values">The value. Should all be of the same type.</param>
        [KRPCMethod]
        public static Expression CreateList (IList<Expression> values)
        {
            var valueType = values.First ().Type;
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
            var valueType = values.First ().Type;
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
            var keyType = keys.First ().Type;
            var valueType = values.First ().Type;
            var method = typeof(Expression).GetMethod("CreateDictionaryHelper", BindingFlags.Static | BindingFlags.NonPublic);
            method = method.MakeGenericMethod (keyType, valueType);
            var keysArg = LinqExpression.NewArrayInit (keyType, keys.Select (x => x.internalExpression));
            var valuesArg = LinqExpression.NewArrayInit (valueType, values.Select (x => x.internalExpression));
            return new Expression (LinqExpression.Call (method, keysArg, valuesArg));
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

        /// <summary>
        /// A statement that adds a value to the end of a list.
        /// </summary>
        /// <param name="list">The list to add to.</param>
        /// <param name="value">The value to add.</param>
        [KRPCMethod]
        public static Expression ListAdd (Expression list, Expression value)
        {
            if (ReferenceEquals (list, null))
                throw new ArgumentNullException (nameof (list));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var valueType = GetEnumerableValueType (list);
            var add = typeof (ICollection<>).MakeGenericType (valueType).GetMethod ("Add");
            return new Expression (LinqExpression.Call (
                list, add, ConvertElement (value, valueType)));
        }

        /// <summary>
        /// A statement that sets the element at an index of a list.
        /// </summary>
        /// <param name="list">The list to modify.</param>
        /// <param name="index">The zero indexed position of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        [KRPCMethod]
        public static Expression ListSet (Expression list, Expression index, Expression value)
        {
            if (ReferenceEquals (list, null))
                throw new ArgumentNullException (nameof (list));
            if (ReferenceEquals (index, null))
                throw new ArgumentNullException (nameof (index));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var valueType = GetEnumerableValueType (list);
            var item = typeof (IList<>).MakeGenericType (valueType).GetProperty ("Item");
            return new Expression (LinqExpression.Assign (
                LinqExpression.Property (list, item, index),
                ConvertElement (value, valueType)));
        }

        /// <summary>
        /// A statement that adds a value to a set. Has no effect if the set
        /// already contains the value.
        /// </summary>
        /// <param name="set">The set to add to.</param>
        /// <param name="value">The value to add.</param>
        [KRPCMethod]
        public static Expression SetAdd (Expression set, Expression value)
        {
            if (ReferenceEquals (set, null))
                throw new ArgumentNullException (nameof (set));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var valueType = GetEnumerableValueType (set);
            var add = typeof (HashSet<>).MakeGenericType (valueType).GetMethod ("Add");
            // Discard the added/already-present result so this is a statement
            return new Expression (LinqExpression.Block (typeof (void),
                LinqExpression.Call (set, add, ConvertElement (value, valueType))));
        }

        /// <summary>
        /// A statement that sets the value for a key of a dictionary, adding an
        /// entry if the key is not present.
        /// </summary>
        /// <param name="dictionary">The dictionary to modify.</param>
        /// <param name="key">The key of the entry to set.</param>
        /// <param name="value">The value to set the entry to.</param>
        [KRPCMethod]
        public static Expression DictionarySet (Expression dictionary, Expression key, Expression value)
        {
            if (ReferenceEquals (dictionary, null))
                throw new ArgumentNullException (nameof (dictionary));
            if (ReferenceEquals (key, null))
                throw new ArgumentNullException (nameof (key));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var types = dictionary.Type.GetGenericArguments ();
            if (types.Length != 2)
                throw new InvalidOperationException ("Expected a dictionary");
            var item = typeof (IDictionary<,>).MakeGenericType (types).GetProperty ("Item");
            return new Expression (LinqExpression.Assign (
                LinqExpression.Property (
                    dictionary, item, ConvertElement (key, types [0])),
                ConvertElement (value, types [1])));
        }

        /// <summary>
        /// Convert a value to a collection's element type, allowing implicit
        /// numeric conversions.
        /// </summary>
        static LinqExpression ConvertElement (Expression value, System.Type type)
        {
            LinqExpression expression = value;
            if (expression.Type != type &&
                IsNumericType (expression.Type) && IsNumericType (type))
                return LinqExpression.Convert (expression, type);
            return expression;
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
        /// <returns>The element.</returns>
        /// <param name="arg">The tuple, list or dictionary.</param>
        /// <param name="index">The index of the element to access.
        /// A zero indexed integer for a tuple or list, or a key for a dictionary.</param>
        [KRPCMethod]
        public static Expression Get (Expression arg, Expression index)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            var argType = arg.Type;
            if (argType.Name.StartsWith("Tuple`", StringComparison.Ordinal)) {
                var tupleIndex = LinqExpression.Lambda<Func<int>> (index).Compile () ();
                var property = argType.GetProperty ("Item" + (tupleIndex + 1));
                if (property == null)
                    throw new ArgumentOutOfRangeException (nameof (index));
                return new Expression (LinqExpression.Property (arg, property));
            }
            var method = argType.GetMethod ("get_Item");
            return new Expression (LinqExpression.Call (arg, method, index));
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
            var count = arg.Type.GetProperty ("Count");
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
            return new Expression (LinqExpression.Call (min, arg));
        }

        /// <summary>
        /// Minimum of all elements in a collection.
        /// </summary>
        /// <returns>The minimum elements in the collection.</returns>
        /// <param name="arg">The list or set.</param>
        [KRPCMethod]
        public static Expression Average (Expression arg)
        {
            CheckIsEnumerable (arg);
            var average = typeof (Enumerable).GetMethod ("Average", new [] { arg.Type });
            return new Expression (LinqExpression.Call (average, arg));
        }

        /// <summary>
        /// Run a function on every element in the collection.
        /// </summary>
        /// <returns>The modified collection.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The function.</param>
        [KRPCMethod]
        public static Expression Select (Expression arg, Expression func)
        {
            if (ReferenceEquals (arg, null))
                throw new ArgumentNullException (nameof (arg));
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            var resultType = func.Type.GetGenericArguments () [1];
            CheckIsFunction (func, sourceType, resultType);
            var select = typeof (Enumerable)
                .GetMethods ()
                .Single (x => x.Name == "Select" &&
                         x.GetParameters () [1].ParameterType.GetGenericArguments ().Length == 2);
            select = select.MakeGenericMethod (sourceType, resultType);
            return new Expression (LinqExpression.Call (select, arg, func));
        }

        /// <summary>
        /// Run a function on every element in the collection.
        /// </summary>
        /// <returns>The modified collection.</returns>
        /// <param name="arg">The list or set.</param>
        /// <param name="func">The function.</param>
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
            if (ReferenceEquals (func, null))
                throw new ArgumentNullException (nameof (func));
            var sourceType = GetEnumerableValueType (arg);
            var funcResultType = func.Type.GetGenericArguments () [1];
            if (!typeof (IEnumerable).IsAssignableFrom (funcResultType))
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
            if (ReferenceEquals (keyFunc, null))
                throw new ArgumentNullException (nameof (keyFunc));
            if (ReferenceEquals (valueFunc, null))
                throw new ArgumentNullException (nameof (valueFunc));
            var sourceType = GetEnumerableValueType (arg);
            var keyType = keyFunc.Type.GetGenericArguments () [1];
            var valueType = valueFunc.Type.GetGenericArguments () [1];
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
        public static Expression ConcatStrings (IList<Expression> args)
        {
            if (ReferenceEquals (args, null))
                throw new ArgumentNullException (nameof (args));
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
            return new Expression (LinqExpression.Call (contains, arg, value));
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
            if (ReferenceEquals (key, null))
                throw new ArgumentNullException (nameof (key));
            var sourceType = GetEnumerableValueType (arg);
            var keyType = key.Type.GetGenericArguments () [1];
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
        /// The value's type must be assignable to the variable's type; numeric
        /// values of a different type are converted.
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
            LinqExpression converted = value;
            var targetType = variable.Type;
            if (converted.Type != targetType &&
                IsNumericType (converted.Type) && IsNumericType (targetType))
                converted = LinqExpression.Convert (converted, targetType);
            return new Expression (LinqExpression.Assign (variable, converted));
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
            return new Expression (LinqExpression.Block (
                variables.Select (x => (ParameterExpression)x.internalExpression),
                statements.Select (x => x.internalExpression)));
        }

        static void CheckStatements (IList<Expression> statements)
        {
            if (ReferenceEquals (statements, null))
                throw new ArgumentNullException (nameof (statements));
            if (statements.Count == 0)
                throw new ArgumentException ("A block must contain at least one statement");
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

        // Markers for break, continue and return statements. They are replaced
        // with jumps when the enclosing loop or function is created; binding to
        // the nearest enclosing construct follows from expressions being built
        // from the innermost node outwards.
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
        /// which must be created with <see cref="Function"/>, with the given
        /// value as its result.
        /// </summary>
        /// <param name="value">The value to return.</param>
        [KRPCMethod]
        public static Expression Return (Expression value)
        {
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var method = typeof (Expression)
                .GetMethod (nameof (ReturnMarker), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (value.Type);
            return new Expression (LinqExpression.Call (method, value.internalExpression));
        }

        /// <summary>
        /// A return statement with no value. Ends the evaluation of the
        /// enclosing function, which must be created with <see cref="Function"/>
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
        /// condition evaluates to true. <see cref="Break"/> and
        /// <see cref="Continue"/> statements within the body apply to this loop.
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
        /// value, with the variable set to the value. <see cref="Break"/> and
        /// <see cref="Continue"/> statements within the body apply to this loop.
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

        static void CheckIsEnumerable (Expression collection)
        {
            if (!typeof (IEnumerable).IsAssignableFrom (collection.Type))
                throw new InvalidOperationException ("Expected an enumerable collection type");
        }

        static System.Type GetEnumerableValueType (Expression collection)
        {
            CheckIsEnumerable (collection);
            return collection.Type.GetGenericArguments () [0];
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
