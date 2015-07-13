using System;

namespace KRPC.Utils
{
    static class Logger
    {
        public static bool Enabled = true;
        public static Severity Level = Severity.Info;

        [Flags]
        internal enum Severity
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }

        internal static void WriteLine (string line, Severity severity = Severity.Info)
        {
            if (ShouldLog (severity))
                Console.WriteLine ("[kRPC] " + line);
        }

        internal static bool ShouldLog (Severity severity)
        {
            return Enabled && severity >= Level;
        }
    }
}
