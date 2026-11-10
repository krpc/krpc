using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// How long the supplies of a vehicle or a habitat zone last its crew. Obtained from
    /// <see cref="Vehicle.LifeSupport"/> or <see cref="HabitatZone.LifeSupport"/>.
    /// </summary>
    /// <remarks>
    /// The times count down only while <see cref="KSA.LifeSupportEnabled"/> is true. Air
    /// lasts forever in breathable air outside the vehicle.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class LifeSupport : Equatable<LifeSupport>, IGameObjectState
    {
        readonly Vehicle vehicle;
        readonly HabitatZone zone;

        internal LifeSupport (Vehicle vehicle, HabitatZone zone = null)
        {
            this.vehicle = vehicle;
            this.zone = zone;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LifeSupport other)
        {
            return !ReferenceEquals (other, null) && vehicle.Equals (other.vehicle) && Equals (zone, other.zone);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return zone == null ? vehicle.GameObjectState : zone.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return zone == null ? vehicle.GetHashCode () : zone.GetHashCode ();
        }

        // Scratch space for the zone forecasts
        static readonly List<LifeSupportForecast> forecasts = new List<LifeSupportForecast> ();

        // Computed as the game's per-frame update does, which leaves the forecast empty for
        // a frame after a vehicle or a kitten on EVA appears. The vehicle's forecast is its
        // worst crewed zone's, and the game's is empty when no zone has crew
        LifeSupportForecast Forecast {
            get {
                var v = vehicle.InternalVehicle;
                var internalZone = zone?.InternalZone;
                var tree = v.Parts;
                tree.EnsureHabitatZones ();
                forecasts.Clear ();
                global::KSA.LifeSupport.ComputeZoneForecasts (tree, tree.Moles.States, null, false, v.GetOutsideAir (), forecasts);
                if (internalZone == null) {
                    var worst = global::KSA.LifeSupport.FindWorst (forecasts);
                    return worst.Kittens == 0 && forecasts.Count > 0 ? forecasts [0] : worst;
                }
                foreach (var forecast in forecasts) {
                    if (forecast.Zone == internalZone.Number)
                        return forecast;
                }
                throw new InvalidOperationException ("The habitat zone has no forecast");
            }
        }

        /// <summary>
        /// The number of kittens the supplies are shared between.
        /// </summary>
        [KRPCProperty]
        public int Kittens {
            get { return Forecast.Kittens; }
        }

        /// <summary>
        /// The mass of oxygen in the crew's tanks, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float AirMass {
            get { return Forecast.AirMass; }
        }

        /// <summary>
        /// The mass of water in the crew's tanks, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float WaterMass {
            get { return Forecast.WaterMass; }
        }

        /// <summary>
        /// The number of meals.
        /// </summary>
        [KRPCProperty]
        public int Meals {
            get { return Forecast.Meals; }
        }

        /// <summary>
        /// The time the air lasts, in seconds. Infinite with no crew, or in breathable air.
        /// </summary>
        [KRPCProperty]
        public float AirTime {
            get { return (float)Forecast.AirSeconds; }
        }

        /// <summary>
        /// The time the carbon dioxide scrubbers last, in seconds. Infinite with no crew.
        /// </summary>
        [KRPCProperty]
        public float ScrubberTime {
            get { return (float)Forecast.ScrubberSeconds; }
        }

        /// <summary>
        /// The time the water lasts, in seconds. Infinite with no crew.
        /// </summary>
        [KRPCProperty]
        public float WaterTime {
            get { return (float)Forecast.WaterSeconds; }
        }

        /// <summary>
        /// The time the food lasts, in seconds. Infinite with no crew.
        /// </summary>
        [KRPCProperty]
        public float FoodTime {
            get { return (float)Forecast.FoodSeconds; }
        }

        /// <summary>
        /// The time until the first supply runs out, in seconds. Infinite with no crew.
        /// </summary>
        [KRPCProperty]
        public float Time {
            get { return (float)Forecast.LevelSeconds; }
        }

        /// <summary>
        /// The supply that runs out first. Air when every time is infinite, as with no crew.
        /// </summary>
        [KRPCProperty]
        public LifeSupportSupply LimitingSupply {
            get {
                // In the order the game names the limit
                var forecast = Forecast;
                var seconds = forecast.LevelSeconds;
                if (forecast.AirSeconds <= seconds)
                    return LifeSupportSupply.Air;
                if (forecast.ScrubberSeconds <= seconds)
                    return LifeSupportSupply.Scrubber;
                if (forecast.WaterSeconds <= seconds)
                    return LifeSupportSupply.Water;
                return LifeSupportSupply.Food;
            }
        }

        /// <summary>
        /// How long the supplies last, as the game's life support gauge shows it.
        /// </summary>
        [KRPCProperty]
        public LifeSupportLevel Level {
            get { return (LifeSupportLevel)Forecast.Level; }
        }

        /// <summary>
        /// The time until <see cref="Level"/> next changes, in seconds. Infinite with no crew.
        /// </summary>
        [KRPCProperty]
        public float TimeToNextLevel {
            get { return (float)Forecast.SecondsToNextLevel; }
        }
    }
}
