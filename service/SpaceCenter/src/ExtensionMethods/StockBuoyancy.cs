using System;
using System.Collections.Generic;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Buoyancy readout for a part, in the game's native scale: world space, cubic meters,
    /// tonnes and kilonewtons. Callers converting to SI multiply forces by 1000.
    /// The displaced volume is recomputed from the drag cubes rather than read from
    /// PartBuoyancy, which only exists in flight, so the editor and the flight scene report
    /// the same figure.
    /// </summary>
    static class StockBuoyancy
    {
        /// <summary>
        /// The volume of water the part displaces when fully submerged, in cubic meters, as
        /// PartBuoyancy.UpdateDisplacement computes it. The drag cube gives a bounding volume,
        /// which the ratio of each face's area to its side shrinks towards the shape of the part.
        /// </summary>
        public static double Displacement (global::Part part)
        {
            var cubes = part.DragCubes;
            if (cubes != null && !cubes.None) {
                // The weighted cube is built lazily and ForceUpdate only marks it stale.
                // The flight integrator builds one every frame, so only the editor builds
                // one here.
                if (HighLogic.LoadedSceneIsEditor && cubes.WeightedSize.x == 0f) {
                    cubes.ForceUpdate (true, true);
                    cubes.SetDragWeights ();
                }
                var cube = OverrideCube (part);
                var size = cube != null ? cube.Size : cubes.WeightedSize;
                var area = cube != null ? cube.Area : cubes.WeightedArea;
                if (size.x > 0f && size.y > 0f && size.z > 0f) {
                    double volume = (double)size.x * size.y * size.z;
                    double x = area [0] / ((double)size.y * size.z);
                    double y = area [2] / ((double)size.x * size.z);
                    double z = area [4] / ((double)size.x * size.y);
                    double total = x + y + z;
                    if (!double.IsNaN (total) && !double.IsInfinity (total) && total > 0) {
                        double xz = (Math.Min (x, z) + 2d * x * z) / 3d;
                        volume *= xz * y;
                    }
                    return volume;
                }
            }
            if (part.collider != null) {
                var size = part.collider.bounds.size;
                return (double)size.x * size.y * size.z * 0.75d;
            }
            return PhysicsGlobals.BuoyancyDefaultVolume;
        }

        /// <summary>
        /// The drag cube named by the part's buoyancyUseCubeNamed, or null when the part
        /// names none. A parachute that names none takes its packed cube, which the game
        /// only writes to the part once the part runs in flight.
        /// </summary>
        static DragCube OverrideCube (global::Part part)
        {
            var name = part.buoyancyUseCubeNamed;
            if (string.IsNullOrEmpty (name)) {
                if (!PartBuoyancy.overrideCubeOnChutesIfUnspecified ||
                    part.Modules == null || !part.Modules.Contains ("ModuleParachute"))
                    return null;
                name = "PACKED";
            }
            var cubes = part.DragCubes.Cubes;
            for (int i = 0; i < cubes.Count; i++) {
                if (cubes [i].Name == name)
                    return cubes [i];
            }
            return null;
        }

        /// <summary>
        /// Whether the body the vessel is at has an ocean. The game leaves the live
        /// buoyancy fields at whatever they last held away from water, so every reader of
        /// one checks this first.
        /// </summary>
        public static bool HasOcean (global::Vessel vessel)
        {
            if (vessel == null)
                return false;
            var body = vessel.mainBody;
            return body != null && body.ocean;
        }

        /// <summary>
        /// Whether the body the part's vessel is at has an ocean. A part in the editor
        /// belongs to no vessel, and reads as having none.
        /// </summary>
        public static bool HasOcean (global::Part part)
        {
            return HasOcean (part.vessel);
        }

        /// <summary>
        /// The game's buoyancy state for the part while it is in the water, or null.
        /// </summary>
        static PartBuoyancy Splashed (global::Part part)
        {
            var buoyancy = part.partBuoyancy;
            if (buoyancy == null || !buoyancy.splashed || !HasOcean (part))
                return null;
            return buoyancy;
        }

        /// <summary>
        /// The point the buoyant force acts at, in world space. A submerged part uses the
        /// point the game applied the force at, which it shifts towards the submerged part of
        /// the hull. Any other part uses its configured center of buoyancy.
        /// </summary>
        public static Vector3d CenterOfBuoyancy (global::Part part)
        {
            var buoyancy = Splashed (part);
            if (buoyancy != null)
                return buoyancy.lastForcePosition;
            return ConfiguredCenterOfBuoyancy (part);
        }

        /// <summary>
        /// The point the buoyant force acts at with the part fully submerged, in world space.
        /// </summary>
        public static Vector3d ConfiguredCenterOfBuoyancy (global::Part part)
        {
            var transform = part.transform;
            return transform.position + transform.rotation * part.CenterOfBuoyancy;
        }

        /// <summary>
        /// The point the depth of the part is measured at, in world space.
        /// </summary>
        public static Vector3d CenterOfDisplacement (global::Part part)
        {
            var transform = part.transform;
            return transform.position + transform.rotation * part.CenterOfDisplacement;
        }

        /// <summary>
        /// The mass of water the part is displacing, in tonnes, after the game's buoyancy
        /// scalar, the part's own multiplier and the ramp that eases the force in over the
        /// first fraction of a meter of depth. Zero for a part clear of the water.
        /// </summary>
        public static double BuoyantGeeForce (global::Part part)
        {
            var buoyancy = Splashed (part);
            return buoyancy == null ? 0d : buoyancy.buoyantGeeForce;
        }

        /// <summary>
        /// The buoyant force acting on the part, in world space, in kilonewtons.
        /// Rebuilt from the displaced mass: the force the game applies carries water drag and
        /// lift as well, so it is not the buoyant force on its own.
        /// </summary>
        public static Vector3d BuoyantForce (global::Part part)
        {
            return BuoyantForce (part, CenterOfBuoyancy (part));
        }

        /// <summary>
        /// The buoyant force acting on the part, in world space, in kilonewtons, given the
        /// point it acts at. For a caller that already has the point.
        /// </summary>
        public static Vector3d BuoyantForce (global::Part part, Vector3d centerOfBuoyancy)
        {
            var displacedMass = BuoyantGeeForce (part);
            if (displacedMass == 0d)
                return Vector3d.zero;
            var gee = FlightGlobals.getGeeForceAtPosition (centerOfBuoyancy);
            return -gee * displacedMass * part.vessel.gravityMultiplier
                * PhysicsGlobals.GraviticForceMultiplier;
        }

        /// <summary>
        /// The buoyant force on the fully submerged part in a fluid of the given density, in
        /// kilonewtons. Density is in tonnes per cubic meter and gravity in meters per second
        /// squared. Applies the game's buoyancy scalar and the part's own multiplier, so it
        /// predicts what the game does rather than what Archimedes' principle alone gives.
        /// </summary>
        public static double BuoyantForceAt (global::Part part, double density, double gravity)
        {
            return Displacement (part) * density * gravity
                * PhysicsGlobals.BuoyancyScalar * part.buoyancy;
        }

        /// <summary>
        /// The buoyancy of a set of parts when fully submerged. Each part pushes up in
        /// proportion to its displacement times its multiplier, at its configured center of
        /// buoyancy.
        /// </summary>
        internal struct Submerged
        {
            /// <summary>
            /// The sum of each part's displacement times its multiplier, in cubic meters.
            /// </summary>
            public double Volume;

            /// <summary>
            /// The point the total buoyant force acts through, in world space, or null when
            /// no part displaces any water.
            /// </summary>
            public Vector3d? CenterOfBuoyancy;

            /// <summary>
            /// The total buoyant force, in kilonewtons. Density is in tonnes per cubic meter
            /// and gravity in meters per second squared.
            /// </summary>
            public double Force (double density, double gravity)
            {
                return Volume * density * gravity * PhysicsGlobals.BuoyancyScalar;
            }

            /// <summary>
            /// The torque the buoyant force exerts about the given point, in world space, in
            /// kilonewton-meters. The force acts against the given down direction.
            /// </summary>
            public Vector3d Torque (double density, double gravity, Vector3d down, Vector3d point)
            {
                if (!CenterOfBuoyancy.HasValue)
                    return Vector3d.zero;
                var force = -down.normalized * Force (density, gravity);
                return Vector3d.Cross (CenterOfBuoyancy.Value - point, force);
            }
        }

        /// <summary>
        /// The buoyancy of the given parts when fully submerged.
        /// </summary>
        public static Submerged FullySubmerged (IEnumerable<global::Part> parts)
        {
            var weighted = Vector3d.zero;
            double total = 0d;
            foreach (var part in parts) {
                var weight = Displacement (part) * part.buoyancy;
                if (weight <= 0d)
                    continue;
                weighted += ConfiguredCenterOfBuoyancy (part) * weight;
                total += weight;
            }
            var result = new Submerged ();
            result.Volume = total;
            if (total > 0d)
                result.CenterOfBuoyancy = weighted / total;
            return result;
        }

        /// <summary>
        /// The pressure of the water above a point at the given depth below sea level, in
        /// kilopascals, as the flight integrator adds it to a part's static pressure.
        /// Gravity is in meters per second squared. Zero above sea level or away from an ocean.
        /// </summary>
        public static double WaterPressure (CelestialBody body, double depth, double gravity)
        {
            if (!body.ocean || depth <= 0d)
                return 0d;
            return depth * body.oceanDensity * gravity;
        }
    }
}
