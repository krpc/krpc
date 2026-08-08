using System.Collections.Generic;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The RealFuels state of an engine: how many ignitions it has left, how settled its
    /// propellant is, and whether its tanks supply the feed pressure it needs. Wraps a
    /// ModuleEnginesRF, driven through the stock part module API by field name so kRPC does
    /// not need a compile-time reference to RealFuels.
    /// </summary>
    /// <remarks>
    /// Built for a single access and discarded, so the module it wraps never outlives the
    /// access that resolved it. Holding one across calls would leave it reading a module the
    /// game has replaced or torn down.
    /// </remarks>
    sealed class RealFuelsEngine
    {
        readonly PartModule module;

        /// <summary>
        /// Whether RealFuels manages the given engine module, and provides everything the
        /// adapter reads from it.
        /// </summary>
        internal static bool Is (ModuleEngines engine)
        {
            return ExternalAPI.RealFuels.IsAvailable &&
                // Nothing in RealFuels derives from ModuleEnginesRF, so an exact type name
                // match finds every engine it manages.
                engine.GetType ().Name == "ModuleEnginesRF" &&
                // Every field the adapter reads must be present before it claims the module.
                engine.Fields ["ignitions"] != null &&
                engine.Fields ["ullage"] != null &&
                engine.Fields ["pressureFed"] != null;
        }

        /// <summary>
        /// Returns an adapter for the given engine module, or <c>null</c> if RealFuels does
        /// not manage it.
        /// </summary>
        internal static RealFuelsEngine Create (ModuleEngines engine)
        {
            return Is (engine) ? new RealFuelsEngine (engine) : null;
        }

        RealFuelsEngine (PartModule engineModule)
        {
            module = engineModule;
        }

        /// <summary>
        /// The number of ignitions the engine has left, or -1 if it has an unlimited number.
        /// </summary>
        internal int Ignitions {
            get { return (int)module.Fields ["ignitions"].GetValue (module); }
        }

        /// <summary>
        /// The resources the engine consumes each time it ignites, keyed by resource name,
        /// with the amount required of each.
        /// </summary>
        internal IDictionary<string, double> IgnitionResources {
            get {
                var result = new Dictionary<string, double> ();
                var resources = ExternalAPI.RealFuels.IgnitionResources (module);
                if (resources == null)
                    return result;
                foreach (var resource in resources)
                    result [resource.name] = resource.amount;
                return result;
            }
        }

        /// <summary>
        /// Whether the engine's propellant is subject to ullage.
        /// </summary>
        internal bool HasUllage {
            get { return (bool)module.Fields ["ullage"].GetValue (module); }
        }

        /// <summary>
        /// Whether the engine is fed by tank pressure instead of a pump.
        /// </summary>
        internal bool PressureFed {
            get { return (bool)module.Fields ["pressureFed"].GetValue (module); }
        }

        /// <summary>
        /// How settled the propellant feeding the engine is, from 0 to 1.
        /// </summary>
        internal double PropellantStability {
            get {
                var ullageSet = UllageSet;
                // Fully settled is the state the simulator starts in, and the state an engine
                // that has never run is in.
                return ullageSet == null ? 1 : ExternalAPI.RealFuels.GetUllageStability (ullageSet);
            }
        }

        /// <summary>
        /// The probability that the propellant feeding the engine behaves normally, from 0 to 1.
        /// </summary>
        internal double PropellantReliability {
            get {
                var ullageSet = UllageSet;
                return ullageSet == null ? 1 : ExternalAPI.RealFuels.GetUllageProbability (ullageSet);
            }
        }

        /// <summary>
        /// Whether the engine's tanks supply the feed pressure it needs.
        /// </summary>
        internal bool HasFeedPressure {
            get {
                var ullageSet = UllageSet;
                // Without an ullage set there is no tank information, and only a pressure-fed
                // engine can be short of feed pressure.
                return ullageSet == null ? !PressureFed : ExternalAPI.RealFuels.PressureOk (ullageSet);
            }
        }

        /// <summary>
        /// The engine's ullage set. RealFuels creates it when the engine starts, so it is
        /// present throughout flight but not in the editor. It is read on each access rather
        /// than cached, as the engine may not have started when the adapter was created.
        /// </summary>
        object UllageSet {
            get { return ExternalAPI.RealFuels.UllageSet (module); }
        }
    }
}
