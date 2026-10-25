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
    }
}
