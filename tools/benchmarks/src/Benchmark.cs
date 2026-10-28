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

        static bool descending;

        static bool NextIsDescending ()
        {
            descending = !descending;
            return descending;
        }
    }
}
