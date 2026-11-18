using System;
using System.Diagnostics;
using System.Threading;

namespace KRPC.Server
{
    /// <summary>
    /// The event the transport threads set when bytes arrive from a client, or a client's
    /// connection ends. The server's update waits on it between polls.
    /// </summary>
    static class Arrivals
    {
        // No spinning before the wait blocks. KSP's Mono spins for about 150 us before a
        // ManualResetEventSlim with the default spin count wakes, against 5 us with none.
        static readonly ManualResetEventSlim signal = new ManualResetEventSlim (false, 0);

        // How far a timed wait can overshoot, in stopwatch ticks. A wait for a deadline closer
        // than this spins. Windows times waits to its timer resolution, 15.6 ms by default.
        static readonly long overshootTicks = Stopwatch.Frequency *
            (Environment.OSVersion.Platform == PlatformID.Win32NT ? 16 : 2) / 1000;

        /// <summary>
        /// Signal that bytes arrived, or a connection ended. Called from any thread.
        /// </summary>
        public static void Set ()
        {
            signal.Set ();
        }

        /// <summary>
        /// Clear the signal, before polling the clients, so that an arrival during the poll
        /// is not missed.
        /// </summary>
        public static void Reset ()
        {
            signal.Reset ();
        }

        /// <summary>
        /// Wait until the signal is set or the deadline passes, and return whether it was set.
        /// The deadline is a <see cref="Stopwatch.GetTimestamp"/> value.
        /// </summary>
        public static bool WaitUntil (long deadline)
        {
            while (!signal.IsSet) {
                var remaining = deadline - Stopwatch.GetTimestamp ();
                if (remaining <= 0)
                    return false;
                var block = remaining - overshootTicks;
                if (block > 0)
                    signal.Wait ((int)Math.Max (1, block * 1000 / Stopwatch.Frequency));
            }
            return true;
        }
    }
}
