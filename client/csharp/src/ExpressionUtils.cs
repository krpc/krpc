using System.Linq.Expressions;

namespace KRPC.Client
{
    static class ExpressionUtils
    {
        internal static bool IsAClassMethod (MethodCallExpression expression)
        {
            var instance = expression.Object;
            return instance != null && typeof(RemoteObject).IsAssignableFrom (instance.Type);
        }

        internal static bool IsAClassStaticMethod (MethodCallExpression expression)
        {
            return expression.Object == null;
        }

        internal static bool IsAClassProperty (MemberExpression expression)
        {
            var instance = expression.Expression;
            return instance != null && typeof(RemoteObject).IsAssignableFrom (instance.Type);
        }
    }
}
