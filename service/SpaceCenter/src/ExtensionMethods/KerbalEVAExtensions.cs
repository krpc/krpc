using System.Collections.Generic;
using System.Reflection;
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
        static readonly FieldInfo airlockPart = typeof (KerbalEVA).GetField (
            "currentAirlockPart", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo ladderTriggers = typeof (KerbalEVA).GetField (
            "currentLadderTriggers", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// The part whose hatch the kerbal is standing at, or <c>null</c> if it is not at one.
        /// </summary>
        public static global::Part CurrentAirlock (this KerbalEVA eva)
        {
            var part = airlockPart == null ? null : airlockPart.GetValue (eva) as global::Part;
            // A destroyed part is still a reference, and only compares equal to null through
            // Unity's operator, so return it through one.
            return part == null ? null : part;
        }

        /// <summary>
        /// Whether the kerbal is alongside a ladder it could take hold of.
        /// </summary>
        public static bool LadderInReach (this KerbalEVA eva)
        {
            var triggers = ladderTriggers == null
                ? null : ladderTriggers.GetValue (eva) as List<Collider>;
            return triggers != null && triggers.Count > 0;
        }
    }
}
