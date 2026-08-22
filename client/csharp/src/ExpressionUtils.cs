using System;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Client.Attributes;

namespace KRPC.Client
{
    static class ExpressionUtils
    {
        internal static bool IsAClassMethod (MethodCallExpression expression)
        {
            return IsARemoteObject (expression.Object);
        }

        internal static bool IsAClassProperty (MemberExpression expression)
        {
            return IsARemoteObject (expression.Expression);
        }

        internal static bool IsAClassStaticMethod (MethodCallExpression expression)
        {
            return expression.Object == null;
        }

        static bool IsARemoteObject (Expression instance)
        {
            return instance != null && typeof(RemoteObject).IsAssignableFrom (instance.Type);
        }

        // Stream lambdas may wrap a remote call in conversions and multiplication.
        // F# units of measure compile to a multiply by one.
        internal static bool TryFindStreamedRpc (Expression expression, out Expression rpc)
        {
            rpc = null;
            return FindStreamedRpc (expression, ref rpc);
        }

        static bool FindStreamedRpc (Expression expression, ref Expression rpc)
        {
            var unary = expression as UnaryExpression;
            if (unary != null && (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked))
                return FindStreamedRpc (unary.Operand, ref rpc);

            var binary = expression as BinaryExpression;
            if (binary != null && (binary.NodeType == ExpressionType.Multiply ||
                binary.NodeType == ExpressionType.MultiplyChecked))
                return FindStreamedRpc (binary.Left, ref rpc) &&
                    FindStreamedRpc (binary.Right, ref rpc);

            var call = expression as MethodCallExpression;
            if (call != null && IsRpc (call.Method)) {
                if (rpc != null)
                    return false;
                rpc = expression;
                return true;
            }

            var member = expression as MemberExpression;
            if (member != null && IsRpc (member.Member)) {
                if (rpc != null)
                    return false;
                rpc = expression;
                return true;
            }

            return true;
        }

        internal static bool IsConstantMultiplyWrapper (Expression body, Expression rpc)
        {
            return IsWrapper (body, rpc, IsConstantFactor);
        }

        internal static bool IsIdentityWrapper (Expression body, Expression rpc)
        {
            return body.Type == rpc.Type && IsWrapper (body, rpc, IsMultiplicativeIdentity);
        }

        internal static object FoldedFactor (Expression body, Expression rpc)
        {
            if (ReferenceEquals (body, rpc))
                return null;

            var unary = body as UnaryExpression;
            if (unary != null && (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked)) {
                var inner = FoldedFactor (unary.Operand, rpc);
                if (inner == null)
                    return null;
                return Convert.ChangeType (inner, unary.Type);
            }

            var binary = body as BinaryExpression;
            if (binary != null && (binary.NodeType == ExpressionType.Multiply ||
                binary.NodeType == ExpressionType.MultiplyChecked)) {
                object inner;
                object factor;
                if (IsWrapper (binary.Left, rpc, IsConstantFactor)) {
                    inner = FoldedFactor (binary.Left, rpc);
                    TryConstantValue (binary.Right, out factor);
                } else {
                    inner = FoldedFactor (binary.Right, rpc);
                    TryConstantValue (binary.Left, out factor);
                }
                if (inner == null)
                    return Convert.ChangeType (factor, binary.Type);
                return Convert.ChangeType (
                    Convert.ToDouble (inner) * Convert.ToDouble (factor), binary.Type);
            }

            return null;
        }

        static bool IsWrapper (Expression body, Expression rpc, Func<Expression, bool> factorOk)
        {
            if (ReferenceEquals (body, rpc))
                return true;

            var unary = body as UnaryExpression;
            if (unary != null && (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked))
                return IsWrapper (unary.Operand, rpc, factorOk);

            var binary = body as BinaryExpression;
            if (binary != null && (binary.NodeType == ExpressionType.Multiply ||
                binary.NodeType == ExpressionType.MultiplyChecked)) {
                if (IsWrapper (binary.Left, rpc, factorOk))
                    return factorOk (binary.Right);
                if (IsWrapper (binary.Right, rpc, factorOk))
                    return factorOk (binary.Left);
            }

            return false;
        }

        internal static Func<object, T> CompileTransform<T> (Expression body, Expression rpc)
        {
            var value = Expression.Parameter (typeof(object), "value");
            var transformed = new Replacer (rpc, Expression.Convert (value, rpc.Type)).Visit (body);
            if (transformed.Type != typeof(T))
                transformed = Expression.Convert (transformed, typeof(T));
            return Expression.Lambda<Func<object, T>> (transformed, value).Compile ();
        }

        static bool IsRpc (MemberInfo member)
        {
            return member.GetCustomAttributes (typeof(RPCAttribute), false).Length == 1;
        }

        static bool IsConstantFactor (Expression expression)
        {
            object value;
            return TryConstantValue (expression, out value);
        }

        static bool IsMultiplicativeIdentity (Expression expression)
        {
            object value;
            if (!TryConstantValue (expression, out value) || value == null)
                return false;
            if (value is int)
                return (int)value == 1;
            if (value is long)
                return (long)value == 1L;
            if (value is float)
                return (float)value == 1.0f;
            if (value is double)
                return (double)value == 1.0;
            if (value is uint)
                return (uint)value == 1u;
            if (value is ulong)
                return (ulong)value == 1ul;
            return false;
        }

        static bool TryConstantValue (Expression expression, out object value)
        {
            while (true) {
                var unary = expression as UnaryExpression;
                if (unary == null || (unary.NodeType != ExpressionType.Convert &&
                    unary.NodeType != ExpressionType.ConvertChecked))
                    break;
                expression = unary.Operand;
            }
            var constant = expression as ConstantExpression;
            if (constant == null) {
                value = null;
                return false;
            }
            value = constant.Value;
            return true;
        }

        sealed class Replacer : ExpressionVisitor
        {
            readonly Expression from;
            readonly Expression to;

            public Replacer (Expression from, Expression to)
            {
                this.from = from;
                this.to = to;
            }

            public override Expression Visit (Expression node)
            {
                if (ReferenceEquals (node, from))
                    return to;
                return base.Visit (node);
            }
        }
    }
}
