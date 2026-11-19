using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Whether the supplies of a habitat zone last its crew through the vehicle's mission, as
    /// the life support window shows it. Obtained from <see cref="HabitatZone.MissionSupplies"/>.
    /// </summary>
    /// <remarks>
    /// The check takes the legs left in the mission as planned, counted from the vehicle's
    /// launch, and the kittens planned for each leg. The times are from now.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class MissionSupplies : Equatable<MissionSupplies>, IGameObjectState
    {
        readonly HabitatZone zone;

        internal MissionSupplies (HabitatZone zone)
        {
            this.zone = zone;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (MissionSupplies other)
        {
            return !ReferenceEquals (other, null) && zone.Equals (other.zone);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = zone.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return Compute (zone) == null ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return zone.GetHashCode ();
        }

        // Scratch space for the game's forecasts and zone missions
        static readonly List<LifeSupportForecast> forecasts = new List<LifeSupportForecast> ();
        static readonly List<ZoneMission> missions = new List<ZoneMission> ();

        // The zone's mission, as the life support window computes it, or null if the window
        // shows none
        internal static ZoneMission? Compute (HabitatZone zone)
        {
            if (!Universe.Rules.LifeSupport)
                return null;
            var vehicle = zone.Vehicle;
            var tree = vehicle.InternalVehicle.Parts;
            if (tree.Mission == null)
                return null;
            var legs = new Mission (vehicle).RemainingLegs;
            if (legs.Status != MissionLegsStatus.Planned || legs.Total <= UniverseTime.Zero)
                return null;
            var zones = tree.EnsureHabitatZones ();
            forecasts.Clear ();
            global::KSA.LifeSupport.ComputeZoneForecasts (
                tree, tree.Moles.States, null, false, global::KSA.LifeSupport.GetFlightAir (tree), forecasts);
            global::KSA.MissionSupplies.Compute (tree, zones, forecasts, in legs, missions);
            var internalZone = zone.InternalZone;
            for (var i = 0; i < zones.Length && i < missions.Count; i++) {
                if (ReferenceEquals (zones [i], internalZone))
                    return missions [i];
            }
            return null;
        }

        ZoneMission ZoneMission {
            get { return Compute (zone) ?? throw new InvalidOperationException ("The vehicle has no mission to check"); }
        }

        static float Seconds (UniverseTime time)
        {
            return time.IsEndOfTime () ? float.PositiveInfinity : (float)time.Seconds ();
        }

        /// <summary>
        /// Whether the supplies last the mission.
        /// </summary>
        [KRPCProperty]
        public MissionSupplyVerdict Verdict {
            get { return (MissionSupplyVerdict)ZoneMission.Verdict; }
        }

        /// <summary>
        /// The time by which the first supply to run out falls short of the end of the
        /// mission, in seconds. Zero when the supplies last.
        /// </summary>
        [KRPCProperty]
        public float ShortBy {
            get { return Seconds (ZoneMission.ShortBy); }
        }

        /// <summary>
        /// The supply that runs out first.
        /// </summary>
        [KRPCProperty]
        public LifeSupportSupply FirstSupply {
            get { return (LifeSupportSupply)ZoneMission.FirstSupply; }
        }

        /// <summary>
        /// The time until a supply runs out with the crew planned for each leg, in seconds.
        /// Infinite if it lasts the mission.
        /// </summary>
        /// <param name="supply">The supply.</param>
        /// <remarks>
        /// Air lasts while the vehicle is in breathable air.
        /// </remarks>
        [KRPCMethod]
        public float TimeLeft (LifeSupportSupply supply)
        {
            var mission = ZoneMission;
            switch (supply) {
            case LifeSupportSupply.Air:
                return Seconds (mission.AirEnds);
            case LifeSupportSupply.Scrubber:
                return Seconds (mission.ScrubberEnds);
            case LifeSupportSupply.Water:
                return Seconds (mission.WaterEnds);
            case LifeSupportSupply.Food:
                return Seconds (mission.FoodEnds);
            default:
                throw new ArgumentException ("Unknown supply");
            }
        }

        /// <summary>
        /// The time until the zone has no crew left in the mission, in seconds.
        /// </summary>
        [KRPCProperty]
        public float End {
            get { return Seconds (ZoneMission.End); }
        }
    }
}
