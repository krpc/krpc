using System;
using System.Collections.Generic;
using System.Linq;

namespace KRPC.Utils
{
    /// <summary>
    /// The mod integrations a procedure requires: a list of alternatives, any one of which will
    /// do, each a list of integrations that must all be available.
    /// </summary>
    public sealed class ModRequirement
    {
        internal ModRequirement (IList<IList<ModAPI>> alternatives)
        {
            Alternatives = alternatives;
        }

        /// <summary>
        /// The alternatives, any one of which will do. Each lists integrations that must all be
        /// available.
        /// </summary>
        public IList<IList<ModAPI>> Alternatives { get; private set; }

        /// <summary>
        /// Throws an exception if no alternative has all of its integrations available and
        /// ready. The message names the integrations that are not, with their reasons.
        /// </summary>
        public void Check ()
        {
            // A single integration, as nearly every requirement is
            if (Alternatives.Count == 1 && Alternatives [0].Count == 1) {
                Alternatives [0] [0].CheckAvailable ();
                return;
            }
            foreach (var alternative in Alternatives) {
                if (alternative.All (x => x.IsAvailable && x.NotReadyReason () == null))
                    return;
            }
            throw new InvalidOperationException (Message ());
        }

        /// <summary>
        /// The message for a failed check: the integrations that are not available or not ready,
        /// with their reasons.
        /// </summary>
        string Message ()
        {
            var problems = Alternatives.SelectMany (x => x).Distinct ()
                .ToDictionary (x => x, x => x.IsAvailable ? x.NotReadyReason () : x.Reason);
            var missing = Alternatives.Select (x => x.Where (y => problems [y] != null).ToList ()).ToList ();
            var names = string.Join (" or ", missing.Select (x => string.Join (" and ", x.Select (y => y.Name).ToArray ())).ToArray ());
            var mods = missing.SelectMany (x => x).Distinct ().ToList ();
            var notReady = mods.All (x => x.IsAvailable);
            var reasons = mods.Select (x => problems [x]).Distinct ().ToList ();
            var reason = reasons.Count == 1 ? reasons [0] :
                string.Join ("; ", mods.Select (x => x.Name + (x.IsAvailable ? " not ready: " : " ") + problems [x]).ToArray ());
            var verb = missing.All (x => x.Count == 1) ? " is" : " are";
            return names + verb + (notReady ? " not ready: " : " not available: ") + reason;
        }
    }
}
