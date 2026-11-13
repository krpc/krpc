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
        /// The mod integrations whose mod is installed, and that failed to load or bound a
        /// member as a stub.
        /// </summary>
        public static IList<ModAPI> Mods { get; internal set; } = new List<ModAPI> ().AsReadOnly ();
    }
}
