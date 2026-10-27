using System;
using System.IO;

namespace KRPC.Client
{
    /// <summary>
    /// A fault that fails a write to the connection, as a failed socket write does. Only the
    /// conformance testee sets the hook, by reflection.
    /// </summary>
    static class WriteFault
    {
        // Called before each write, returning the message of the error to fail it with, or null.
        // Set by reflection, which the compiler cannot see.
#pragma warning disable 0649
        static volatile Func<string> hook;
#pragma warning restore 0649

        /// <summary>
        /// Throws the error of an armed fault, if the hook arms one.
        /// </summary>
        internal static void Reach ()
        {
            var current = hook;
            if (current == null)
                return;
            var message = current ();
            if (message != null)
                throw new IOException (message);
        }
    }
}
