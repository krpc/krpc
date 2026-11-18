using System.Collections.Generic;
using System.Diagnostics;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// Running totals of the frames the host has run, for the update loop benchmarks. The
    /// host calls <see cref="FrameEnded"/> once a frame: TestServer at the end of each frame
    /// of its game loop, and TestingTools from a MonoBehaviour's Update in game. Both call it
    /// on the game thread, which is also where the counters are read.
    /// </summary>
    public static class GameLoop
    {
        static long lastFrame;
        static ulong frames;
        static long frameTicks;
        static long maxFrameTicks;

        /// <summary>
        /// Record the end of a frame. A frame's period runs from the end of the previous one,
        /// or is <paramref name="seconds"/> where the host measures it, as the game does.
        /// </summary>
        public static void FrameEnded (double seconds = 0)
        {
            var now = Stopwatch.GetTimestamp ();
            if (lastFrame != 0) {
                var period = seconds > 0 ? (long)(seconds * Stopwatch.Frequency) : now - lastFrame;
                frames++;
                frameTicks += period;
                if (period > maxFrameTicks)
                    maxFrameTicks = period;
            }
            lastFrame = now;
        }

        /// <summary>
        /// The totals, in seconds where they are times. The longest frame is the longest since
        /// the previous read, which this resets.
        /// </summary>
        internal static IDictionary<string, double> Counters ()
        {
            var core = Core.Instance;
            var counters = new Dictionary<string, double> {
                { "time", Seconds (Stopwatch.GetTimestamp ()) },
                { "frames", frames },
                { "frame_seconds", Seconds (frameTicks) },
                { "max_frame_seconds", Seconds (maxFrameTicks) },
                { "updates", core.Updates },
                { "update_seconds", Seconds (core.UpdateTicks) },
                { "poll_seconds", Seconds (core.PollTicks) },
                { "exec_seconds", Seconds (core.ExecTicks) },
                { "stream_seconds", Seconds (core.StreamTicks) },
                { "rpcs", core.RPCsExecuted },
                // In microseconds, the working value that adaptive rate control adjusts
                { "budget_per_frame", core.BudgetPerFrame }
            };
            maxFrameTicks = 0;
            return counters;
        }

        static double Seconds (long ticks)
        {
            return (double)ticks / Stopwatch.Frequency;
        }
    }
}
