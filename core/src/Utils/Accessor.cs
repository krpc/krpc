using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KRPC.Utils
{
    /// <summary>
    /// Compiled delegates for members found by name, such as non-public game members and the
    /// members of other mods. Each returns null if the member is not found, or its type does
    /// not convert to the delegate's.
    /// </summary>
    public static class Accessor
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic;

        static readonly ConditionalWeakTable<Delegate, string> missing = new ConditionalWeakTable<Delegate, string> ();

        /// <summary>
        /// A getter for a field or property of <typeparamref name="TInstance"/>.
        /// </summary>
        public static Func<TInstance, TValue> Getter<TInstance, TValue> (string name)
        {
            return Getter<TInstance, TValue> (typeof (TInstance), name);
        }

        /// <summary>
        /// A getter for a field or property of <paramref name="type"/>, a type derived from
        /// <typeparamref name="TInstance"/>. The member may be a private member of a base type.
        /// </summary>
        public static Func<TInstance, TValue> Getter<TInstance, TValue> (Type type, string name)
        {
            var instance = Expression.Parameter (typeof (TInstance), "instance");
            var body = Convert (Member (instance, type, name), typeof (TValue));
            return body == null ? null : Expression.Lambda<Func<TInstance, TValue>> (body, instance).Compile ();
        }

        /// <summary>
        /// A getter for a static field or property of <paramref name="type"/>.
        /// </summary>
        public static Func<TValue> StaticGetter<TValue> (Type type, string name)
        {
            var body = Convert (Member (null, type, name), typeof (TValue));
            return body == null ? null : Expression.Lambda<Func<TValue>> (body).Compile ();
        }

        /// <summary>
        /// A setter for a static field or property of <paramref name="type"/>. Returns null for
        /// a read-only field or property.
        /// </summary>
        public static Action<TValue> StaticSetter<TValue> (Type type, string name)
        {
            var value = Expression.Parameter (typeof (TValue), "value");
            var member = Member (null, type, name);
            if (member == null || !CanWrite (member.Member))
                return null;
            var converted = Convert (value, member.Type);
            if (converted == null)
                return null;
            var body = Expression.Assign (member, converted);
            return Expression.Lambda<Action<TValue>> (body, value).Compile ();
        }

        /// <summary>
        /// A setter for a field or property of <typeparamref name="TInstance"/>.
        /// </summary>
        public static Action<TInstance, TValue> Setter<TInstance, TValue> (string name)
        {
            return Setter<TInstance, TValue> (typeof (TInstance), name);
        }

        /// <summary>
        /// A setter for a field or property of <paramref name="type"/>, a type derived from
        /// <typeparamref name="TInstance"/>. Returns null for a read-only field or property.
        /// </summary>
        public static Action<TInstance, TValue> Setter<TInstance, TValue> (Type type, string name)
        {
            var instance = Expression.Parameter (typeof (TInstance), "instance");
            var value = Expression.Parameter (typeof (TValue), "value");
            var member = Member (instance, type, name);
            if (member == null || !CanWrite (member.Member))
                return null;
            var converted = Convert (value, member.Type);
            if (converted == null)
                return null;
            var body = Expression.Assign (member, converted);
            return Expression.Lambda<Action<TInstance, TValue>> (body, instance, value).Compile ();
        }

        /// <summary>
        /// A getter and setter for a field or property of <typeparamref name="TInstance"/>.
        /// </summary>
        public static Field<TInstance, TValue> Field<TInstance, TValue> (string name)
        {
            return Field<TInstance, TValue> (typeof (TInstance), name);
        }

        /// <summary>
        /// A getter and setter for a field or property of <paramref name="type"/>, a type
        /// derived from <typeparamref name="TInstance"/>. The setter is null for a read-only
        /// field or property.
        /// </summary>
        public static Field<TInstance, TValue> Field<TInstance, TValue> (Type type, string name)
        {
            var get = Getter<TInstance, TValue> (type, name);
            return get == null ? null : new Field<TInstance, TValue> (get, Setter<TInstance, TValue> (type, name));
        }

        /// <summary>
        /// A delegate that calls a method of <paramref name="type"/>. For an instance method,
        /// the delegate's first parameter is the instance, and a virtual method dispatches to
        /// the instance's override.
        /// </summary>
        /// <remarks>
        /// The method's parameters match the delegate's other parameters exactly or, for the
        /// only method of that name and parameter count, convert from them. A <c>ref</c> or
        /// <c>out</c> parameter always matches exactly.
        /// </remarks>
        public static TDelegate Method<TDelegate> (Type type, string name) where TDelegate : class
        {
            if (type == null)
                return null;
            var invoke = typeof (TDelegate).GetMethod ("Invoke");
            var parameters = invoke.GetParameters ()
                .Select (p => Expression.Parameter (p.ParameterType, p.Name)).ToArray ();
            var types = parameters.Select (p => p.IsByRef ? p.Type.MakeByRefType () : p.Type).ToArray ();
            Expression instance = null;
            var arguments = parameters;
            var method = types.Length == 0 ? null
                : FindMethod (type, name, BindingFlags.Instance, types.Skip (1).ToArray ());
            if (method != null) {
                instance = Convert (parameters [0], type);
                if (instance == null)
                    return null;
                arguments = parameters.Skip (1).ToArray ();
            } else {
                method = FindMethod (type, name, BindingFlags.Static, types);
                if (method == null)
                    return null;
            }
            var converted = method.GetParameters ()
                .Select ((p, i) => p.ParameterType.IsByRef ? arguments [i] : Convert (arguments [i], p.ParameterType))
                .ToArray ();
            if (converted.Any (x => x == null))
                return null;
            Expression body = Expression.Call (instance, method, converted);
            if (invoke.ReturnType != typeof (void))
                body = Convert (body, invoke.ReturnType);
            return body == null ? null : Expression.Lambda<TDelegate> (body, parameters).Compile ();
        }

        /// <summary>
        /// A delegate that calls a constructor of <paramref name="type"/> and returns the new
        /// instance.
        /// </summary>
        /// <remarks>
        /// The constructor's parameters match the delegate's exactly or, for the only
        /// constructor with that parameter count, convert from them.
        /// </remarks>
        public static TDelegate Constructor<TDelegate> (Type type) where TDelegate : class
        {
            if (type.IsAbstract)
                return null;
            var invoke = typeof (TDelegate).GetMethod ("Invoke");
            var parameters = invoke.GetParameters ()
                .Select (p => Expression.Parameter (p.ParameterType, p.Name)).ToArray ();
            var types = parameters.Select (p => p.Type).ToArray ();
            var constructor = type.GetConstructor (Flags | BindingFlags.Instance, null, types, null);
            if (constructor == null) {
                var candidates = type.GetConstructors (Flags | BindingFlags.Instance)
                    .Where (c => c.GetParameters ().Length == types.Length)
                    .Take (2).ToArray ();
                if (candidates.Length != 1)
                    return null;
                constructor = candidates [0];
            }
            var arguments = constructor.GetParameters ()
                .Select ((p, i) => p.ParameterType.IsByRef ? null : Convert (parameters [i], p.ParameterType))
                .ToArray ();
            if (arguments.Any (x => x == null))
                return null;
            var body = Convert (Expression.New (constructor, arguments), invoke.ReturnType);
            return body == null ? null : Expression.Lambda<TDelegate> (body, parameters).Compile ();
        }

        /// <summary>
        /// The type of a field or property of <paramref name="type"/>, or null if there is no
        /// such member.
        /// </summary>
        public static Type MemberType (Type type, string name)
        {
            var member = FindMember (type, name, BindingFlags.Instance) ?? FindMember (type, name, BindingFlags.Static);
            var field = member as FieldInfo;
            if (field != null)
                return field.FieldType;
            var property = member as PropertyInfo;
            return property == null ? null : property.PropertyType;
        }

        /// <summary>
        /// A delegate that throws a <see cref="MissingMemberException"/> with the message when
        /// called, standing in for a member that was not found.
        /// </summary>
        public static TDelegate Missing<TDelegate> (string message) where TDelegate : class
        {
            return (TDelegate)(object)Missing (typeof (TDelegate), message);
        }

        /// <summary>
        /// A delegate of type <paramref name="delegateType"/> that throws a
        /// <see cref="MissingMemberException"/> with the message when called.
        /// </summary>
        public static Delegate Missing (Type delegateType, string message)
        {
            return Missing (delegateType, message, Expression.New (
                typeof (MissingMemberException).GetConstructor (new [] { typeof (string) }),
                Expression.Constant (message)));
        }

        /// <summary>
        /// A delegate of type <paramref name="delegateType"/> that throws the exception the
        /// expression creates when called.
        /// </summary>
        internal static Delegate Missing (Type delegateType, string message, NewExpression exception)
        {
            if (!typeof (Delegate).IsAssignableFrom (delegateType))
                throw new ArgumentException ("Not a delegate type", nameof (delegateType));
            var invoke = delegateType.GetMethod ("Invoke");
            var parameters = invoke.GetParameters ().Select (p => Expression.Parameter (p.ParameterType, p.Name));
            var body = Expression.Throw (exception, invoke.ReturnType);
            var result = Expression.Lambda (delegateType, body, parameters).Compile ();
            missing.Add (result, message);
            return result;
        }

        /// <summary>
        /// Whether the delegate is null or was created by <see cref="Missing(Type, string)"/>.
        /// </summary>
        public static bool IsMissing (Delegate value)
        {
            string message;
            return value == null || missing.TryGetValue (value, out message);
        }

        static MethodInfo FindMethod (Type type, string name, BindingFlags kind, Type [] types)
        {
            var method = type.GetMethod (name, Flags | kind, null, types, null);
            if (method != null)
                return method;
            var candidates = type.GetMethods (Flags | kind)
                .Where (m => m.Name == name && m.GetParameters ().Length == types.Length)
                .Take (2).ToArray ();
            if (candidates.Length != 1)
                return null;
            var parameters = candidates [0].GetParameters ();
            for (int i = 0; i < types.Length; i++) {
                var parameter = parameters [i].ParameterType;
                if ((parameter.IsByRef || types [i].IsByRef) && parameter != types [i])
                    return null;
            }
            return candidates [0];
        }

        /// <summary>
        /// A field or property of the instance, or a static one if the instance is null.
        /// </summary>
        static MemberExpression Member (ParameterExpression instance, Type type, string name)
        {
            var member = FindMember (type, name, instance == null ? BindingFlags.Static : BindingFlags.Instance);
            if (member == null)
                return null;
            var target = Convert (instance, type);
            if (instance != null && target == null)
                return null;
            return Expression.MakeMemberAccess (target, member);
        }

        /// <summary>
        /// The field or property declared closest to <paramref name="type"/>. Walking the
        /// declaring types finds private members of base types, and the member hiding another.
        /// </summary>
        static MemberInfo FindMember (Type type, string name, BindingFlags kind)
        {
            for (; type != null; type = type.BaseType) {
                var flags = Flags | kind | BindingFlags.DeclaredOnly;
                var field = type.GetField (name, flags);
                if (field != null)
                    return field;
                var property = type.GetProperty (name, flags);
                if (property != null && property.GetIndexParameters ().Length == 0)
                    return property;
            }
            return null;
        }

        static bool CanWrite (MemberInfo member)
        {
            var field = member as FieldInfo;
            if (field != null)
                return !field.IsInitOnly && !field.IsLiteral;
            return ((PropertyInfo)member).CanWrite;
        }

        /// <summary>
        /// The expression converted to the type, or null if there is no conversion.
        /// </summary>
        static Expression Convert (Expression expression, Type type)
        {
            if (expression == null || expression.Type == type)
                return expression;
            try {
                return Expression.Convert (expression, type);
            } catch (InvalidOperationException) {
                return null;
            }
        }
    }
}
