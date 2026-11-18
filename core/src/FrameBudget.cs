using System;
using System.Diagnostics;
using KRPC.Utils;

namespace KRPC
{
    /// <summary>
    /// The time the server may spend in a frame: executing calls, waiting for them and
    /// updating streams. With adaptive rate control, the budget shrinks when the server's time
    /// pushes the frame rate below the target, and grows while the budget is what stops the
    /// server and the frame rate holds.
    /// </summary>
    sealed class FrameBudget
    {
        // The least time an update gets, in microseconds, so calls progress on every tick
        const uint floorMicroseconds = 1000;

        // Frames within this share of the target period meet the target
        const double targetTolerance = 0.02;

        // The frames the controller looks back over
        const int windowFrames = 16;

        // The fastest frames in the window whose average period meets the target or not:
        // three quarters of them. Under a frame limiter a game's frames vary by a few
        // milliseconds about the target, and their average holds it. Leaving out the slowest
        // quarter keeps the game's own spikes out. A load that slows most frames, or a load in
        // bursts that slows one frame in three, still moves the average.
        const int keptFrames = windowFrames * 3 / 4;


        // The frames after a cut before the next, so a cut does not compound while the window
        // catches up with it
        const int cutInterval = 8;

        // The growth per frame: an eighth of the budget, plus this many microseconds
        static readonly long growthTicks = StopwatchExtensions.MicrosecondsToTicks (100);

        bool adaptive;
        uint targetFrameRate;
        uint maxTimePerFrame;
        bool configured;

        long max;
        long floor;
        long targetPeriod;
        // The recent frames' periods and the server's time in each, in ticks, oldest
        // overwritten first, and the periods sorted, reused by each frame
        readonly long[] periods = new long [windowFrames];
        readonly long[] spents = new long [windowFrames];
        readonly long[] sorted = new long [windowFrames];
        int frames;
        int next;
        int sinceCut = cutInterval;

        /// <summary>
        /// The budget per frame, in stopwatch ticks.
        /// </summary>
        public long Budget { get; private set; }

        /// <summary>
        /// Whether the budget is at its floor.
        /// </summary>
        public bool AtFloor {
            get { return Budget <= floor; }
        }

        /// <summary>
        /// Apply the settings, each held within its bounds in <see cref="Configuration"/>. A
        /// change restarts the budget from the maximum.
        /// </summary>
        public void Configure (bool adaptiveRateControl, uint targetRate, uint maxMicroseconds)
        {
            targetRate = Clamp (targetRate, Configuration.TargetFrameRateMin, Configuration.TargetFrameRateMax);
            maxMicroseconds = Clamp (maxMicroseconds, Configuration.MaxTimePerFrameMin, Configuration.MaxTimePerFrameMax);
            if (configured && adaptive == adaptiveRateControl && targetFrameRate == targetRate &&
                maxTimePerFrame == maxMicroseconds)
                return;
            configured = true;
            adaptive = adaptiveRateControl;
            targetFrameRate = targetRate;
            maxTimePerFrame = maxMicroseconds;
            max = StopwatchExtensions.MicrosecondsToTicks (maxMicroseconds);
            floor = StopwatchExtensions.MicrosecondsToTicks (Math.Min (floorMicroseconds, maxMicroseconds));
            targetPeriod = (long)(Stopwatch.Frequency * (1 + targetTolerance) / targetRate);
            frames = 0;
            next = 0;
            sinceCut = cutInterval;
            Budget = max;
        }

        static uint Clamp (uint value, uint min, uint max)
        {
            return Math.Max (min, Math.Min (max, value));
        }

        /// <summary>
        /// The time an update may take, in ticks, given the time the frame has spent already.
        /// </summary>
        public long Allowance (long spent)
        {
            return Math.Max (floor, Budget - spent);
        }

        /// <summary>
        /// Adapt the budget to a frame that has ended.
        /// </summary>
        /// <param name="period">The frame's length, in ticks.</param>
        /// <param name="spent">The server's time in the frame, in ticks.</param>
        /// <param name="bound">Whether the budget stopped the server in the frame.</param>
        /// <param name="held">Whether a client held the tick in the frame.</param>
        public void FrameEnded (long period, long spent, bool bound, bool held)
        {
            // A held frame lasts as long as the client holding it, and shows nothing about
            // the server's own time
            if (!adaptive || held || period <= 0)
                return;
            periods [next] = period;
            spents [next] = spent;
            next = (next + 1) % windowFrames;
            if (frames < windowFrames)
                frames++;
            if (sinceCut < cutInterval)
                sinceCut++;
            Array.Copy (periods, sorted, frames);
            Array.Sort (sorted, 0, frames);
            var kept = Math.Max (1, frames * keptFrames / windowFrames);
            long keptTotal = 0;
            for (int i = 0; i < kept; i++)
                keptTotal += sorted [i];
            var typical = keptTotal / kept;
            if (typical > targetPeriod) {
                // The first cut waits for a full window after the settings are applied, so the
                // long frames of the load the server starts in do not cut the budget
                if (sinceCut < cutInterval || frames < windowFrames)
                    return;
                // Cut the server's time to its average less the frames' excess
                long total = 0;
                for (int i = 0; i < frames; i++)
                    total += spents [i];
                var average = total / frames;
                if (average > floor) {
                    Budget = Math.Max (floor, Math.Min (Budget, average - (typical - targetPeriod)));
                    sinceCut = 0;
                }
                return;
            }
            // A frame limiter or vsync hides the slack in a frame, so probe for it. A frame where
            // the server stopped short of the budget shows nothing about whether it is too small
            if (bound)
                Budget = Math.Min (max, Budget + Budget / 8 + growthTicks);
        }
    }
}
