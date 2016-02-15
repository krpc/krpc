using System.Diagnostics;

namespace KRPC.Utils
{
    static class StopwatchExtensions
    {
        static readonly double ticksToSeconds = 1d / (double)Stopwatch.Frequency;
        static readonly double microsecondsToTicks = (double)Stopwatch.Frequency / 1000000d;

        public static double ElapsedSeconds (this Stopwatch stopwatch)
        {
            return (double)stopwatch.ElapsedTicks * ticksToSeconds;
        }

        public static long MicrosecondsToTicks (long microseconds)
        {
            return (long)((double)microseconds * microsecondsToTicks);
        }
    }
}
