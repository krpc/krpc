using System;

namespace KRPC.Utils
{
    public class Logger
    {
        internal static void WriteLine (string line)
        {
            Console.WriteLine ("[kRPC] " + line);
        }
    }
}

