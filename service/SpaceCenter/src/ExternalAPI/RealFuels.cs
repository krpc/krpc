using System;
using System.Collections.Generic;
using System.Reflection;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// Reflective access to the parts of RealFuels that its part modules do not expose as
    /// KSPFields. Fields such as an engine's ignition count are read through the stock part
    /// module API by name; the members here are the ones with no such route, so they are bound
    /// by reflection once at startup.
    /// </summary>
    static class RealFuels
    {
        // ModuleEnginesRF
        static FieldInfo ullageSetField;
        static FieldInfo ignitionResourcesField;
        // RealFuels.Ullage.UllageSet
        static MethodInfo getUllageStabilityMethod;
        static MethodInfo getUllageProbabilityMethod;
        static MethodInfo pressureOkMethod;
        // RealFuels.Tanks.ModuleFuelTanks
        static PropertyInfo boiloffMassRateProperty;

        /// <summary>
        /// Bind to the RealFuels assembly. No version is required: the members used are checked
        /// for individually, which is a stronger test than the assembly version and lets any
        /// release that still provides them work.
        /// </summary>
        public static void Load ()
        {
            IsAvailable = false;
            var engineType = APILoader.Load (typeof(RealFuels), "RealFuels", "RealFuels.ModuleEnginesRF");
            if (engineType == null)
                return;
            var assembly = engineType.Assembly;
            var ullageSetType = assembly.GetType ("RealFuels.Ullage.UllageSet");
            var tankType = assembly.GetType ("RealFuels.Tanks.ModuleFuelTanks");
            if (ullageSetType == null || tankType == null) {
                Logger.WriteLine ("Load API: RealFuels ullage or tank type not found; skipping", Logger.Severity.Error);
                return;
            }
            ullageSetField = engineType.GetField ("ullageSet");
            ignitionResourcesField = engineType.GetField ("ignitionResources");
            getUllageStabilityMethod = ullageSetType.GetMethod ("GetUllageStability", Type.EmptyTypes);
            getUllageProbabilityMethod = ullageSetType.GetMethod ("GetUllageProbability", Type.EmptyTypes);
            pressureOkMethod = ullageSetType.GetMethod ("PressureOK", Type.EmptyTypes);
            boiloffMassRateProperty = tankType.GetProperty ("BoiloffMassRate");
            IsAvailable =
                ullageSetField != null && ignitionResourcesField != null &&
                getUllageStabilityMethod != null && getUllageProbabilityMethod != null &&
                pressureOkMethod != null && boiloffMassRateProperty != null;
            if (!IsAvailable)
                Logger.WriteLine ("Load API: RealFuels is installed but does not provide the expected members; skipping", Logger.Severity.Error);
        }

        public static bool IsAvailable { get; private set; }

        /// <summary>
        /// The engine's ullage set, which tracks how settled its propellant is and whether its
        /// tanks supply the feed pressure it needs. Null until the engine has started, so
        /// outside of flight.
        /// </summary>
        public static object UllageSet (PartModule engine)
        {
            return ullageSetField.GetValue (engine);
        }

        /// <summary>
        /// How settled the propellant feeding the engine is, from 0 to 1.
        /// </summary>
        public static double GetUllageStability (object ullageSet)
        {
            return (double)getUllageStabilityMethod.Invoke (ullageSet, null);
        }

        /// <summary>
        /// The probability that the propellant feeding the engine behaves normally, from 0 to 1.
        /// </summary>
        public static double GetUllageProbability (object ullageSet)
        {
            return (double)getUllageProbabilityMethod.Invoke (ullageSet, null);
        }

        /// <summary>
        /// Whether the engine's tanks supply the feed pressure it needs.
        /// </summary>
        public static bool PressureOk (object ullageSet)
        {
            return (bool)pressureOkMethod.Invoke (ullageSet, null);
        }

        /// <summary>
        /// The resources the engine consumes each time it ignites.
        /// </summary>
        public static IEnumerable<ModuleResource> IgnitionResources (PartModule engine)
        {
            return ignitionResourcesField.GetValue (engine) as IEnumerable<ModuleResource>;
        }

        /// <summary>
        /// The mass of propellant the tank boiled off during the last physics update, in tonnes.
        /// </summary>
        public static double BoiloffMassRate (PartModule tank)
        {
            return (double)boiloffMassRateProperty.GetValue (tank, null);
        }
    }
}
