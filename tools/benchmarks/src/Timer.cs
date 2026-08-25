using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// Runs an operation in a tight loop and reports what one iteration of it cost: time,
    /// allocations, and the garbage collections that happened along the way.
    ///
    /// Public so that a service with cases of its own to measure - one reaching into a game
    /// object, say, which nothing here can - reports the same numbers in the same units as
    /// <see cref="Benchmark.Call"/> does.
    /// </summary>
    public static class Timer
    {
        // Number of iterations, and the time budget, for the discarded pass that runs before every
        // timed loop. Mono compiles a method on its first call, so without this the first few
        // iterations pay for the JIT. The time bound keeps the warmup short for cases where a
        // single operation is expensive (bulk proxy construction, for example).
        const int WarmupIterations = 2000;
        const double WarmupSeconds = 0.05;

        /// <summary>Where a benchmark case puts its result.</summary>
        /// <remarks>
        /// Reading a property and discarding the value lets the JIT hoist the read out of the
        /// loop, or drop it altogether, which measures nothing; a volatile store cannot be
        /// moved or elided. Nothing is meant to read these back. They are given their default
        /// value explicitly because an assembly whose cases happen not to use one would
        /// otherwise be warned that it is never written to either.
        /// </remarks>
        public static volatile int IntSink = 0;

        /// <summary>Where a benchmark case puts a boolean result. See <see cref="IntSink"/>.</summary>
        public static volatile bool BoolSink = false;

        /// <summary>Where a benchmark case puts a reference result. See <see cref="IntSink"/>.</summary>
        public static volatile object ObjectSink = null;

        // Reads the number of bytes this thread has allocated, or null if the runtime does not
        // provide it. See AllocatedBytesProbe.
        static readonly Func<long> allocatedBytes = AllocatedBytesProbe ();

        /// <summary>
        /// Run an operation the given number of times and return what it cost, per operation:
        /// nanoseconds, bytes allocated, and the number of garbage collections that happened while
        /// it ran. The caller decides the iteration count, so it can keep a single call short
        /// enough not to stall the game.
        /// </summary>
        public static IDictionary<string, double> Measure (Action operation, uint iterations)
        {
            if (operation == null)
                throw new ArgumentNullException (nameof (operation));
            if (iterations == 0)
                throw new ArgumentException ("Iterations must be at least one", nameof (iterations));

            Warmup (operation);

            var collectionsBefore = GC.CollectionCount (0);
            var bytesBefore = AllocatedBytes ();
            var timer = Stopwatch.StartNew ();
            for (uint i = 0; i < iterations; i++)
                operation ();
            timer.Stop ();
            var bytesAfter = AllocatedBytes ();
            var collections = GC.CollectionCount (0) - collectionsBefore;

            var seconds = timer.Elapsed.TotalSeconds;
            return new Dictionary<string, double> {
                { "iterations", iterations },
                { "seconds", seconds },
                { "nanoseconds_per_operation", seconds * 1e9 / iterations },
                { "bytes_per_operation", (double)(bytesAfter - bytesBefore) / iterations },
                { "collections", collections },
                // Whether the allocation figure is exact (1) or the coarse whole-heap estimate (0),
                // which a collection inside the window invalidates. See AllocatedBytes.
                { "exact_allocations", allocatedBytes != null ? 1 : 0 },
            };
        }

        static void Warmup (Action operation)
        {
            var timer = Stopwatch.StartNew ();
            for (var i = 0; i < WarmupIterations; i++) {
                operation ();
                if (timer.Elapsed.TotalSeconds > WarmupSeconds)
                    break;
            }
        }

        // Bytes allocated so far, for the allocation figures. GC.GetAllocatedBytesForCurrentThread
        // is exact and unaffected by collections, but is not guaranteed to exist on the runtime
        // KSP ships, so fall back to the size of the heap. That is coarse per operation, and
        // meaningless if a collection happens inside the window, but over a large enough loop it
        // still shows whether a path allocates at all. The one used is reported alongside the
        // figure.
        static long AllocatedBytes ()
        {
            return allocatedBytes != null ? allocatedBytes () : GC.GetTotalMemory (false);
        }

        static Func<long> AllocatedBytesProbe ()
        {
            var method = typeof (GC).GetMethod (
                "GetAllocatedBytesForCurrentThread",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (method == null)
                return null;
            try {
                var probe = (Func<long>)Delegate.CreateDelegate (typeof (Func<long>), method);
                probe ();
                return probe;
            } catch (Exception e) {
                KRPC.Utils.Logger.WriteLine (
                    "Benchmark: GC.GetAllocatedBytesForCurrentThread is unusable (" +
                    e.Message + "), measuring allocations from the heap size instead",
                    KRPC.Utils.Logger.Severity.Warning);
                return null;
            }
        }
    }
}
