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

        /// <summary>
        /// A constant value of type double.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantDouble(double value)
        {
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// A constant value of type float.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantFloat(float value)
        {
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// A constant value of type int.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantInt(int value)
        {
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// A constant value of type string.
        /// </summary>
        /// <param name="value"></param>
        [KRPCMethod]
        public static Expression ConstantString(string value)
        {
            return new Expression(LinqExpression.Constant(value));
        }

        /// <summary>
        /// An RPC call.
        /// </summary>
        /// <param name="call"></param>
        [KRPCMethod]
        public static Expression Call(ProcedureCall call)
        {
            if (ReferenceEquals (call, null))
                throw new ArgumentNullException (nameof (call));
            var services = Services.Instance;
            var procedure = services.GetProcedureSignature(call.Service, call.Procedure);
            if (!procedure.HasReturnType)
                throw new InvalidOperationException(
                    "Cannot use a procedure that does not return a value.");
            var arguments = services.GetArguments(procedure, call.Arguments);

            var servicesExpr = LinqExpression.Constant(services);
            var executeCallMethod = typeof(Services).GetMethod(
                "ExecuteCall", new[] { typeof(Scanner.ProcedureSignature), typeof(object[]) });
            var procedureExpr = LinqExpression.Constant(procedure);
            var argumentsExpr = LinqExpression.Constant(arguments);

            var result = LinqExpression.Call(
                servicesExpr, executeCallMethod,
                new[] { procedureExpr, argumentsExpr });
            var value = LinqExpression.Convert(
                LinqExpression.Property(result, "Value"), procedure.ReturnType);
            return new Expression(value);
        }

        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Equal(Expression arg0, Expression arg1)
        {
            return new Expression(
                LinqExpression.Equal(arg0, arg1));
        }

        /// <summary>
        /// Inequality comparison.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression NotEqual(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.NotEqual(arg0, arg1));
        }

        /// <summary>
        /// Greater than numerical comparison.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression GreaterThan(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.GreaterThan(arg0, arg1));
        }

        /// <summary>
        /// Greater than or equal numerical comparison.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression GreaterThanOrEqual(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.GreaterThanOrEqual(arg0, arg1));
        }

        /// <summary>
        /// Less than numerical comparison.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression LessThan(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.LessThan(arg0, arg1));
        }

        /// <summary>
        /// Less than or equal numerical comparison.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression LessThanOrEqual(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.LessThanOrEqual(arg0, arg1));
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
        /// Numerical addition.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Add(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Add(arg0, arg1));
        }

        /// <summary>
        /// Numerical subtraction.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Subtract(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Subtract(arg0, arg1));
        }

        /// <summary>
        /// Numerical multiplication.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Multiply(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Multiply(arg0, arg1));
        }

        /// <summary>
        /// Numerical division.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        [KRPCMethod]
        public static Expression Divide(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Divide(arg0, arg1));
        }

        /// <summary>
        /// Numerical modulo operator.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns>The remainder of arg0 divided by arg1</returns>
        [KRPCMethod]
        public static Expression Modulo(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Modulo(arg0, arg1));
        }

        /// <summary>
        /// Numerical power operator.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns>arg0 raised to the power of arg1</returns>
        [KRPCMethod]
        public static Expression Power(Expression arg0, Expression arg1)
        {
            return new Expression(LinqExpression.Power(arg0, arg1));
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
        /// Convert to a double type.
        /// </summary>
        /// <param name="arg"></param>
        [KRPCMethod]
        public static Expression ToDouble(Expression arg)
        {
            return new Expression(LinqExpression.Convert(arg, typeof(double)));
        }

        /// <summary>
        /// Convert to a float type.
        /// </summary>
        /// <param name="arg"></param>
        [KRPCMethod]
        public static Expression ToFloat(Expression arg)
        {
            return new Expression(LinqExpression.Convert(arg, typeof(float)));
        }

        /// <summary>
        /// Convert to an int type.
        /// </summary>
        /// <param name="arg"></param>
        [KRPCMethod]
        public static Expression ToInt(Expression arg)
        {
            return new Expression(LinqExpression.Convert(arg, typeof(int)));
        }
    }
}
