using System;
using System.Globalization;

namespace KRPC.Utils
{
    /// <summary>
    /// Logger that writes to the game logs.
    /// </summary>
    public static class Logger
    {
        static string format = "[kRPC] {1:G} - {2}";
        static bool enabled = true;
        static Severity level = Severity.Info;

        /// <summary>
        /// Format string for log messages, given the time, the severity and the message. The
        /// default leaves out the time, which the game's log adds.
        /// </summary>
        public static string Format {
            get { return format; }
            set { format = value; }
        }

        /// <summary>
        /// Whether logging is enabled.
        /// </summary>
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
        /// Delegate that performs the actual log output. Defaults to Console.WriteLine.
        /// Replace this in the KSP plugin entry point to route through UnityEngine.Debug.
        /// </summary>
        public static Action<string, Severity> Writer { get; set; } =
            (msg, _) => Console.WriteLine (msg);

        /// <summary>
        /// Write a message to the log.
        /// </summary>
        public static void WriteLine (string message, Severity severity = Severity.Info)
        {
            if (ShouldLog (severity))
                Writer (string.Format (CultureInfo.InvariantCulture, format, DateTime.Now, severity, message), severity);
        }

        /// <summary>
        /// Whether a message of the given severity is written, to skip building one that is not.
        /// </summary>
        public static bool ShouldLog (Severity severity)
        {
            return Enabled && severity >= Level;
        }
    }
}
