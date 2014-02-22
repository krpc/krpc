using System;

namespace KRPC.Utils
{
    static class Logger
    {
        internal static void WriteLine (string line)
        {
            Console.WriteLine ("[kRPC] " + line);
        }
    }
}

