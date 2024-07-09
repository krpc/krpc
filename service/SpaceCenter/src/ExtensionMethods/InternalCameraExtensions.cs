using System;
using System.Linq.Expressions;
using System.Reflection;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// A few small utility functions for getting and setting private members of the internal camera.
    /// </summary>
    public static class InternalCameraExtensions
    {
        private static readonly Lazy<Action<InternalCamera, object>> _setPitch =
            new Lazy<Action<InternalCamera, object>>(() => CreateFieldSetter<InternalCamera>("currentPitch"));

        private static readonly Lazy<Func<InternalCamera, object>> _getPitch =
            new Lazy<Func<InternalCamera, object>>(() => CreateFieldGetter<InternalCamera>("currentPitch"));

        private static readonly Lazy<Action<InternalCamera, object>> _setRot =
            new Lazy<Action<InternalCamera, object>>(() => CreateFieldSetter<InternalCamera>("currentRot"));

        private static readonly Lazy<Action<InternalCamera, object>> _setZoom =
            new Lazy<Action<InternalCamera, object>>(() => CreateFieldSetter<InternalCamera>("currentZoom"));

        private static readonly Lazy<Func<InternalCamera, object>> _getRot =
            new Lazy<Func<InternalCamera, object>>(() => CreateFieldGetter<InternalCamera>("currentRot"));

        private static readonly Lazy<Func<InternalCamera, object>> _getFoV =
            new Lazy<Func<InternalCamera, object>>(() => CreateFieldGetter<InternalCamera>("currentFoV"));

        private static readonly Lazy<Func<InternalCamera, object>> _getDefaultFoV =
            new Lazy<Func<InternalCamera, object>>(() => CreateFieldGetter<InternalCamera>("initialZoom"));

        /// <summary>
        /// Sets the pitch of the internal camera.
        /// </summary>
        public static Action<InternalCamera, object> SetPitch => _setPitch.Value;

        /// <summary>
        /// Gets the pitch of the internal camera.
        /// </summary>
        public static Func<InternalCamera, object> GetPitch => _getPitch.Value;

        /// <summary>
        /// Sets the rotation of the internal camera.
        /// </summary>
        public static Action<InternalCamera, object> SetRot => _setRot.Value;

        /// <summary>
        /// Sets the current zoom of the internal camera.
        /// </summary>
        public static Action<InternalCamera, object> SetZoom => _setZoom.Value;

        /// <summary>
        /// Gets the rotation of the internal camera.
        /// </summary>
        public static Func<InternalCamera, object> GetRot => _getRot.Value;

        /// <summary>
        /// Gets the field of view of the internal camera.
        /// </summary>
        public static Func<InternalCamera, object> GetFoV => _getFoV.Value;

        /// <summary>
        /// Gets the default field of view of the internal camera.
        /// </summary>
        public static Func<InternalCamera, object> GetDefaultFoV => _getDefaultFoV.Value;

        private static Action<T, object> CreateFieldSetter<T>(string fieldName)
        {
            var parameterExpression = Expression.Parameter(typeof(T), "target");
            var valueExpression = Expression.Parameter(typeof(object), "value");

            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldExpression = Expression.Field(parameterExpression, field);
            var assignExpression = Expression.Assign(fieldExpression, Expression.Convert(valueExpression, field.FieldType));

            var lambdaExpression = Expression.Lambda<Action<T, object>>(assignExpression, parameterExpression, valueExpression);
            return lambdaExpression.Compile();
        }

        private static Func<T, object> CreateFieldGetter<T>(string fieldName)
        {
            var parameterExpression = Expression.Parameter(typeof(T), "target");

            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldExpression = Expression.Field(parameterExpression, field);
            var convertExpression = Expression.Convert(fieldExpression, typeof(object));

            var lambdaExpression = Expression.Lambda<Func<T, object>>(convertExpression, parameterExpression);
            return lambdaExpression.Compile();
        }
    }
}
