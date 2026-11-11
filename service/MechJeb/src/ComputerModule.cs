using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// A wrapper of a MechJeb computer module.
    /// </summary>
    public abstract class ComputerModule
    {
        static Value<bool> enabled;
        static Func<object, object> users;
        static Action<object, object> usersAdd;
        static Action<object, object> usersRemove;

        readonly string moduleName;

        internal static void Load ()
        {
            var type = Reflection.Type ("MuMech.ComputerModule");
            enabled = Reflection.Field<bool> (type, "Enabled");
            users = Reflection.Field<object> (type, "Users").Get;
            var userPool = Reflection.Type ("MuMech.UserPool");
            usersAdd = Reflection.Method<Action<object, object>> (userPool, "Add");
            usersRemove = Reflection.Method<Action<object, object>> (userPool, "Remove");
        }

        /// <summary>
        /// Create a wrapper of the module with the given type name, such as <c>MechJebModuleSmartASS</c>.
        /// </summary>
        internal ComputerModule (string name)
        {
            moduleName = name;
        }

        /// <summary>
        /// The module of the active vessel's MechJeb.
        /// </summary>
        internal virtual object Instance {
            get { return MechJeb.Module (moduleName); }
        }

        /// <summary>
        /// Whether the module is enabled.
        /// </summary>
        /// <remarks>
        /// We enable a module by adding this wrapper to its users, as MechJeb's windows do. The
        /// module stays enabled while any other user holds it.
        /// </remarks>
        public virtual bool Enabled {
            get { return enabled.Get (Instance); }
            set { SetEnabled (Instance, value); }
        }

        /// <summary>
        /// Add this wrapper to, or remove it from, the users of a module.
        /// </summary>
        internal void SetEnabled (object module, bool value)
        {
            var pool = users (module);
            if (value)
                usersAdd (pool, this);
            else
                usersRemove (pool, this);
        }
    }

    /// <summary>
    /// A wrapper of a MechJeb computer module that can be enabled through the service.
    /// </summary>
    public abstract class KRPCComputerModule : ComputerModule
    {
        internal KRPCComputerModule (string name) : base (name)
        {
        }

        /// <summary>
        /// Whether the module is enabled.
        /// </summary>
        [KRPCProperty]
        public override bool Enabled {
            get { return base.Enabled; }
            set { base.Enabled = value; }
        }
    }

    /// <summary>
    /// A wrapper of a MechJeb autopilot module.
    /// </summary>
    public abstract class AutopilotModule : KRPCComputerModule
    {
        static Value<string> status;

        internal static new void Load ()
        {
            status = Reflection.Field<string> (Reflection.Type ("MuMech.AutopilotModule"), "Status");
        }

        internal AutopilotModule (string name) : base (name)
        {
        }

        /// <summary>
        /// The status of the autopilot.
        /// </summary>
        [KRPCProperty]
        public string Status {
            get { return status.Get (Instance); }
        }
    }

    /// <summary>
    /// A wrapper of a MechJeb window.
    /// </summary>
    public abstract class DisplayModule : ComputerModule
    {
        internal DisplayModule (string name) : base (name)
        {
        }
    }
}
