using System.Diagnostics;

namespace KRPC.Utils
{
    static class StopwatchExtensions
    {
        static readonly double ticksToSeconds = 1d / Stopwatch.Frequency;
        static readonly double microsecondsToTicks = Stopwatch.Frequency / 1000000d;

        public static double ElapsedSeconds (this Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * ticksToSeconds;
        }

        public static long MicrosecondsToTicks (long microseconds)
        {
            return (long)(microseconds * microsecondsToTicks);
        }
    }
}
