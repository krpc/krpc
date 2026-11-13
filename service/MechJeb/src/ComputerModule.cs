using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// A wrapper of a MechJeb computer module.
    /// </summary>
    public abstract class ComputerModule
    {
        static Func<object, bool> enabled;
        static Func<object, object> users;
        static Action<object, object> usersAdd;
        static Action<object, object> usersRemove;

        readonly string moduleName;

        internal static void Load ()
        {
            var type = Bindings.Type ("MuMech.ComputerModule");
            enabled = type.Getter<object, bool> ("Enabled");
            users = type.Getter<object, object> ("Users");
            var userPool = Bindings.Type ("MuMech.UserPool");
            usersAdd = userPool.Method<Action<object, object>> ("Add");
            usersRemove = userPool.Method<Action<object, object>> ("Remove");
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
            get { return enabled (Instance); }
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
        static Func<object, string> status;

        internal static new void Load ()
        {
            status = Bindings.Type ("MuMech.AutopilotModule").Getter<object, string> ("Status");
        }

        internal AutopilotModule (string name) : base (name)
        {
        }

        /// <summary>
        /// The status of the autopilot.
        /// </summary>
        [KRPCProperty]
        public string Status {
            get { return status (Instance); }
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
