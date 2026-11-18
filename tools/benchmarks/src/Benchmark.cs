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
    [KRPCService (GameScene = GameScene.All)]
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

        /// <summary>
        /// Run one step of the server's path for a procedure call the given number of times,
        /// and return what one step cost, as <see cref="Call"/> does.
        /// </summary>
        /// <param name="call">The procedure call. Any procedure of any service.</param>
        /// <param name="step">The step, from <see cref="CallSteps"/>.</param>
        /// <param name="iterations">Number of times to run the step.</param>
        [KRPCProcedure]
        public static IDictionary<string, double> CallStep (ProcedureCall call, string step, uint iterations)
        {
            return Steps.Measure (call, step, iterations);
        }

        /// <summary>
        /// The steps that <see cref="CallStep"/> accepts.
        /// </summary>
        [KRPCProperty]
        public static IList<string> CallSteps {
            get { return Steps.Names; }
        }

        /// <summary>
        /// Update a stream of a procedure call the given number of times, and return what one
        /// update cost: nanoseconds, bytes allocated, and the number of garbage collections
        /// that happened while it ran.
        /// </summary>
        /// <param name="call">The procedure call to stream. Any procedure of any service.</param>
        /// <param name="iterations">Number of times to update the stream.</param>
        [KRPCProcedure]
        public static IDictionary<string, double> StreamUpdate (ProcedureCall call, uint iterations)
        {
            return StreamUpdates.Measure (call, iterations);
        }

        /// <summary>
        /// Running totals of the frames the host has run and the time the server has spent in
        /// its updates, for the update loop benchmarks. Times are in seconds, apart from
        /// <c>budget_per_frame</c>, the server's current budget, in microseconds.
        /// </summary>
        /// <remarks>
        /// <c>max_frame_seconds</c> is the longest frame since the previous call.
        /// </remarks>
        [KRPCProcedure]
        public static IDictionary<string, double> LoopCounters ()
        {
            return GameLoop.Counters ();
        }

        /// <summary>
        /// Wait in one of the ways the server could wait for a call, the given number of times,
        /// and return the minimum, median, 99th percentile and maximum of the times taken, in
        /// microseconds.
        /// </summary>
        /// <param name="kind">The way to wait, from <see cref="WaitKinds"/>.</param>
        /// <param name="iterations">Number of times to wait.</param>
        [KRPCProcedure]
        public static IDictionary<string, double> Wait (string kind, uint iterations)
        {
            return Waits.Measure (kind, iterations);
        }

        /// <summary>
        /// The ways of waiting that <see cref="Wait"/> accepts.
        /// </summary>
        [KRPCProperty]
        public static IList<string> WaitKinds {
            get { return Waits.Names; }
        }

        /// <summary>
        /// A string of the given number of characters, different on every call, for a stream
        /// that sends an update on every tick.
        /// </summary>
        /// <param name="size">The number of characters.</param>
        [KRPCProcedure]
        public static string Payload (uint size)
        {
            payloads++;
            var prefix = payloads.ToString (System.Globalization.CultureInfo.InvariantCulture) + " ";
            if (prefix.Length >= size)
                return prefix.Substring (0, (int)size);
            return prefix + new string ('x', (int)size - prefix.Length);
        }

        static ulong payloads;

        /// <summary>
        /// A new list of the given number of strings, the same on every call.
        /// </summary>
        /// <param name="size">The number of elements.</param>
        [KRPCProcedure]
        public static IList<string> NewList (uint size)
        {
            var list = new List<string> ((int)size);
            for (uint i = 0; i < size; i++)
                list.Add (Name (i));
            return list;
        }

        // A new string on each call, as the runtime caches the strings of small integers
        static string Name (uint i)
        {
            return "item " + i.ToString (System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// A new set of the integers from 0 to one less than the given size. Alternate calls
        /// add the elements in the opposite order.
        /// </summary>
        /// <param name="size">The number of elements.</param>
        [KRPCProcedure]
        public static HashSet<int> NewSet (uint size)
        {
            var set = new HashSet<int> ();
            var descending = NextIsDescending ();
            for (uint i = 0; i < size; i++)
                set.Add ((int)(descending ? size - 1 - i : i));
            return set;
        }

        /// <summary>
        /// A new dictionary from a string for each of the integers from 0 to one less than the
        /// given size to the integer. Alternate calls add the entries in the opposite order.
        /// </summary>
        /// <param name="size">The number of entries.</param>
        [KRPCProcedure]
        public static IDictionary<string, int> NewDictionary (uint size)
        {
            var dictionary = new Dictionary<string, int> ();
            var descending = NextIsDescending ();
            for (uint i = 0; i < size; i++) {
                var value = descending ? size - 1 - i : i;
                dictionary [Name (value)] = (int)value;
            }
            return dictionary;
        }

        /// <summary>
        /// The sum of the arguments, for the scalar case of <see cref="CallStep"/>.
        /// </summary>
        /// <param name="x">A float.</param>
        /// <param name="y">An integer.</param>
        /// <param name="z">A long.</param>
        [KRPCProcedure]
        public static double Add (float x, int y, long z)
        {
            return x + y + z;
        }

        /// <summary>
        /// The string passed.
        /// </summary>
        /// <param name="value">The string.</param>
        [KRPCProcedure]
        public static string EchoString (string value)
        {
            return value;
        }

        /// <summary>
        /// The list passed.
        /// </summary>
        /// <param name="value">The list.</param>
        [KRPCProcedure]
        public static IList<int> EchoList (IList<int> value)
        {
            return value;
        }

        /// <summary>
        /// The dictionary passed.
        /// </summary>
        /// <param name="value">The dictionary.</param>
        [KRPCProcedure]
        public static IDictionary<string, int> EchoDictionary (IDictionary<string, int> value)
        {
            return value;
        }

        /// <summary>
        /// A new object.
        /// </summary>
        [KRPCProcedure]
        public static BenchmarkObject NewObject ()
        {
            return new BenchmarkObject ();
        }

        /// <summary>
        /// The object passed.
        /// </summary>
        /// <param name="value">The object.</param>
        [KRPCProcedure]
        public static BenchmarkObject EchoObject (BenchmarkObject value)
        {
            return value;
        }

        /// <summary>
        /// The list of objects passed.
        /// </summary>
        /// <param name="value">The list.</param>
        [KRPCProcedure]
        public static IList<BenchmarkObject> EchoObjects (IList<BenchmarkObject> value)
        {
            return value;
        }

        static bool descending;

        static bool NextIsDescending ()
        {
            descending = !descending;
            return descending;
        }
    }

    /// <summary>
    /// An object for the object cases of <see cref="Benchmark.CallStep"/>.
    /// </summary>
    [KRPCClass (Service = "Benchmark")]
    public sealed class BenchmarkObject
    {
    }
}
