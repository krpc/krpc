using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using KRPC.Service;

namespace KRPC.Utils
{
    /// <summary>
    /// The integration of one mod. A subclass binds the mod's members in <see cref="Bind"/>, and
    /// holds them in static fields. Each subclass has one instance, created on first use, which
    /// the load pass binds as the game starts. The instance holds whether the integration is
    /// available.
    /// </summary>
    public abstract class ModAPI
    {
        readonly List<string> missingMembers = new List<string> ();

        /// <summary>
        /// Create the integration of the mod named <paramref name="name"/>, whose main assembly is
        /// <paramref name="assemblyName"/>.
        /// </summary>
        protected ModAPI (string name, string assemblyName)
        {
            Name = name;
            AssemblyName = assemblyName;
            Reason = "not loaded";
        }

        /// <summary>
        /// The name of the mod.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The name of the mod's main assembly.
        /// </summary>
        public string AssemblyName { get; private set; }

        /// <summary>
        /// Whether the game has loaded the mod's main assembly, whether or not the integration
        /// loaded.
        /// </summary>
        public bool IsInstalled { get; private set; }

        /// <summary>
        /// Whether the mod is installed and the integration loaded.
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// Why the integration is not available, or null if it is.
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// The types and members of the mod that are bound as stubs.
        /// </summary>
        public IList<string> MissingMembers {
            get { return missingMembers.AsReadOnly (); }
        }

        /// <summary>
        /// Throws an exception if the integration is not available, or the mod is not ready
        /// for a call.
        /// </summary>
        public void CheckAvailable ()
        {
            if (!IsAvailable)
                throw new InvalidOperationException (Name + " is not available: " + Reason);
            var reason = NotReady ();
            if (reason != null)
                throw new InvalidOperationException (Name + " is not ready: " + reason);
        }

        /// <summary>
        /// Why the available integration's mod cannot serve a call now, or null when it can, or
        /// when the integration is not available.
        /// </summary>
        internal string NotReadyReason ()
        {
            return IsAvailable ? NotReady () : null;
        }

        /// <summary>
        /// Why the mod cannot serve a call now, such as while it has no instance in the scene,
        /// or null when it can. Runs before each call that checks the integration, once it is
        /// available.
        /// </summary>
        protected virtual string NotReady ()
        {
            return null;
        }

        /// <summary>
        /// Whether the mod is installed, for a caller with a stock fallback, which runs only when
        /// the mod is not installed. Throws an exception if the mod is installed and the
        /// integration is not available.
        /// </summary>
        public bool CheckInstalled ()
        {
            if (IsInstalled)
                CheckAvailable ();
            return IsInstalled;
        }

        /// <summary>
        /// Bind the members of the mod. An exception marks the integration unavailable.
        /// </summary>
        protected abstract void Bind (ModBinder mod);

        static readonly Dictionary<Type, ModAPI> instances = new Dictionary<Type, ModAPI> ();

        static class Instance<T> where T : ModAPI
        {
            // Null for an integration that cannot be created, so Get<T> reports why on each call
            public static readonly T Value = Create ();

            static T Create ()
            {
                try {
                    return (T)Get (typeof (T));
                } catch (InvalidOperationException) {
                    return null;
                }
            }
        }

        /// <summary>
        /// The instance of the integration. Throws an exception if the integration cannot be
        /// created.
        /// </summary>
        public static T Get<T> () where T : ModAPI
        {
            return Instance<T>.Value ?? (T)Get (typeof (T));
        }

        /// <summary>
        /// The instance of the integration <paramref name="type"/>, a subclass of
        /// <see cref="ModAPI"/>.
        /// </summary>
        internal static ModAPI Get (Type type)
        {
            lock (instances) {
                ModAPI instance;
                if (!instances.TryGetValue (type, out instance)) {
                    try {
                        instance = (ModAPI)Activator.CreateInstance (type, true);
                    } catch (Exception exn) {
                        var error = exn is TargetInvocationException && exn.InnerException != null ? exn.InnerException : exn;
                        throw new InvalidOperationException ("The mod integration " + type + " cannot be created: " + error.Message, error);
                    }
                    instances [type] = instance;
                }
                return instance;
            }
        }

        /// <summary>
        /// Create and bind each integration, from the assemblies that reference kRPC's core.
        /// </summary>
        public static void LoadAll (IModAssemblies assemblies)
        {
            LoadAll (assemblies, Reflection.GetSubclasses<ModAPI> ());
        }

        internal static IList<ModAPI> LoadAll (IModAssemblies assemblies, IEnumerable<Type> types)
        {
            var loaded = assemblies.Loaded.ToList ();
            var result = new List<ModAPI> ();
            foreach (var type in types) {
                ModAPI instance;
                try {
                    instance = Get (type);
                } catch (InvalidOperationException exn) {
                    // A bug in the integration, which leaves only this one not loaded
                    Logger.WriteLine ("Failed to create the mod integration " + type + ": " + exn.InnerException, Logger.Severity.Error);
                    continue;
                }
                instance.Load (loaded);
                result.Add (instance);
            }
            ServiceProblems.Mods = new ReadOnlyCollection<ModAPI> (
                result.Where (x => x.IsInstalled && (!x.IsAvailable || x.missingMembers.Count > 0)).ToList ());
            return result;
        }

        void Load (IList<LoadedAssembly> loaded)
        {
            // Reset the state of an earlier load
            IsInstalled = false;
            IsAvailable = false;
            missingMembers.Clear ();
            var main = loaded.FirstOrDefault (a => a.Assembly.GetName ().Name == AssemblyName);
            if (main == null) {
                Reason = "not installed";
                Logger.WriteLine (Name + " is not installed");
                return;
            }
            IsInstalled = true;
            var directory = System.IO.Path.GetDirectoryName (main.Path);
            var others = loaded
                .Where (a => a != main && System.IO.Path.GetDirectoryName (a.Path) == directory)
                .Select (a => a.Assembly).ToArray ();
            var version = main.Assembly.GetName ().Version;
            try {
                Bind (new ModBinder (Name, main.Assembly, others, missingMembers));
            } catch (ModTypeMissingException exn) {
                Fail ("type " + exn.Type + " is not available in the installed version");
                return;
            } catch (ModMemberMissingException exn) {
                Fail (exn.Member + " is not available in the installed version");
                return;
            } catch (Exception exn) {
                // A bug in the integration, logged with its stack trace
                Fail (exn.GetType ().Name + ": " + exn.Message, exn.ToString ());
                return;
            }
            IsAvailable = true;
            Reason = null;
            Logger.WriteLine (Name + " version " + version + " is installed");
        }

        void Fail (string reason, string detail = null)
        {
            Reason = reason;
            Logger.WriteLine (Name + " failed to load: " + (detail ?? reason), Logger.Severity.Error);
        }
    }
}
