using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using KRPC.Client.Services.TestService;

namespace KRPC.Client.Benchmark
{
    /// <summary>
    /// Benchmarks for the C# client, run by <c>//tools/benchmarks:csharp</c>.
    ///
    /// Measures what this client costs from inside it: the round trip for a procedure call,
    /// and what a call carrying a collection of values costs. The runner starts a TestServer,
    /// says in the environment where it is listening and which transport that is, and reads
    /// the JSON printed here; see tools/benchmarks/run_client.py for the contract and for what
    /// happens to these numbers afterwards.
    /// </summary>
    static class Benchmark
    {
        // Duration of one timed loop. Long enough that the clock and a stray scheduling delay
        // do not decide the answer, and short enough that a whole run stays in seconds.
        const double TargetSeconds = 0.2;

        // The number of timed loops to take.
        const int Samples = 9;

        // The duration of one discarded chunk of calls while a case is being settled, the
        // number of them compared at a time, and the margin the last few have to beat for the
        // case to count as still improving.
        const double SettleChunkSeconds = 0.1;
        const int SettleChunks = 3;
        const double SettleTolerance = 0.02;

        // The time to keep settling one case before measuring it anyway.
        const double SettleTimeoutSeconds = 10.0;

        // The number of values the collection case sends and gets back. A call carries one
        // value at a time, so the cost of encoding and decoding it is lost in the round trip.
        // A list makes that per-value cost most of what the case measures. The same count for
        // every client, so that the figures can be read against each other.
        const int ListValues = 100;

        sealed class Case
        {
            public string Name { get; set; }
            public string Unit { get; set; }
            public IList<double> Samples { get; set; }
            public string Rate { get; set; }
            public string Note { get; set; }

            // Whether the case had settled before it was measured. See
            // tools/benchmarks/run_client.py for how the runner uses it.
            public bool Settled { get; set; } = true;
        }

        // Samples for a case, and whether it had settled before they were taken.
        sealed class Timing
        {
            public IList<double> Samples { get; set; }

            public bool Settled { get; set; }
        }

        // The cost of one call once a case has stopped getting faster, and whether it got there.
        sealed class Settled
        {
            public double PerCall { get; set; }

            public bool Reached { get; set; }
        }

        static ushort Port (string name, ushort fallback)
        {
            var value = Environment.GetEnvironmentVariable (name);
            ushort port;
            return value != null && ushort.TryParse (value, out port) ? port : fallback;
        }

        // Connect over whichever transport the runner started the server with, named by socket
        // path or by port. Both are measured, as the transport is part of what a call costs.
        static Connection Connect ()
        {
            var rpcPath = Environment.GetEnvironmentVariable ("RPC_PATH");
            if (rpcPath != null) {
                return Connection.ConnectLocal (
                    "csharp_client_benchmark", rpcPath,
                    Environment.GetEnvironmentVariable ("STREAM_PATH"));
            }
            return new Connection (
                "csharp_client_benchmark",
                rpcPort: Port ("RPC_PORT", 50000),
                streamPort: Port ("STREAM_PORT", 50001));
        }

        /// <summary>
        /// Call for a short while and return the milliseconds one call took.
        /// </summary>
        static double Chunk (Action call)
        {
            var timer = Stopwatch.StartNew ();
            long calls = 0;
            while (timer.Elapsed.TotalSeconds < SettleChunkSeconds) {
                call ();
                calls++;
            }
            return timer.Elapsed.TotalMilliseconds / Math.Max (calls, 1L);
        }

        /// <summary>
        /// Make discarded calls until they stop getting faster, and return what one costs
        /// along with whether it got there.
        /// </summary>
        /// <remarks>
        /// A fixed warmup cannot know when it is done. Both ends of a round trip get faster
        /// under load for a while - the server's rate control adapts to what it is being asked
        /// for, and the runtime recompiles the paths it finds itself running - and a case
        /// measured before that finishes returns a curve rather than a cost. Every case is
        /// settled on its own, since one already warmed by the case before it says so within a
        /// few chunks. The cost of a call also falls out of the last chunk, which is what
        /// sizes the timed loops.
        /// </remarks>
        static Settled Settle (Action call)
        {
            var chunks = new List<double> { Chunk (call) };
            var timer = Stopwatch.StartNew ();
            while (timer.Elapsed.TotalSeconds < SettleTimeoutSeconds) {
                chunks.Add (Chunk (call));
                if (chunks.Count > SettleChunks) {
                    var split = chunks.Count - SettleChunks;
                    var recent = chunks.Skip (split).Min ();
                    var earlier = chunks.Take (split).Min ();
                    if (recent >= earlier * (1 - SettleTolerance))
                        return new Settled { PerCall = recent, Reached = true };
                }
            }
            // The chunks it got through, which is fewer than a settle compares when a single
            // chunk ran longer than the whole timeout.
            return new Settled {
                PerCall = chunks.Skip (Math.Max (chunks.Count - SettleChunks, 0)).Min (),
                Reached = false
            };
        }

        /// <summary>
        /// Time the call in a loop, several times over, and return milliseconds per call along
        /// with whether the case had settled before any of it was measured.
        /// </summary>
        static Timing TimedLoop (Action call)
        {
            var warm = Settle (call);
            var iterations = Math.Max ((long)(TargetSeconds * 1e3 / warm.PerCall), 1L);

            var samples = new List<double> ();
            for (var sample = 0; sample < Samples; sample++) {
                var loop = Stopwatch.StartNew ();
                for (long i = 0; i < iterations; i++)
                    call ();
                samples.Add (loop.Elapsed.TotalMilliseconds / iterations);
            }
            return new Timing { Samples = samples, Settled = warm.Reached };
        }

        static Case RoundTrip (string name, Action call)
        {
            var timing = TimedLoop (call);
            return new Case {
                Name = name, Unit = "ms", Samples = timing.Samples,
                Rate = "calls/s", Note = string.Empty, Settled = timing.Settled
            };
        }

        static void Emit (IList<Case> cases)
        {
            var out_ = new StringBuilder ("{\"results\": [");
            for (var i = 0; i < cases.Count; i++) {
                var item = cases [i];
                out_.Append (i == 0 ? string.Empty : ", ")
                    .Append ("{\"case\": \"").Append (item.Name)
                    .Append ("\", \"unit\": \"").Append (item.Unit)
                    .Append ("\", \"rate\": \"").Append (item.Rate)
                    .Append ("\", \"note\": \"").Append (item.Note)
                    .Append ("\", \"samples\": [");
                for (var j = 0; j < item.Samples.Count; j++)
                    out_.Append (j == 0 ? string.Empty : ", ")
                        .Append (item.Samples [j].ToString ("R", CultureInfo.InvariantCulture));
                out_.Append ("]");
                if (!item.Settled)
                    out_.Append (", \"settled\": false");
                out_.Append ("}");
            }
            out_.Append ("]}");
            Console.WriteLine (out_.ToString ());
        }

        public static void Main ()
        {
            using (var connection = Connect ()) {
                var testService = connection.TestService ();
                var values = new List<int> (ListValues);
                for (var i = 0; i < ListValues; i++)
                    values.Add (i);

                var cases = new List<Case> {
                    RoundTrip ("round trip", () => testService.FloatToString (3.14159f)),
                    RoundTrip ("round trip, 3 arguments",
                               () => testService.AddMultipleValues (3.14159f, 1, 2)),
                    RoundTrip (string.Format (CultureInfo.InvariantCulture,
                                              "round trip, list of {0} values", ListValues),
                               () => testService.IncrementList (values)),
                };

                Emit (cases);
            }
        }
    }
}
