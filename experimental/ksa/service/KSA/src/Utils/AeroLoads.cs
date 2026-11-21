using System;
using System.Runtime.CompilerServices;
using BepuUtilities.Memory;
using Brutal.Numerics;
using KSA;
using KSA.Aero;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// The aerodynamic force and torque on a vehicle, computed as the game's physics step
    /// computes them.
    /// </summary>
    static class AeroLoads
    {
        // The game's surface flow caches the shielding for the last flow direction, and the
        // physics step updates it from worker threads. We keep a flow of our own over the
        // same surface, rebuilt when the game rebuilds the vehicle's aerodynamic mesh.
        sealed class Flow
        {
            public AeroMesh Mesh;
            public BufferPool Pool;
            public AeroSurfaceFlow SurfaceFlow;

            // The pool's native memory, once the vehicle's aerodynamics are collected
            ~Flow ()
            {
                Pool?.Clear ();
            }
        }

        static readonly ConditionalWeakTable<Aerodynamics, Flow> flows = new ConditionalWeakTable<Aerodynamics, Flow> ();

        static AeroSurfaceFlow GetSurfaceFlow (Vehicle vehicle)
        {
            var aerodynamics = vehicle.Parts?.AwaitAerodynamics ();
            var mesh = aerodynamics?.Mesh;
            if (mesh == null || mesh.Surface.FaceCount == 0)
                return null;
            var flow = flows.GetValue (aerodynamics, _ => new Flow ());
            if (!ReferenceEquals (flow.Mesh, mesh)) {
                flow.Pool?.Clear ();
                flow.Mesh = mesh;
                flow.Pool = new BufferPool ();
                flow.SurfaceFlow = new AeroSurfaceFlow (mesh.Surface, flow.Pool);
            }
            return flow.SurfaceFlow;
        }

        /// <summary>
        /// The aerodynamic force and torque on a vehicle, in its body axes, for air moving
        /// past it at the given velocity in its body axes.
        /// </summary>
        public static void Compute (
            Vehicle vehicle, PhysicalAtmosphere atmosphere, double density, double temperature, double speedOfSound,
            double3 airVelocityBody, double3 bodyRates, out double3 forceBody, out double3 torqueBody)
        {
            forceBody = double3.Zero;
            torqueBody = double3.Zero;
            var speedSquared = airVelocityBody.LengthSquared ();
            var dynamicPressure = 0.5 * density * speedSquared;
            if (atmosphere == null || dynamicPressure.IsNearlyZero () || speedOfSound <= 0)
                return;
            var speed = Math.Sqrt (speedSquared);
            ref readonly var props = ref vehicle.Props;
            var flowDirection = -airVelocityBody / speed;
            // The physics step applies no aerodynamic load to a vehicle with no drag area
            if (!(props.AerodynamicCdABody.ComputeCdA (-flowDirection) > 0))
                return;
            var surfaceFlow = GetSurfaceFlow (vehicle);
            if (surfaceFlow == null) {
                ComputeDrag (in props, dynamicPressure, density, flowDirection, bodyRates, out forceBody, out torqueBody);
                return;
            }
            // The physics step also limits the loads so that they cannot reverse the flow or
            // the rotation within a step. We give the loads before that limit.
            var direction = flowDirection.ToBepu ();
            surfaceFlow.Update (direction);
            var mixture = atmosphere.Mixture;
            var law = new LocalInclinationLaw ((float)(speed / speedOfSound), (float)mixture.ComputeGamma (temperature));
            var friction = new SkinFrictionLaw (mixture, temperature, density, speed, null);
            var loads = law.ComputeLoads (
                surfaceFlow, in friction, direction, (bodyRates / speed).ToBepu (), props.CenterOfMassAsmb.ToBepu ());
            forceBody = dynamicPressure * Double3Ex.FromBepu (loads.ForcePerDynamicPressure);
            torqueBody = dynamicPressure * Double3Ex.FromBepu (loads.MomentPerDynamicPressure);
        }

        // The drag on a vehicle with no aerodynamic surface, from its drag area
        static void ComputeDrag (
            in VehicleProperties props, double dynamicPressure, double density, double3 flowDirection, double3 bodyRates,
            out double3 forceBody, out double3 torqueBody)
        {
            var dragArea = props.AerodynamicCdABody.ComputeCdA (-flowDirection) + 0.1 * props.TotalSurfaceArea;
            forceBody = dragArea * dynamicPressure * flowDirection;
            torqueBody = double3.Cross (double3.Unpack (props.MassToGeometryAsmb), forceBody);
            // Damping of the rotation
            var rimSpeed = bodyRates.Length () * props.ComputeBoundingSphereRadiusAsmb ();
            torqueBody += 0.1 * props.TotalSurfaceArea * 0.5 * density * rimSpeed * rimSpeed * -bodyRates.NormalizeOrZero ();
        }
    }
}
