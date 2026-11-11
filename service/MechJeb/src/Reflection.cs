using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KRPC.Utils;
using Logger = KRPC.Utils.Logger;

namespace KRPC.MechJeb
{
    // Binds to the members of MechJeb by name. MechJeb has no public API, so we bind to the
    // members of its 2.15.4.0 release. A missing member is logged at load, and throws an
    // exception when used, so the rest of the service still works.
    static class Reflection
    {
        static readonly List<string> missing = new List<string> ();

        static Field<object, double> editableDouble;
        static Field<object, int> editableInt;

        public static void Load ()
        {
            missing.Clear ();
            var editableDoubleType = Type ("MuMech.EditableDoubleMult");
            if (editableDoubleType != null)
                editableDouble = Accessor.Field<object, double> (editableDoubleType, "Val");
            var editableIntType = Type ("MuMech.EditableInt");
            if (editableIntType != null)
                editableInt = Accessor.Field<object, int> (editableIntType, "Val");
        }

        public static void LogMissing ()
        {
            if (missing.Count > 0)
                Logger.WriteLine ("MechJeb: members not found: " + string.Join (", ", missing.ToArray ()), Logger.Severity.Warning);
        }

        public static Type Type (string name)
        {
            foreach (var loaded in AssemblyLoader.loadedAssemblies) {
                var type = loaded.assembly.GetType (name);
                if (type != null)
                    return type;
            }
            missing.Add (name);
            return null;
        }

        static string Name (Type type, string name)
        {
            return (type == null ? "?" : type.Name) + "." + name;
        }

        static void Check (object member, Type type, string name)
        {
            if (member == null && type != null)
                missing.Add (Name (type, name));
        }

        /// <summary>
        /// A field or property of the type.
        /// </summary>
        public static Value<T> Field<T> (Type type, string name)
        {
            var field = type == null ? null : Accessor.Field<object, T> (type, name);
            Check (field, type, name);
            return field == null ? new Value<T> (Name (type, name)) :
                new Value<T> (Name (type, name), field.Get, (instance, value) => {
                    var set = field.Set;
                    if (set == null)
                        throw new MissingMemberException ("MechJeb has no writable " + Name (type, name));
                    set (instance, value);
                });
        }

        /// <summary>
        /// A static field or property of the type.
        /// </summary>
        public static Func<T> Static<T> (Type type, string name)
        {
            var get = type == null ? null : Accessor.StaticGetter<T> (type, name);
            Check (get, type, name);
            return get ?? (() => { throw new MissingMemberException ("MechJeb has no " + Name (type, name)); });
        }

        /// <summary>
        /// The value of an EditableDouble, or a type derived from it, held in a field of the type.
        /// We read the field on each access, as MechJeb replaces some of these objects.
        /// </summary>
        public static Value<double> EditableDouble (Type type, string name)
        {
            return Editable (type, name, editableDouble);
        }

        /// <summary>
        /// The value of an EditableInt held in a field of the type.
        /// </summary>
        public static Value<int> EditableInt (Type type, string name)
        {
            return Editable (type, name, editableInt);
        }

        static Value<T> Editable<T> (Type type, string name, Field<object, T> value)
        {
            var field = type == null ? null : Accessor.Getter<object, object> (type, name);
            Check (field, type, name);
            if (field == null || value == null)
                return new Value<T> (Name (type, name));
            return new Value<T> (Name (type, name),
                instance => value.Get (field (instance)),
                (instance, x) => value.Set (field (instance), x));
        }

        /// <summary>
        /// A delegate that calls a method of the type, or throws an exception if the method is
        /// missing. See <see cref="Accessor.Method"/>.
        /// </summary>
        public static TDelegate Method<TDelegate> (Type type, string name) where TDelegate : class
        {
            var method = type == null ? null : Accessor.Method<TDelegate> (type, name);
            Check (method, type, name);
            return method ?? Throwing<TDelegate> (Name (type, name));
        }

        static TDelegate Throwing<TDelegate> (string name)
        {
            var invoke = typeof (TDelegate).GetMethod ("Invoke");
            var parameters = invoke.GetParameters ().Select (p => Expression.Parameter (p.ParameterType, p.Name));
            var exception = Expression.New (
                typeof (MissingMemberException).GetConstructor (new [] { typeof (string) }),
                Expression.Constant ("MechJeb has no " + name));
            return Expression.Lambda<TDelegate> (Expression.Throw (exception, invoke.ReturnType), parameters).Compile ();
        }
    }

    /// <summary>
    /// A field or property of a MechJeb object.
    /// </summary>
    sealed class Value<T>
    {
        readonly string name;
        readonly Func<object, T> get;
        readonly Action<object, T> set;

        public Value (string memberName, Func<object, T> getter = null, Action<object, T> setter = null)
        {
            name = memberName;
            get = getter;
            set = setter;
        }

        public T Get (object instance)
        {
            if (get == null)
                throw new MissingMemberException ("MechJeb has no " + name);
            return get (instance);
        }

        public void Set (object instance, T value)
        {
            if (set == null)
                throw new MissingMemberException ("MechJeb has no writable " + name);
            set (instance, value);
        }
    }
}
