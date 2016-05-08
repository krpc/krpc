using System.Linq.Expressions;

namespace KRPC.Client
{
    internal static class ExpressionUtils
    {
        internal static bool IsAClassMethod (MethodCallExpression expression)
        {
            var instance = expression.Object;
            if (instance == null)
                return false;
            return typeof(RemoteObject).IsAssignableFrom (instance.Type);
        }

        internal static bool IsAClassStaticMethod (MethodCallExpression expression)
        {
            return expression.Object == null;
        }

        internal static bool IsAClassProperty (MemberExpression expression)
        {
            var instance = expression.Expression;
            if (instance == null)
                return false;
            return typeof(RemoteObject).IsAssignableFrom (instance.Type);
        }
    }
}
