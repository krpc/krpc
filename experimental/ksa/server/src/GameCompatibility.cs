using System;
using System.Collections.Generic;
using System.Linq;
using KSA;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA
{
    /// <summary>
    /// Checks the mod against the running game, for a game version other than the one the
    /// mod was built against.
    /// </summary>
    /// <remarks>
    /// A game member missing from the running game throws when the method using it is first
    /// compiled, so only the RPCs that reach that method fail. KRPC.Service.BrokenProcedures
    /// finds them without running them.
    /// </remarks>
    public static class GameCompatibility
    {
        /// <summary>
        /// The KSA version the mod was built against.
        /// </summary>
        public static Version BuiltFor {
            get { return typeof (GameCompatibility).Assembly.GetReferencedAssemblies ().First (x => x.Name == "KSA").Version; }
        }

        /// <summary>
        /// The version of the running game.
        /// </summary>
        public static Version Running {
            get { return typeof (Universe).Assembly.GetName ().Version; }
        }

        /// <summary>
        /// The RPCs that use game members missing from the running game, as service, class and
        /// member names. Logs the missing member of each, in one entry.
        /// </summary>
        public static IList<string> FindBreakages ()
        {
            var broken = KRPC.Service.BrokenProcedures.Find ();
            if (broken.Count > 0)
                Logger.WriteLine ("RPCs broken by KSA " + Running + ":\n  " +
                    string.Join ("\n  ", broken.Select (x => x.Key + ": " + x.Value)));
            return broken.Select (x => x.Key).ToList ();
        }

        static readonly SortedSet<string> patchBreakages = new SortedSet<string> (StringComparer.Ordinal);

        /// <summary>
        /// The RPCs that need a game patch that failed to apply, as service, class and member
        /// names.
        /// </summary>
        public static IList<string> PatchBreakages {
            get { return patchBreakages.ToList (); }
        }

        /// <summary>
        /// Record a game patch that failed to apply, and log the RPCs that need it.
        /// </summary>
        public static void PatchFailed (string patch, string error, params string [] rpcs)
        {
            Logger.WriteLine ("Failed to patch " + patch + ", needed by " + string.Join (", ", rpcs) + ": " + error, Logger.Severity.Error);
            foreach (var rpc in rpcs)
                patchBreakages.Add (rpc);
        }
    }
}
