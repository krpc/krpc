using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The RealFuels state of a fuel tank: whether it is pressurized enough to feed a
    /// pressure-fed engine, and how fast its contents are boiling off. Wraps a
    /// ModuleFuelTanks, driven through the stock part module API by field name so kRPC does
    /// not need a compile-time reference to RealFuels.
    /// </summary>
    /// <remarks>
    /// Built for a single access and discarded, so the module it wraps never outlives the
    /// access that resolved it. Holding one across calls would leave it reading a module the
    /// game has replaced or torn down.
    /// </remarks>
    sealed class RealFuelsTank
    {
        readonly PartModule module;

        /// <summary>
        /// The part's RealFuels tank module, or <c>null</c> if RealFuels is not installed or
        /// the part is not one of its tanks.
        /// </summary>
        static PartModule TankModule (global::Part part)
        {
            if (!ExternalAPI.RealFuels.IsAvailable)
                return null;
            var module = part.Module ("ModuleFuelTanks");
            // Every field the adapter reads must be present before it claims the module.
            if (module == null || module.Fields ["highlyPressurized"] == null)
                return null;
            return module;
        }

        /// <summary>
        /// Whether the part is a fuel tank that RealFuels manages.
        /// </summary>
        internal static bool Is (global::Part part)
        {
            return TankModule (part) != null;
        }

        /// <summary>
        /// Returns an adapter for the part's RealFuels tank, or <c>null</c> if the part is
        /// not one.
        /// </summary>
        internal static RealFuelsTank Create (global::Part part)
        {
            var module = TankModule (part);
            return module == null ? null : new RealFuelsTank (module);
        }

        RealFuelsTank (PartModule tankModule)
        {
            module = tankModule;
        }

        /// <summary>
        /// Whether the tank is pressurized enough to feed a pressure-fed engine.
        /// </summary>
        internal bool HighlyPressurized {
            get { return (bool)module.Fields ["highlyPressurized"].GetValue (module); }
        }

        /// <summary>
        /// The rate at which the tank's contents are boiling off, in kilograms per second.
        /// </summary>
        internal double BoiloffRate {
            get {
                // RealFuels accumulates the mass boiled off during an update, in tonnes, so
                // divide by the length of that update to get a rate. It runs off the flight
                // integrator's thermal step, which is shorter than the fixed update only
                // when the game steps faster than the minimum the integrator honors, well
                // below one frame. Under time warp both scale together, so this stays a rate
                // per second of in-game time
                var delta = TimeWarp.fixedDeltaTime;
                if (delta <= 0)
                    return 0;
                return ExternalAPI.RealFuels.BoiloffMassRate (module) * 1000 / delta;
            }
        }
    }
}
