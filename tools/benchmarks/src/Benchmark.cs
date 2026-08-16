using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;

namespace KRPC.Benchmarks
{
    /// <summary>
    /// Server-side benchmarking.
    ///
    /// This assembly is a service like any other, and is loaded by whichever server it is
    /// installed alongside: TestServer, for a measurement that needs no game and takes
    /// seconds, and the game, for the same measurement against a real service. So a number
    /// taken one way is the same measurement as a number taken the other, and a change to the
    /// server can be prototyped against TestServer and confirmed in game.
    ///
    /// It does not ship with the mod. The scripts that drive it live in
    /// <c>tools/benchmarks/</c>.
    /// </summary>
    [KRPCService (Id = 9998, GameScene = GameScene.All)]
    public static class Benchmark
    {
        /// <summary>
        /// Run a procedure call through the server's call path the given number of times, and
        /// return what one call cost: nanoseconds, bytes allocated, and the number of garbage
        /// collections that happened while it ran.
        /// </summary>
        /// <param name="call">The procedure call to make. Any procedure of any service.</param>
        /// <param name="iterations">Number of times to make the call.</param>
        [KRPCProcedure]
        public static IDictionary<string, double> Call (ProcedureCall call, uint iterations)
        {
            return Dispatch.MeasureCall (call, iterations);
        }
    }
}
