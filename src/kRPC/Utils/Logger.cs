using System;

namespace KRPC.Utils
{
    static class Logger
    {
        public static bool Enabled = true;

        internal static void WriteLine (string line)
        {
            if (Enabled) {
                Console.WriteLine ("[kRPC] " + line);
            }
        }
    }
}
