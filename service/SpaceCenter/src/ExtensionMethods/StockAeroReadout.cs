using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Live readout of the per-part forces the stock aerodynamic model applied on the
    /// current physics frame. All vectors are in world space, in kilonewtons (the
    /// game's native force scale); callers converting to SI multiply by 1000.
    /// Not valid when the Ferram Aerospace Research mod is installed, as it replaces
    /// the stock model these fields are read from; callers are responsible for
    /// checking (see Flight.CheckNoFAR and Part.CheckNoFAR).
    /// </summary>
    static class StockAeroReadout
    {
        /// <summary>
        /// Drag applied by the drag-cube model, as applied by
        /// FlightIntegrator.ApplyAeroDrag: -dragVectorDir * dragScalar.
        /// </summary>
        public static Vector3d CubeDrag (global::Part part)
        {
            return -(Vector3d)part.dragVectorDir * part.dragScalar;
        }

        /// <summary>
        /// Body (drag-cube) lift acting on the part, as applied by
        /// FlightIntegrator.ApplyAeroLift. Returns zero for the parts the
        /// FlightIntegrator skips, whose bodyLift fields hold stale values from the
        /// last frame they were updated: parts with a lifting-surface module, and
        /// parts whose bodyLiftOnlyUnattachedLift gate is closed (e.g. a pod with a
        /// heatshield attached on the designated node).
        /// </summary>
        public static Vector3d BodyLift (global::Part part)
        {
            if (part.hasLiftModule)
                return Vector3d.zero;
            if (part.bodyLiftOnlyUnattachedLiftActual
                && part.bodyLiftOnlyProvider != null
                && part.bodyLiftOnlyProvider.IsLifting)
                return Vector3d.zero;
            Vector3 lift = part.transform.rotation * (part.bodyLiftScalar * part.DragCubes.LiftForce);
            return Vector3.ProjectOnPlane (lift, -part.dragVectorDir);
        }

        /// <summary>
        /// Total drag acting on the part: cube drag plus every lifting-surface
        /// module's drag.
        /// </summary>
        public static Vector3d Drag (global::Part part)
        {
            var drag = CubeDrag (part);
            foreach (var module in part.Modules) {
                var wing = module as ModuleLiftingSurface;
                if (wing != null)
                    drag += wing.dragForce;
            }
            return drag;
        }

        /// <summary>
        /// Total lift acting on the part: body lift plus every lifting-surface
        /// module's lift.
        /// </summary>
        public static Vector3d Lift (global::Part part)
        {
            var lift = BodyLift (part);
            foreach (var module in part.Modules) {
                var wing = module as ModuleLiftingSurface;
                if (wing != null)
                    lift += wing.liftForce;
            }
            return lift;
        }
    }
}
