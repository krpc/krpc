using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KRPC.Utils
{
    /// <summary>
    /// Binds to the members of a mod by reflection, for <see cref="ModAPI.Bind"/>. Gives a
    /// <see cref="MemberBinder"/> for each type of the mod's assemblies: its main assembly and
    /// those beside it in its directory. A type that is not found binds every member as missing.
    /// </summary>
    public sealed class ModBinder
    {
        readonly Assembly [] others;
        readonly ICollection<string> missing;
        readonly bool required;

        internal ModBinder (string name, Assembly assembly, Assembly [] others, ICollection<string> missing, bool required = false)
        {
            Name = name;
            Assembly = assembly;
            this.others = others;
            this.missing = missing;
            this.required = required;
        }

        /// <summary>
        /// The name of the mod, used in the log and in the messages of missing members.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The mod's main assembly.
        /// </summary>
        public Assembly Assembly { get; private set; }

        /// <summary>
        /// A binder whose types are required. A type that is not found throws a
        /// <see cref="ModTypeMissingException"/>, and fails the load.
        /// </summary>
        public ModBinder Require {
            get { return new ModBinder (Name, Assembly, others, missing, true); }
        }

        /// <summary>
        /// A binder for the type of the given full name. A type that is not found gives a binder
        /// whose members are all missing, and a warning in the log.
        /// </summary>
        public MemberBinder Type (string fullName)
        {
            var type = Assembly.GetType (fullName) ??
                others.Select (a => a.GetType (fullName)).FirstOrDefault (t => t != null);
            var name = fullName.Substring (fullName.LastIndexOfAny (new [] { '.', '+' }) + 1);
            if (type != null)
                return new MemberBinder (Name, type, type.Name, missing, false);
            if (required)
                throw new ModTypeMissingException (Name, name);
            missing.Add (name);
            Logger.WriteLine (Name + ": type " + name + " is not available in the installed version",
                Logger.Severity.Warning);
            return new MemberBinder (Name, null, name, missing, false);
        }

        /// <summary>
        /// Bind each static delegate property of <paramref name="api"/> to the static method of
        /// the same name in the mod's type <paramref name="typeName"/>.
        /// </summary>
        public void Statics (Type api, string typeName)
        {
            if (api == null)
                throw new ArgumentNullException (nameof (api));
            var type = Type (typeName);
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var property in api.GetProperties (flags)) {
                if (typeof (Delegate).IsAssignableFrom (property.PropertyType))
                    property.SetValue (null, type.Method (property.PropertyType, property.Name), null);
            }
        }
    }
}
