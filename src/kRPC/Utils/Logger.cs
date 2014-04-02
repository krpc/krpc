using System;
using UnityEngine;

namespace KRPC.Utils
{
    static class Logger
    {
        internal static void WriteLine (string line)
        {
            line = "[kRPC] " + line;
            try {
                Debug.Log (line);
            } catch {
                Console.WriteLine (line);
            }
        }
    }
}

