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
        /// The total thrust and mass flow rate of the vehicle's engines at the current
        /// atmospheric pressure. Each rocket fires at the throttle it is firing at, or for full
        /// throttle each active engine with propellant at 1.
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
                var (engineThrust, engineMassFlowRate) = fullThrottle ?
                    Available (engine.Module, in engine.State, centerOfMass, pressure) :
                    Firing (vehicle, engine.Module, pressure);
                thrust += engineThrust;
                massFlowRate += engineMassFlowRate;
            }
            return (thrust, massFlowRate);
        }

        static (double thrust, double massFlowRate) Available (
            EngineController engine, in EngineControllerState state, Brutal.Numerics.float3 centerOfMass, float pressure)
        {
            var performance = engine.ComputeActivePerformance (in state, centerOfMass, pressure, 1f);
            return (performance.ThrustMax.Length (), performance.MassFlowRateMax);
        }

        // The thrust and mass flow rate of an engine's rockets at the throttles they fire at.
        // A solid rocket fires at full throttle whatever the vehicle's throttle.
        static (double thrust, double massFlowRate) Firing (Vehicle vehicle, EngineController engine, float pressure)
        {
            if (!ModuleStateful<Rocket, RocketState, EmptyStruct, EmptyStruct>.TryGetFrom (vehicle.Parts.States, out var rocketStates))
                return (0, 0);
            double thrust = 0;
            double massFlowRate = 0;
            foreach (var rocket in engine.Rockets) {
                var throttle = rocketStates.GetState (rocket).Throttle;
                if (!(throttle > 0f))
                    continue;
                var performance = RocketControllerData.ComputeFromRockets (new ReadOnlySpan<Rocket> (in rocket), pressure, throttle);
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
            var pressure = vehicle.PhysicsEnvironment.AtmosphericPressure;
            if (!fullThrottle)
                return Firing (vehicle, engine, pressure);
            return Available (engine, in State (vehicle, engine), vehicle.TotalMassPropsAsmb.Offset, pressure);
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
