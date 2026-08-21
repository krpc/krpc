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
            if (!procedure.HasReturnType)
                throw new InvalidOperationException(
                    "Cannot use a procedure that does not return a value.");

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
        /// </summary>
        /// <returns>A function.</returns>
        /// <param name="parameters">The parameters of the function.</param>
        /// <param name="body">The body of the function.</param>
        [KRPCMethod]
        public static Expression Function (IList<Expression> parameters, Expression body)
        {
            return new Expression (LinqExpression.Lambda (body, parameters.Select(x => (ParameterExpression)(x.internalExpression)).ToArray()));
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
