using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    static class Logger
    {
        static bool enabled = true;
        static Severity level = Severity.Info;

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public static bool Enabled {
            get { return enabled; }
            set { enabled = value; }
        }

        public static Severity Level {
            get { return level; }
            set { level = value; }
        }

        [Flags]
        [Serializable]
        [SuppressMessage ("Gendarme.Rules.Naming", "UsePluralNameInEnumFlagsRule")]
        internal enum Severity
        {
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4
        }

        [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        internal static void WriteLine (string line, Severity severity = Severity.Info)
        {
            if (ShouldLog (severity))
                Console.WriteLine ("[kRPC] [" + severity + "] " + line);
        }

        internal static bool ShouldLog (Severity severity)
        {
            return Enabled && severity >= Level;
        }
    }
}
