using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace KRPC.Utils
{
    /// <summary>
    /// Compiled delegates for the members of a type from another mod, built with
    /// <see cref="Accessor"/>. A member that is not found gets a delegate that throws a
    /// <see cref="ModMemberMissingException"/> when called, and a warning in the log. A mod update
    /// that removes a member then fails only the calls that use it.
    /// </summary>
    /// <remarks>
    /// A binder for a type that was not found binds every member as missing. It logs and records
    /// no member, as the code that looked for the type does so once for the type.
    /// </remarks>
    public sealed class MemberBinder
    {
        readonly string mod;
        readonly Type type;
        readonly string typeName;
        readonly ICollection<string> missing;
        readonly bool required;

        /// <summary>
        /// Create a binder for the members of <paramref name="type"/>, in the mod named
        /// <paramref name="mod"/>. Every member is missing if the type is null.
        /// </summary>
        public MemberBinder (string mod, Type type)
        {
            this.mod = mod;
            this.type = type;
            typeName = type == null ? null : type.Name;
        }

        /// <summary>
        /// Create a binder for a type of the mod named <paramref name="mod"/> that was not found.
        /// Every member is missing, and the messages name the type.
        /// </summary>
        public MemberBinder (string mod, string typeName)
        {
            this.mod = mod;
            this.typeName = typeName;
        }

        /// <summary>
        /// Create a binder that adds the name of each missing member to
        /// <paramref name="missing"/>, or throws for it if <paramref name="required"/>.
        /// </summary>
        internal MemberBinder (string mod, Type type, string typeName, ICollection<string> missing, bool required)
        {
            this.mod = mod;
            this.type = type;
            this.typeName = typeName;
            this.missing = missing;
            this.required = required;
        }

        /// <summary>
        /// The type, or null if it was not found.
        /// </summary>
        public Type Type {
            get { return type; }
        }

        /// <summary>
        /// A binder for another type from the same mod.
        /// </summary>
        public MemberBinder For (Type other)
        {
            return new MemberBinder (mod, other, other == null ? null : other.Name, missing, required);
        }

        /// <summary>
        /// A binder for the same type whose members are required. A member that is not found
        /// throws a <see cref="ModMemberMissingException"/> in place of binding a stub.
        /// </summary>
        public MemberBinder Require {
            get { return new MemberBinder (mod, type, typeName, missing, true); }
        }

        /// <summary>
        /// A getter for a field or property. See <see cref="Accessor.Getter{TInstance, TValue}(Type, string)"/>.
        /// </summary>
        public Func<TInstance, TValue> Getter<TInstance, TValue> (string name)
        {
            return Bind (Accessor.Getter<TInstance, TValue> (type, name), name);
        }

        /// <summary>
        /// A getter for a static field or property. See <see cref="Accessor.StaticGetter{TValue}(Type, string)"/>.
        /// </summary>
        public Func<TValue> StaticGetter<TValue> (string name)
        {
            return Bind (Accessor.StaticGetter<TValue> (type, name), name);
        }

        /// <summary>
        /// A setter for a field or property. See <see cref="Accessor.Setter{TInstance, TValue}(Type, string)"/>.
        /// </summary>
        public Action<TInstance, TValue> Setter<TInstance, TValue> (string name)
        {
            return Bind (Accessor.Setter<TInstance, TValue> (type, name), name);
        }

        /// <summary>
        /// A setter for a static field or property. See <see cref="Accessor.StaticSetter{TValue}(Type, string)"/>.
        /// </summary>
        public Action<TValue> StaticSetter<TValue> (string name)
        {
            return Bind (Accessor.StaticSetter<TValue> (type, name), name);
        }

        /// <summary>
        /// A getter and setter for a field or property that we set. Both bind now, so a read-only
        /// member is missing here. Use <see cref="Getter{TInstance, TValue}(string)"/> for a
        /// member we only read.
        /// </summary>
        public Field<TInstance, TValue> Field<TInstance, TValue> (string name)
        {
            var get = Accessor.Getter<TInstance, TValue> (type, name);
            if (get == null) {
                var missingGet = Bind<Func<TInstance, TValue>> (null, name);
                var missingSet = Stub<Action<TInstance, TValue>> (name);
                return new Field<TInstance, TValue> (missingGet, missingSet);
            }
            var set = Bind (Accessor.Setter<TInstance, TValue> (type, name), name);
            return new Field<TInstance, TValue> (get, set);
        }

        /// <summary>
        /// A delegate that calls a method. See <see cref="Accessor.Method{TDelegate}(Type, string)"/>.
        /// </summary>
        public TDelegate Method<TDelegate> (string name) where TDelegate : class
        {
            return Bind (Accessor.Method<TDelegate> (type, name), name);
        }

        /// <summary>
        /// A delegate of type <paramref name="delegateType"/> that calls a method.
        /// </summary>
        internal Delegate Method (Type delegateType, string name)
        {
            var method = typeof (MemberBinder).GetMethods ()
                .Single (m => m.Name == "Method" && m.IsGenericMethodDefinition)
                .MakeGenericMethod (delegateType);
            try {
                return (Delegate)method.Invoke (this, new object [] { name });
            } catch (TargetInvocationException exn) {
                throw exn.InnerException;
            }
        }

        /// <summary>
        /// A delegate that calls a constructor. See <see cref="Accessor.Constructor{TDelegate}(Type)"/>.
        /// </summary>
        public TDelegate Constructor<TDelegate> () where TDelegate : class
        {
            var constructor = type == null ? null : Accessor.Constructor<TDelegate> (type);
            return Bind (constructor, "constructor");
        }

        T Bind<T> (T value, string name) where T : class
        {
            if (value != null)
                return value;
            if (required)
                throw new ModMemberMissingException (mod, Member (name), Message (name));
            if (type != null) {
                if (missing != null)
                    missing.Add (Member (name));
                Logger.WriteLine (Message (name), Logger.Severity.Warning);
            }
            return Stub<T> (name);
        }

        T Stub<T> (string name) where T : class
        {
            var constructor = typeof (ModMemberMissingException)
                .GetConstructor (new [] { typeof (string), typeof (string), typeof (string) });
            var exception = Expression.New (constructor,
                Expression.Constant (mod), Expression.Constant (Member (name)), Expression.Constant (Message (name)));
            return (T)(object)Accessor.Missing (typeof (T), Message (name), exception);
        }

        string Member (string name)
        {
            return typeName == null ? name : typeName + (name == "constructor" ? " " : ".") + name;
        }

        string Message (string name)
        {
            return mod + ": " + Member (name) + " is not available in the installed version";
        }
    }
}
