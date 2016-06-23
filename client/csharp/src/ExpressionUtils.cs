using System.Linq.Expressions;

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
    }
}
