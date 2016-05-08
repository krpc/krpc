using System;
using System.Diagnostics;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class PerformanceTest : ServerTestCase
    {
        [Test]
        public void Performance ()
        {
            int n = 100;
            Stopwatch stopWatch = new Stopwatch ();
            stopWatch.Start ();
            var testService = connection.TestService ();
            for (int i = 0; i < n; i++)
                testService.FloatToString (3.14159f);
            stopWatch.Stop ();
            double t = ((double)stopWatch.ElapsedMilliseconds / 1000.0);
            Console.WriteLine ("Total execution time: " + t + " seconds");
            Console.WriteLine ("RPC execution rate: " + (int)(n / t) + " per second");
            Console.WriteLine ("Latency: " + ((t * 1000) / n) + " milliseconds");
        }
    }
}
