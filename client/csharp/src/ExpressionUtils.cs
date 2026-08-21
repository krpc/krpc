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
            if (expression == null)
                return true;

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

        internal static bool IsIdentityWrapper (Expression body, Expression rpc)
        {
            if (ReferenceEquals (body, rpc))
                return true;
            if (body.Type != rpc.Type)
                return false;

            var unary = body as UnaryExpression;
            if (unary != null && (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked))
                return IsIdentityWrapper (unary.Operand, rpc);

            var binary = body as BinaryExpression;
            if (binary != null && (binary.NodeType == ExpressionType.Multiply ||
                binary.NodeType == ExpressionType.MultiplyChecked)) {
                if (IsIdentityWrapper (binary.Left, rpc))
                    return IsMultiplicativeIdentity (binary.Right);
                if (IsIdentityWrapper (binary.Right, rpc))
                    return IsMultiplicativeIdentity (binary.Left);
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

        static bool IsMultiplicativeIdentity (Expression expression)
        {
            object value;
            try {
                var lambda = Expression.Lambda<Func<object>> (
                                 Expression.Convert (expression, typeof(object)));
                value = lambda.Compile () ();
            } catch (System.Exception) {
                return false;
            }
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
