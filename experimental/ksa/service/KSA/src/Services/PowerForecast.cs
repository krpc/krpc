using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The charge, draw and supply of the electrical circuit a part is on, and how long its
    /// batteries last. Obtained from <see cref="Part.PowerForecast"/>.
    /// </summary>
    /// <remarks>
    /// A circuit is the parts joined by connections that carry electricity. This is the
    /// forecast the game shows for a probe core.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class PowerForecast : Equatable<PowerForecast>, IGameObjectState
    {
        readonly Part part;

        internal PowerForecast (Part part)
        {
            this.part = part;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PowerForecast other)
        {
            return !ReferenceEquals (other, null) && part.Equals (other.part);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return part.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        global::KSA.PowerForecast Forecast {
            get {
                var tree = part.InternalVehicle.Parts;
                tree.ElectricalCircuits.EnsureBuilt (tree);
                return global::KSA.PowerForecast.Compute (tree, part.InternalPart);
            }
        }

        /// <summary>
        /// Whether the circuit has a battery.
        /// </summary>
        [KRPCProperty]
        public bool HasBattery {
            get { return Forecast.Drain != PowerDrain.NoBattery; }
        }

        /// <summary>
        /// The energy stored in the circuit's batteries, in joules.
        /// </summary>
        [KRPCProperty]
        public float Charge {
            get { return Forecast.Charge.Value (); }
        }

        /// <summary>
        /// The power the parts on the circuit draw, in watts.
        /// </summary>
        [KRPCProperty]
        public float Draw {
            get { return Forecast.Draw.Value (); }
        }

        /// <summary>
        /// The power the solar panels and generators on the circuit supply, in watts.
        /// </summary>
        [KRPCProperty]
        public float Supply {
            get { return Forecast.Supply.Value (); }
        }

        /// <summary>
        /// The time until the batteries run out, in seconds. Infinite while the supply meets
        /// the draw, and zero when the batteries are empty or there are none.
        /// </summary>
        [KRPCProperty]
        public float TimeLeft {
            get {
                var forecast = Forecast;
                switch (forecast.Drain) {
                case PowerDrain.Draining:
                    return (float)forecast.Remaining.Seconds ();
                case PowerDrain.NotDraining:
                    return float.PositiveInfinity;
                default:
                    return 0;
                }
            }
        }
    }
}
