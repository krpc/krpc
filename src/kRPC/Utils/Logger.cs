using System;

namespace KRPC.Utils
{
    class Logger
    {
        internal static void WriteLine (string line)
        {
            Console.WriteLine ("[kRPC] " + line);
        }
    }
}

