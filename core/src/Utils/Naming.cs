using System.Linq;
using System.Text.RegularExpressions;

namespace KRPC.Utils
{
    /// <summary>
    /// Converts names to each case, as the client design page specifies.
    /// </summary>
    static class Naming
    {
        // The digits a word takes
        const string digits = "(?:[0-9]+(?![0-9])(?![A-Z]+(?![a-z]))[a-z]*)?";
        // The words of a name, by the rules on the client design page
        static readonly Regex word = new Regex (
            "(?:UInt|ULong|SInt|FoV)(?![a-z])" + digits +
            "|[A-Z]{2,}s(?![a-z])" + digits +
            "|[A-Z]+(?![a-z])" + digits +
            "|[A-Z]?[a-z]+" + digits +
            "|[0-9]+(?:[A-Z]+(?![a-z])|[a-z]+)?" +
            "|.",
            RegexOptions.CultureInvariant);

        static string[] Words (string name)
        {
            return word.Matches (name).Cast<Match> ().Select (x => x.Value).ToArray ();
        }

        /// <summary>
        /// A name in snake case, e.g. GetServices is get_services.
        /// </summary>
        public static string SnakeCase (string name)
        {
            return string.Join ("__", name.Split ('_').Select (part => string.Join ("_", Words (part)).ToLowerInvariant ()));
        }

        /// <summary>
        /// A name in upper snake case, e.g. GetServices is GET_SERVICES.
        /// </summary>
        public static string UpperSnakeCase (string name)
        {
            return SnakeCase (name).ToUpperInvariant ();
        }
    }
}
