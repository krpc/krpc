using System.Diagnostics;

namespace KRPC.Utils
{
    public static class StopwatchExtensions
    {
        public static double ElapsedSeconds (this Stopwatch stopwatch)
        {
            return (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
        }
    }
}
