using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// How long the ways of waiting on the game thread take: the latency from another thread
    /// signaling to the waiting thread running again, and how far a timed wait or a sleep
    /// overshoots what it asked for. These decide how the server can wait for a client's next
    /// call without spinning.
    /// </summary>
    static class Waits
    {
        // The time the signaling thread spins after the waiting thread releases it, so that
        // the waiting thread is blocked, and not still spinning, when the signal comes
        const double SettleSeconds = 0.0002;

        internal static readonly IList<string> Names = new [] {
            "signal", "signal, no spin", "semaphore", "timeout 1 ms", "sleep 1 ms", "sleep 0"
        };

        static readonly IDictionary<string, Func<uint, double[]>> Kinds =
            new Dictionary<string, Func<uint, double[]>> {
                { "signal", n => Signal (n, new ManualResetEventSlim (false)) },
                { "signal, no spin", n => Signal (n, new ManualResetEventSlim (false, 0)) },
                { "semaphore", Semaphore },
                { "timeout 1 ms", TimedOut },
                { "sleep 1 ms", n => Timed (n, () => Thread.Sleep (1)) },
                { "sleep 0", n => Timed (n, () => Thread.Sleep (0)) }
            };

        /// <summary>
        /// Wait the given number of times, and return the minimum, median, 99th percentile
        /// and maximum of the times taken, in microseconds.
        /// </summary>
        internal static IDictionary<string, double> Measure (string kind, uint iterations)
        {
            Func<uint, double[]> measure;
            if (!Kinds.TryGetValue (kind, out measure))
                throw new ArgumentException ("No wait named " + kind, nameof (kind));
            if (iterations == 0)
                throw new ArgumentException ("Iterations must be at least one", nameof (iterations));
            var samples = measure (iterations);
            Array.Sort (samples);
            return new Dictionary<string, double> {
                { "min_us", samples [0] },
                { "median_us", samples [samples.Length / 2] },
                { "p99_us", samples [Math.Min (samples.Length - 1, samples.Length * 99 / 100)] },
                { "max_us", samples [samples.Length - 1] }
            };
        }

        static double[] Signal (uint iterations, ManualResetEventSlim signal)
        {
            using (signal)
                return Signaled (iterations, () => signal.Reset (), () => signal.Wait (), () => signal.Set ());
        }

        static double[] Semaphore (uint iterations)
        {
            using (var semaphore = new SemaphoreSlim (0))
                return Signaled (iterations, () => { }, () => semaphore.Wait (), () => semaphore.Release ());
        }

        /// <summary>
        /// Time from a second thread signaling to this one waking, once per iteration. The
        /// second thread stamps the time just before it signals.
        /// </summary>
        static double[] Signaled (uint iterations, Action reset, Action wait, Action signal)
        {
            var samples = new double [iterations];
            var ready = new SemaphoreSlim (0);
            long signaled = 0;
            var settleTicks = (long)(SettleSeconds * Stopwatch.Frequency);
            var signaler = new Thread (() => {
                for (uint i = 0; i < iterations; i++) {
                    ready.Wait ();
                    var start = Stopwatch.GetTimestamp ();
                    while (Stopwatch.GetTimestamp () - start < settleTicks) {
                    }
                    Interlocked.Exchange (ref signaled, Stopwatch.GetTimestamp ());
                    signal ();
                }
            });
            signaler.IsBackground = true;
            signaler.Start ();
            for (uint i = 0; i < iterations; i++) {
                reset ();
                ready.Release ();
                wait ();
                var woke = Stopwatch.GetTimestamp ();
                samples [i] = Microseconds (woke - Interlocked.Read (ref signaled));
            }
            signaler.Join ();
            ready.Dispose ();
            return samples;
        }

        static double[] TimedOut (uint iterations)
        {
            using (var signal = new ManualResetEventSlim (false))
                return Timed (iterations, () => signal.Wait (1));
        }

        static double[] Timed (uint iterations, Action wait)
        {
            var samples = new double [iterations];
            for (uint i = 0; i < iterations; i++) {
                var start = Stopwatch.GetTimestamp ();
                wait ();
                samples [i] = Microseconds (Stopwatch.GetTimestamp () - start);
            }
            return samples;
        }

        static double Microseconds (long ticks)
        {
            return ticks * 1e6 / Stopwatch.Frequency;
        }
    }
}
