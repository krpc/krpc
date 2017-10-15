using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    static class Logger
    {
        static string format = "[kRPC] {0} - {1:G} - {2}";
        static bool enabled = true;
        static Severity level = Severity.Info;

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public static string Format {
            get { return format; }
            set { format = value; }
        }

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
        internal static void WriteLine (string message, Severity severity = Severity.Info)
        {
            if (ShouldLog (severity))
                Console.WriteLine (string.Format (format, DateTime.Now, severity, message));
        }

        internal static bool ShouldLog (Severity severity)
        {
            return Enabled && severity >= Level;
        }
    }
}
