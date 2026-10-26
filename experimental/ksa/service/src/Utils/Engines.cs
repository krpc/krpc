using System;
using KSA;

namespace KRPC.KSA.Utils
{
    static class Engines
    {
        /// <summary>
        /// Standard gravity, used to convert exhaust velocity to specific impulse.
        /// </summary>
        public const double G0 = 9.80665;

        /// <summary>
        /// The total thrust and mass flow rate of the vehicle's active engines that have
        /// propellant, at the current atmospheric pressure. The throttle of each engine is
        /// its commanded throttle, or 1 for full throttle.
        /// </summary>
        public static (double thrust, double massFlowRate) Performance (Vehicle vehicle, bool fullThrottle)
        {
            if (!ModuleStateful<EngineController, EngineControllerState, EngineControllerGlobalState, EmptyStruct>.TryGetFrom (
                vehicle.Parts.States, out var states))
                return (0, 0);
            var pressure = vehicle.PhysicsEnvironment.AtmosphericPressure;
            var centerOfMass = vehicle.TotalMassPropsAsmb.Offset;
            double thrust = 0;
            double massFlowRate = 0;
            var enumerator = states.ModulesAndStates.GetEnumerator ();
            while (enumerator.MoveNext ()) {
                var engine = enumerator.Current;
                var throttle = fullThrottle ? 1f : engine.State.CommandThrottle;
                if (!(throttle > 0f))
                    continue;
                var performance = engine.Module.ComputeActivePerformance (in engine.State, centerOfMass, pressure, throttle);
                thrust += performance.ThrustMax.Length ();
                massFlowRate += performance.MassFlowRateMax;
            }
            return (thrust, massFlowRate);
        }

        /// <summary>
        /// The thrust and mass flow rate of one engine, as for <see cref="Performance(Vehicle, bool)"/>.
        /// </summary>
        public static (double thrust, double massFlowRate) Performance (Vehicle vehicle, EngineController engine, bool fullThrottle)
        {
            ref readonly var state = ref State (vehicle, engine);
            var throttle = fullThrottle ? 1f : state.CommandThrottle;
            if (!(throttle > 0f))
                return (0, 0);
            var performance = engine.ComputeActivePerformance (
                in state, vehicle.TotalMassPropsAsmb.Offset, vehicle.PhysicsEnvironment.AtmosphericPressure, throttle);
            return (performance.ThrustMax.Length (), performance.MassFlowRateMax);
        }

        public static ref readonly EngineControllerState State (Vehicle vehicle, EngineController engine)
        {
            if (!ModuleStateful<EngineController, EngineControllerState, EngineControllerGlobalState, EmptyStruct>.TryGetFrom (
                vehicle.Parts.States, out var states))
                throw new InvalidOperationException ("The vehicle has no engines");
            return ref states.GetState (engine);
        }
    }
}
