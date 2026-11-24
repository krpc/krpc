using System;
using System.Diagnostics;
using System.Globalization;

namespace KRPC.Utils
{
    /// <summary>
    /// How late the timed waits of the process wake. Once most recent waits wake later than
    /// <see cref="MarginTicks"/> allows for, <see cref="Spin"/> is set, and the waits spin
    /// to their deadlines rather than block. The timers of a macOS virtual machine wake
    /// several times later than asked.
    /// </summary>
    sealed class TimedWaits
    {
        /// <summary>
        /// The waits of the process.
        /// </summary>
        public static readonly TimedWaits Shared = new TimedWaits ();

        /// <summary>
        /// How far a timed wait can overshoot, in stopwatch ticks. A wait for a deadline
        /// closer than this spins. Windows times waits to its timer resolution, 15.6 ms by
        /// default.
        /// </summary>
        public static readonly long MarginTicks = Stopwatch.Frequency *
            (Environment.OSVersion.Platform == PlatformID.Win32NT ? 16 : 2) / 1000;

        // The recent waits, and whether each one woke late
        const int Samples = 32;
        readonly bool [] late = new bool [Samples];
        int next;
        int recorded;
        int lateCount;
        volatile bool spin;

        /// <summary>
        /// Whether waits spin to their deadlines.
        /// </summary>
        public bool Spin {
            get { return spin; }
        }

        /// <summary>
        /// Record a timed wait that ran to its timeout, from the stopwatch timestamps it
        /// started and ended at, and the timeout in milliseconds.
        /// </summary>
        public void Record (long start, int timeoutMs, long end)
        {
            if (spin)
                return;
            var overshoot = end - start - timeoutMs * Stopwatch.Frequency / 1000;
            lock (late) {
                if (late [next])
                    lateCount--;
                late [next] = overshoot > MarginTicks;
                if (late [next])
                    lateCount++;
                next = (next + 1) % Samples;
                if (recorded < Samples)
                    recorded++;
                if (recorded < Samples || lateCount <= Samples / 2)
                    return;
                spin = true;
            }
            Logger.WriteLine (string.Format (CultureInfo.InvariantCulture,
                "Timed waits wake late: {0} of the last {1} overshot by more than {2} ms. " +
                "The server spins in its waits from now on, which keeps a CPU core busy.",
                lateCount, Samples, MarginTicks * 1000 / Stopwatch.Frequency), Logger.Severity.Warning);
        }
    }
}
