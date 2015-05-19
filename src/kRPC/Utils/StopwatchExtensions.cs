using System.Diagnostics;

namespace KRPC.Utils
{
    public static class StopwatchExtensions
    {
        public static double ElapsedSeconds (this Stopwatch stopwatch)
        {
            return (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
        }

        public static long MicrosecondsToTicks (long microseconds)
        {
            const double mult = (double)Stopwatch.Frequency / 1000000d;
            return (long)((double)microseconds * mult);
        }
    }
}
