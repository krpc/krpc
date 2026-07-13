using System;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Thrown when a TestingTools --krpctest-load-* option cannot be honoured. The launch is
    /// aborted rather than silently continuing with different behaviour than was requested.
    /// </summary>
    sealed class TestingToolsException : Exception
    {
        public TestingToolsException(string message) : base(message) { }
    }

    static class Fatal
    {
        /// <summary>
        /// Report an unrecoverable auto-load failure and quit KSP.
        ///
        /// A requested --krpctest-load-* option could not be honoured. Instead of leaving the
        /// user (or the test framework) staring at a stuck load screen with no idea why the
        /// game did not launch as expected, this logs a loud, clearly delimited error, quits
        /// KSP so the launch fails fast, and returns an exception for the caller to throw so
        /// the current operation stops immediately and a stack trace is captured in the log.
        ///
        /// Call it as <c>throw Fatal.Error(...)</c> so the compiler sees the code path end.
        /// </summary>
        public static Exception Error(string message)
        {
            // Every line carries the tag so it survives krpc-run-ksp's "krpc"-only log filter
            // and shows up in the terminal, not just KSP.log.
            const string tag = "[kRPC testing tools]";
            const string rule = tag + ": ============================================================";
            Debug.LogError(
                "\n" + rule +
                "\n" + tag + ": FATAL: " + message +
                "\n" + tag + ": Quitting KSP so the launch fails fast instead of hanging at a load screen." +
                "\n" + rule);
            // Application.Quit is deferred to the end of the frame; the throw below stops the
            // current work first, then KSP quits to desktop.
            Application.Quit();
            return new TestingToolsException(message);
        }
    }
}
