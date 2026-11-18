using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using KRPC;

namespace TestServer
{
    /// <summary>
    /// A model of a game's frame loop, for the server to run in. Each frame runs the server's
    /// updates, then the game's own work for the frame, then sleeps out the rest of the frame
    /// if the frame limit leaves any.
    ///
    /// With a fixed rate, the updates are fixed steps, as Unity runs <c>FixedUpdate</c>: the
    /// time since the last frame goes into an accumulator, and the frame runs a step for each
    /// whole step it holds, so a frame runs zero, one or several. Without one, the server
    /// updates once a frame, which is TestServer's loop for the client tests.
    /// </summary>
    sealed class GameLoop
    {
        // The most time one frame adds to the accumulator, in seconds. KSP's default
        // PHYSICS_FRAME_DT_LIMIT. Past it the game's time runs slower than real time.
        const double MaxDeltaTime = 0.04;

        readonly long frameTicks;
        readonly long stepTicks;
        readonly long tickWorkTicks;
        readonly long frameWorkTicks;
        long accumulator;
        long lastFrame;

        public GameLoop (double frameLimit, double fixedRate, uint tickWorkMicroseconds,
                         uint frameWorkMicroseconds)
        {
            FrameLimit = frameLimit;
            FixedRate = fixedRate;
            TickWork = tickWorkMicroseconds;
            FrameWork = frameWorkMicroseconds;
            frameTicks = frameLimit > 0 ? (long)(Stopwatch.Frequency / frameLimit) : 0;
            stepTicks = fixedRate > 0 ? (long)(Stopwatch.Frequency / fixedRate) : 0;
            tickWorkTicks = Ticks (tickWorkMicroseconds);
            frameWorkTicks = Ticks (frameWorkMicroseconds);
        }

        /// <summary>The most frames a second, or zero for no limit.</summary>
        public double FrameLimit { get; private set; }

        /// <summary>Fixed steps a second, or zero to update once a frame.</summary>
        public double FixedRate { get; private set; }

        /// <summary>Microseconds of simulated work in each update.</summary>
        public uint TickWork { get; private set; }

        /// <summary>Microseconds of simulated work in each frame.</summary>
        public uint FrameWork { get; private set; }

        /// <summary>The loop, for the log.</summary>
        public string Description {
            get {
                var culture = CultureInfo.InvariantCulture;
                var text = FrameLimit > 0 ? FrameLimit.ToString (culture) + " frames/s" : "off";
                if (FixedRate > 0)
                    text += ", fixed steps at " + FixedRate.ToString (culture) + "/s";
                if (TickWork > 0)
                    text += ", " + TickWork.ToString (culture) + " us of work per update";
                if (FrameWork > 0)
                    text += ", " + FrameWork.ToString (culture) + " us of work per frame";
                return text;
            }
        }

        /// <summary>
        /// Run one frame, and return the time it took before the wait for the frame limit, in
        /// milliseconds.
        /// </summary>
        public double RunFrame (Core core)
        {
            var start = Stopwatch.GetTimestamp ();
            if (stepTicks > 0) {
                if (lastFrame != 0)
                    accumulator += Math.Min (start - lastFrame, Ticks (MaxDeltaTime * 1e6));
                while (accumulator >= stepTicks) {
                    Step (core);
                    accumulator -= stepTicks;
                }
            } else {
                Step (core);
            }
            lastFrame = start;
            Spin (frameWorkTicks);
            var busy = (Stopwatch.GetTimestamp () - start) * 1000d / Stopwatch.Frequency;
            if (frameTicks > 0)
                WaitUntil (start + frameTicks);
            core.EndFrame ();
            KRPC.Benchmarks.GameLoop.FrameEnded ();
            return busy;
        }

        void Step (Core core)
        {
            core.Update ();
            Spin (tickWorkTicks);
        }

        // Simulated work keeps the thread busy, as the game's own would
        static void Spin (long ticks)
        {
            if (ticks <= 0)
                return;
            var end = Stopwatch.GetTimestamp () + ticks;
            while (Stopwatch.GetTimestamp () < end) {
            }
        }

        // Sleep most of the remaining time, so that the process does not burn a core during
        // profiling and benchmarks, then spin to the end
        static void WaitUntil (long end)
        {
            var remaining = end - Stopwatch.GetTimestamp ();
            if (remaining > Stopwatch.Frequency / 1000) {
                var sleepMs = (int)(remaining * 1000L / Stopwatch.Frequency) - 1;
                if (sleepMs > 0)
                    Thread.Sleep (sleepMs);
            }
            while (Stopwatch.GetTimestamp () < end) {
            }
        }

        static long Ticks (double microseconds)
        {
            return (long)(microseconds * Stopwatch.Frequency / 1e6);
        }
    }
}
