using System.Collections.Generic;
using System.Linq;

namespace BrokenProceduresFixture
{
    /// <summary>
    /// Procedures built against version 1 of the library, and run against version 2. It has no
    /// kRPC attributes, so the scans in the other tests leave it out.
    /// </summary>
    public static class Procedures
    {
        /// <summary>
        /// Calls the removed method.
        /// </summary>
        public static int Direct ()
        {
            return BrokenProceduresLib.Lib.Removed ();
        }

        /// <summary>
        /// Calls the removed method through a helper.
        /// </summary>
        public static int Indirect ()
        {
            return Helper () + 1;
        }

        /// <summary>
        /// Calls the removed method from an iterator.
        /// </summary>
        public static IList<int> InAnIterator ()
        {
            return Values ().ToList ();
        }

        /// <summary>
        /// Calls the kept method.
        /// </summary>
        public static int Kept ()
        {
            return BrokenProceduresLib.Lib.Kept ();
        }

        static int Helper ()
        {
            return BrokenProceduresLib.Lib.Removed ();
        }

        static IEnumerable<int> Values ()
        {
            yield return BrokenProceduresLib.Lib.Removed ();
        }
    }
}
