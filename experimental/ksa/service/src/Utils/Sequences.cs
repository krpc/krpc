using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSA;

namespace KRPC.KSA.Utils
{
    static class Sequences
    {
        // The game recomputes sequence performance in flight only while its sequence or engine
        // windows are open, so we recompute it, at most once per game time. The solver steps
        // also recompute it, and the server runs between them.
        static readonly ConditionalWeakTable<PartTree, Box> recomputed = new ConditionalWeakTable<PartTree, Box> ();

        sealed class Box
        {
            public double Time = double.NaN;
        }

        static ReadOnlySpan<SequencePerformance> Performances (Vehicle vehicle)
        {
            var parts = vehicle.Parts;
            var time = Universe.GetElapsedTime ().Seconds ();
            var box = recomputed.GetOrCreateValue (parts);
            if (box.Time != time || parts.PerformanceSequences.IsDirty) {
                parts.PerformanceSequences.RecomputeForFlight (vehicle.PhysicsEnvironment.AtmosphericPressure);
                box.Time = time;
            }
            return parts.PerformanceSequences.PerformanceSequences;
        }

        /// <summary>
        /// The numbers of the vehicle's sequences, in activation order.
        /// </summary>
        public static List<int> Numbers (Vehicle vehicle)
        {
            var result = new List<int> ();
            foreach (var sequence in vehicle.Parts.SequenceList.Sequences)
                result.Add (sequence.Number);
            return result;
        }

        /// <summary>
        /// The sequence with the given number. Throws an exception if the vehicle no longer
        /// has it.
        /// </summary>
        public static global::KSA.Sequence Find (Vehicle vehicle, int number)
        {
            return vehicle.Parts.SequenceList.Sequences[Index (vehicle, number)];
        }

        /// <summary>
        /// The predicted performance of the sequence with the given number.
        /// </summary>
        public static SequencePerformance Performance (Vehicle vehicle, int number)
        {
            var index = Index (vehicle, number);
            var performances = Performances (vehicle);
            return index < performances.Length ? performances[index] : default;
        }

        /// <summary>
        /// The total change in velocity of the vehicle's sequences.
        /// </summary>
        public static float TotalDeltaV (Vehicle vehicle)
        {
            Performances (vehicle);
            return vehicle.Parts.PerformanceSequences.TotalDeltaV;
        }

        static int Index (Vehicle vehicle, int number)
        {
            var sequences = vehicle.Parts.SequenceList.Sequences;
            for (var i = 0; i < sequences.Length; i++) {
                if (sequences[i].Number == number)
                    return i;
            }
            throw new InvalidOperationException ("The vehicle no longer has sequence " + number);
        }
    }
}
