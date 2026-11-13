using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// The problems found while loading the services.
    /// </summary>
    public static class ServiceProblems
    {
        /// <summary>
        /// Scan the services, if they have not been scanned, and log their problems. Throws an
        /// exception if kRPC cannot run, such as when the KRPC service is not available.
        /// </summary>
        public static void Load ()
        {
            Services.Init ();
        }

        /// <summary>
        /// The errors in the services. Each left out the members it names, and the members that
        /// use them.
        /// </summary>
        public static IList<string> Errors {
            get { return Services.Instance.Errors; }
        }

        /// <summary>
        /// The warnings about the services, which left their members in.
        /// </summary>
        public static IList<string> Warnings {
            get { return Services.Instance.Warnings; }
        }

        /// <summary>
        /// The mod integrations whose mod is installed, and that failed to load or bound a
        /// member as a stub.
        /// </summary>
        public static IList<ModAPI> Mods { get; internal set; } = new List<ModAPI> ().AsReadOnly ();
    }
}
