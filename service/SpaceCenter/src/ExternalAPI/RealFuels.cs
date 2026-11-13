using System;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// Reflective access to the parts of RealFuels that its part modules do not expose as
    /// KSPFields. Fields such as an engine's ignition count are read through the stock part
    /// module API by name; the members here are the ones with no such route, so they are bound
    /// by reflection once at startup.
    /// </summary>
    sealed class RealFuels : ModAPI
    {
        public RealFuels () : base (ServiceInfo.Mods.RealFuels.Name, ServiceInfo.Mods.RealFuels.Assembly)
        {
        }

        // ModuleEnginesRF
        static Func<PartModule, object> ullageSet;
        static Func<PartModule, IEnumerable<ModuleResource>> ignitionResources;
        static Func<PartModule, int> ignitions;
        static Func<PartModule, bool> ullage;
        static Func<PartModule, bool> pressureFed;
        // RealFuels.Ullage.UllageSet
        static Func<object, double> getUllageStability;
        static Func<object, double> getUllageProbability;
        static Func<object, bool> pressureOk;
        // RealFuels.Tanks.ModuleFuelTanks
        static Func<PartModule, double> boiloffMassRate;
        static Func<PartModule, bool> highlyPressurized;

        /// <summary>
        /// Bind to the RealFuels assembly. No version is required. A member missing from the
        /// installed release fails only the calls that use it.
        /// </summary>
        protected override void Bind (ModBinder mod)
        {
            var engineMembers = mod.Type ("RealFuels.ModuleEnginesRF");
            var ullageSetMembers = mod.Type ("RealFuels.Ullage.UllageSet");
            var tankMembers = mod.Type ("RealFuels.Tanks.ModuleFuelTanks");
            ullageSet = engineMembers.Getter<PartModule, object> ("ullageSet");
            ignitionResources = engineMembers.Getter<PartModule, IEnumerable<ModuleResource>> ("ignitionResources");
            ignitions = engineMembers.Getter<PartModule, int> ("ignitions");
            ullage = engineMembers.Getter<PartModule, bool> ("ullage");
            pressureFed = engineMembers.Getter<PartModule, bool> ("pressureFed");
            getUllageStability = ullageSetMembers.Method<Func<object, double>> ("GetUllageStability");
            getUllageProbability = ullageSetMembers.Method<Func<object, double>> ("GetUllageProbability");
            pressureOk = ullageSetMembers.Method<Func<object, bool>> ("PressureOK");
            boiloffMassRate = tankMembers.Getter<PartModule, double> ("BoiloffMassRate");
            highlyPressurized = tankMembers.Getter<PartModule, bool> ("highlyPressurized");
        }

        /// <summary>
        /// The engine's ullage set, which tracks how settled its propellant is and whether its
        /// tanks supply the feed pressure it needs. Null until the engine has started, so
        /// outside of flight.
        /// </summary>
        public static object UllageSet (PartModule engine)
        {
            return ullageSet (engine);
        }

        /// <summary>
        /// The number of ignitions the engine has left, or -1 if it has an unlimited number.
        /// </summary>
        public static int Ignitions (PartModule engine)
        {
            return ignitions (engine);
        }

        /// <summary>
        /// Whether the engine's propellant is subject to ullage.
        /// </summary>
        public static bool HasUllage (PartModule engine)
        {
            return ullage (engine);
        }

        /// <summary>
        /// Whether the engine is fed by tank pressure instead of a pump.
        /// </summary>
        public static bool PressureFed (PartModule engine)
        {
            return pressureFed (engine);
        }

        /// <summary>
        /// How settled the propellant feeding the engine is, from 0 to 1.
        /// </summary>
        public static double GetUllageStability (object ullageSet)
        {
            return getUllageStability (ullageSet);
        }

        /// <summary>
        /// The probability that the propellant feeding the engine behaves normally, from 0 to 1.
        /// </summary>
        public static double GetUllageProbability (object ullageSet)
        {
            return getUllageProbability (ullageSet);
        }

        /// <summary>
        /// Whether the engine's tanks supply the feed pressure it needs.
        /// </summary>
        public static bool PressureOk (object ullageSet)
        {
            return pressureOk (ullageSet);
        }

        /// <summary>
        /// The resources the engine consumes each time it ignites.
        /// </summary>
        public static IEnumerable<ModuleResource> IgnitionResources (PartModule engine)
        {
            return ignitionResources (engine);
        }

        /// <summary>
        /// The mass of propellant the tank boiled off during the last physics update, in tonnes.
        /// </summary>
        public static double BoiloffMassRate (PartModule tank)
        {
            return boiloffMassRate (tank);
        }

        /// <summary>
        /// Whether the tank is pressurized enough to feed a pressure-fed engine.
        /// </summary>
        public static bool HighlyPressurized (PartModule tank)
        {
            return highlyPressurized (tank);
        }
    }
}
