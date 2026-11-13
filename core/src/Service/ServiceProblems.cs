using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// The problems found while loading the services, for a server to show the player.
    /// </summary>
    public static class ServiceProblems
    {
        static readonly List<ProblemCategory> added = new List<ProblemCategory> ();
        static IList<ProblemCategory> categories;
        static string digest;

        /// <summary>
        /// Scan the services, if they have not been scanned, and log their problems. Returns
        /// false if kRPC cannot run, such as when the KRPC service is not available.
        /// </summary>
        public static bool Load ()
        {
            if (Failure != null)
                return false;
            try {
                Services.Init ();
                return true;
            } catch (Exception exn) {
                Failure = exn;
                Logger.WriteLine ("kRPC is disabled, as its services failed to load: " + exn, Logger.Severity.Error);
                return false;
            }
        }

        /// <summary>
        /// Why kRPC cannot run, or null if it can.
        /// </summary>
        public static Exception Failure { get; private set; }

        /// <summary>
        /// The errors in the services. Each left out the members it names, and the members that
        /// use them.
        /// </summary>
        public static IList<string> Errors {
            get { return Failure == null ? Services.Instance.Errors : new List<string> ().AsReadOnly (); }
        }

        /// <summary>
        /// The warnings about the services, which left their members in.
        /// </summary>
        public static IList<string> Warnings {
            get { return Failure == null ? Services.Instance.Warnings : new List<string> ().AsReadOnly (); }
        }

        /// <summary>
        /// The mod integrations whose mod is installed, and that failed to load or bound a
        /// member as a stub.
        /// </summary>
        public static IList<ModAPI> Mods {
            get { return mods; }
            internal set {
                mods = value;
                categories = null;
                digest = null;
            }
        }

        static IList<ModAPI> mods = new List<ModAPI> ().AsReadOnly ();

        /// <summary>
        /// Add a category of problems a server found itself, such as the RPCs a game update
        /// broke. It is listed after the others.
        /// </summary>
        public static void Add (ProblemCategory category)
        {
            added.Add (category);
            categories = null;
            digest = null;
        }

        /// <summary>
        /// Every problem, in categories: the members left out of each service, the other
        /// errors, the warnings, the mod integrations that failed to load, then the categories
        /// a server added. The lines of each are sorted, so the digest does not depend on the
        /// order of the scan. Empty if there are none.
        /// </summary>
        public static IList<ProblemCategory> Categories {
            get {
                if (categories == null)
                    categories = GetCategories (Failure, Errors, Warnings, Mods.Where (x => !x.IsAvailable).Select (x => x.Name + ": " + x.Reason), added);
                return categories;
            }
        }

        /// <summary>
        /// A SHA-256 digest of the problems, which changes when they do, or null if there are
        /// none. A server stores it to stop showing problems the player has seen.
        /// </summary>
        public static string Digest {
            get {
                if (digest == null)
                    digest = GetDigest (Categories);
                return digest;
            }
        }

        internal static IList<ProblemCategory> GetCategories (
            Exception failure, IEnumerable<string> errors, IEnumerable<string> warnings,
            IEnumerable<string> mods, IEnumerable<ProblemCategory> others)
        {
            var result = new List<ProblemCategory> ();
            if (failure != null)
                result.Add (new ProblemCategory ("kRPC is disabled, as its services failed to load", new [] { failure.Message }));
            foreach (var service in errors.Select (Split).GroupBy (x => x.Key).OrderBy (x => x.Key.Length == 0).ThenBy (x => x.Key, StringComparer.Ordinal)) {
                var title = service.Key.Length > 0 ? "Disabled members of " + service.Key : "Other errors";
                result.Add (new ProblemCategory (title, service.Select (x => x.Value).OrderBy (x => x, StringComparer.Ordinal).ToList ()));
            }
            var warningLines = warnings.OrderBy (x => x, StringComparer.Ordinal).ToList ();
            if (warningLines.Count > 0)
                result.Add (new ProblemCategory ("Warnings", warningLines));
            var modLines = mods.OrderBy (x => x, StringComparer.Ordinal).ToList ();
            if (modLines.Count > 0)
                result.Add (new ProblemCategory ("Disabled mod support", modLines));
            result.AddRange (others.Where (x => x.Lines.Count > 0));
            return result.AsReadOnly ();
        }

        internal static string GetDigest (IList<ProblemCategory> categories)
        {
            if (categories.Count == 0)
                return null;
            var text = string.Join ("\n", categories.SelectMany (x => new [] { x.Title }.Concat (x.Lines)).ToArray ());
            using (var sha = SHA256.Create ())
                return BitConverter.ToString (sha.ComputeHash (Encoding.UTF8.GetBytes (text))).Replace ("-", string.Empty);
        }

        /// <summary>
        /// The service a problem is in, or an empty string, and the rest of its message. The
        /// scanner opens the message with "In service Foo: " or "In service Foo, class Bar: ".
        /// </summary>
        static KeyValuePair<string, string> Split (string problem)
        {
            const string prefix = "In service ";
            var end = problem.IndexOf (": ", StringComparison.Ordinal);
            if (!problem.StartsWith (prefix, StringComparison.Ordinal) || end < 0)
                return new KeyValuePair<string, string> (string.Empty, problem);
            var context = problem.Substring (prefix.Length, end - prefix.Length).Split (new [] { ", " }, 2, StringSplitOptions.None);
            var message = problem.Substring (end + 2);
            if (context.Length > 1)
                message = context [1] + ": " + message;
            return new KeyValuePair<string, string> (context [0], message);
        }
    }

    /// <summary>
    /// A category of problems, such as the members left out of one service.
    /// </summary>
    public sealed class ProblemCategory
    {
        /// <summary>
        /// Create a category with a title, such as "Disabled members of RemoteTech", and a line
        /// for each problem.
        /// </summary>
        public ProblemCategory (string title, IList<string> lines)
        {
            Title = title;
            Lines = lines;
        }

        /// <summary>
        /// The title of the category.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// One line for each problem.
        /// </summary>
        public IList<string> Lines { get; private set; }
    }
}
