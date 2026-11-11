using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change to a resonant orbit, to place satellites in a constellation.
    /// </summary>
    /// <remarks>
    /// Start from an orbit in the plane of the constellation. To deploy satellites, set the
    /// denominator to their number. A numerator one less than the denominator is the cheapest,
    /// though not the fastest. The two numbers must have no common factor.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationResonantOrbit : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Value<int> resonanceNumerator;
        static Value<int> resonanceDenominator;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationResonantOrbit");
            timeSelector = TimeSelectorField (type);
            resonanceNumerator = Reflection.EditableInt (type, "ResonanceNumerator");
            resonanceDenominator = Reflection.EditableInt (type, "ResonanceDenominator");
        }

        internal OperationResonantOrbit () : base ("OperationResonantOrbit")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The numerator of the ratio of the new orbital period to the current one.
        /// </summary>
        [KRPCProperty]
        public int ResonanceNumerator {
            get { return resonanceNumerator.Get (Instance); }
            set { resonanceNumerator.Set (Instance, value); }
        }

        /// <summary>
        /// The denominator of the ratio of the new orbital period to the current one.
        /// </summary>
        [KRPCProperty]
        public int ResonanceDenominator {
            get { return resonanceDenominator.Get (Instance); }
            set { resonanceDenominator.Set (Instance, value); }
        }
    }
}
