using System;
using System.Collections.Generic;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// What a kerbal on EVA has within reach. The module keeps track of the hatch it is
    /// standing at and the ladders it is alongside, from the trigger volumes it is inside,
    /// but keeps both to itself, so they are read by reflection.
    /// </summary>
    static class KerbalEVAExtensions
    {
        static readonly Func<KerbalEVA, global::Part> airlockPart =
            Accessor.Getter<KerbalEVA, global::Part> ("currentAirlockPart");
        static readonly Func<KerbalEVA, List<Collider>> ladderTriggers =
            Accessor.Getter<KerbalEVA, List<Collider>> ("currentLadderTriggers");

        /// <summary>
        /// The part whose hatch the kerbal is standing at, or <c>null</c> if it is not at one.
        /// </summary>
        public static global::Part CurrentAirlock (this KerbalEVA eva)
        {
            var part = airlockPart == null ? null : airlockPart (eva);
            // A destroyed part is still a reference, and only compares equal to null through
            // Unity's operator, so return it through one.
            return part == null ? null : part;
        }

        /// <summary>
        /// Whether the kerbal is alongside a ladder it could take hold of.
        /// </summary>
        public static bool LadderInReach (this KerbalEVA eva)
        {
            var triggers = ladderTriggers == null ? null : ladderTriggers (eva);
            return triggers != null && triggers.Count > 0;
        }
    }
}
