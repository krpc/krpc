using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    /// <summary>
    /// Logger that writes to the game logs.
    /// </summary>
    public static class Logger
    {
        static string format = "[kRPC] {0} - {1:G} - {2}";
        static bool enabled = true;
        static Severity level = Severity.Info;

        /// <summary>
        /// Format string for log messages.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public static string Format {
            get { return format; }
            set { format = value; }
        }

        /// <summary>
        /// Whether logging is enabled.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public static bool Enabled {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>
        /// Severity of messages to output.
        /// </summary>
        public static Severity Level {
            get { return level; }
            set { level = value; }
        }

        /// <summary>
        /// Log message severity.
        /// </summary>
        [Flags]
        [Serializable]
        [SuppressMessage ("Gendarme.Rules.Naming", "UsePluralNameInEnumFlagsRule")]
        public enum Severity
        {
            /// <summary>
            /// Debug messages, for identifying bugs.
            /// </summary>
            Debug = 1,
            /// <summary>
            /// Informational messages, describing normal operation.
            /// </summary>
            Info = 2,
            /// <summary>
            /// Warning messages, describing non-critical issues.
            /// </summary>
            Warning = 3,
            /// <summary>
            /// Error messages, describing critical issues.
            /// </summary>
            Error = 4
        }

        /// <summary>
        /// Write a message to the log.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.BadPractice", "DisableDebuggingCodeRule")]
        public static void WriteLine (string message, Severity severity = Severity.Info)
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
