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

        // Find the single remote call a stream lambda wraps in conversions and
        // multiplications. Returns false if the lambda wraps more than one.
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

        // The rpc may be wrapped in conversions and multiplications by one that leave its
        // value and type unchanged, as F# units of measure produce.
        internal static bool IsIdentityWrapper (Expression body, Expression rpc)
        {
            return body.Type == rpc.Type && IsWrapper (body, rpc);
        }

        static bool IsWrapper (Expression body, Expression rpc)
        {
            if (ReferenceEquals (body, rpc))
                return true;

            var unary = body as UnaryExpression;
            if (unary != null && (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked))
                return IsWrapper (unary.Operand, rpc);

            var binary = body as BinaryExpression;
            if (binary != null && (binary.NodeType == ExpressionType.Multiply ||
                binary.NodeType == ExpressionType.MultiplyChecked)) {
                if (IsWrapper (binary.Left, rpc))
                    return IsMultiplicativeIdentity (binary.Right);
                if (IsWrapper (binary.Right, rpc))
                    return IsMultiplicativeIdentity (binary.Left);
            }

            return false;
        }

        static bool IsRpc (MemberInfo member)
        {
            return member.GetCustomAttributes (typeof(RPCAttribute), false).Length == 1;
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
    }
}
