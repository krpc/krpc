#pragma warning disable 0618

using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// kRPC testing tools.
    /// </summary>
    [KRPCService]
    public static class TestingTools
    {
        /// <summary>
        /// Get the name of the current save game.
        /// </summary>
        [KRPCProperty]
        public static string CurrentSave {
            get {
                var title = HighLogic.CurrentGame.Title.Split (' ');
                var name = title.Take (title.Length - 1).ToArray ();
                return string.Join (" ", name);
            }
        }

        /// <summary>
        /// Whether a part with the given name is present in the loaded part catalog. Used by the
        /// test framework to detect mods that add parts but no dedicated kRPC service (e.g.
        /// RealChute, wrapped by the SpaceCenter Parachute class).
        /// </summary>
        /// <param name="name">The internal name of the part, e.g. "RC_stack".</param>
        [KRPCProcedure]
        public static bool PartAvailable (string name)
        {
            return PartLoader.getPartInfoByName (name) != null;
        }

        /// <summary>
        /// Whether any loaded part prefab has a part module with the given name. Used by the test
        /// framework to detect mods that add no part and no dedicated kRPC service, but patch a
        /// module onto existing parts (e.g. Action Groups Extended, whose ModuleManager patch adds
        /// a ModuleAGX module to every part; wrapped by the SpaceCenter Control class).
        /// </summary>
        /// <param name="name">The part module class name, e.g. "ModuleAGX".</param>
        [KRPCProcedure]
        public static bool PartModuleAvailable (string name)
        {
            foreach (var part in PartLoader.LoadedPartsList) {
                var prefab = part.partPrefab;
                if (prefab == null)
                    continue;
                foreach (PartModule module in prefab.Modules)
                    if (module.moduleName == name)
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Quit the game, closing Kerbal Space Program and returning to the desktop.
        /// Works from any scene, including in-flight, and skips the confirmation dialog
        /// that the main-menu quit button normally shows.
        /// </summary>
        [KRPCProcedure]
        public static void Quit ()
        {
            Application.Quit ();
        }

        /// <summary>
        /// Load an existing save game.
        /// </summary>
        [KRPCProcedure]
        public static void LoadSave (string directory, string name)
        {
            HighLogic.SaveFolder = directory;
            var game = GamePersistence.LoadGame (name, HighLogic.SaveFolder, true, false);
            if (game == null || game.flightState == null || !game.compatible)
                throw new ArgumentException ("Failed to load save '" + name + "'");
            FlightDriver.StartAndFocusVessel (game, game.flightState.activeVesselIdx);
            throw new YieldException<Action> (() => WaitForVesselSwitch(0));
        }

        /// <summary>
        /// Remove all vessels except the active vessel.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveOtherVessels ()
        {
            var vessels = FlightGlobals.Vessels.Where (v => v != FlightGlobals.ActiveVessel).ToList ();
            foreach (var vessel in vessels)
                vessel.Die ();
        }

        /// <summary>
        /// Set the orbit of the active vessel to a circular orbit.
        /// </summary>
        /// <param name="body">Body to orbit.</param>
        /// <param name="altitude">Altitude to orbit at, in meters above MSL.</param>
        [KRPCProcedure]
        public static void SetCircularOrbit (string body, double altitude)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            var semiMajorAxis = celestialBody.Radius + altitude;
            FlightGlobals.ActiveVessel.SetOrbit(OrbitTools.CreateOrbit(celestialBody, semiMajorAxis, 0, 0, 0, 0, 0, 0));
            throw new YieldException<Action> (() => WaitForVesselSwitch(0));
        }

        /// <summary>
        /// Set the orbit of the active vessel.
        /// </summary>
        [KRPCProcedure]
        public static void SetOrbit (string body, double semiMajorAxis, double eccentricity, double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            FlightGlobals.ActiveVessel.SetOrbit(OrbitTools.CreateOrbit(celestialBody, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch));
            throw new YieldException<Action> (() => WaitForVesselSwitch(0));
        }

        /// <summary>
        /// Place the active vessel in level atmospheric flight over the given point: at the given
        /// altitude and airspeed, pointing along the given heading at the given pitch and roll, and
        /// let physics resume so it is flying. Use this to set up in-air scenarios (e.g. testing the
        /// autopilot's attitude hold on a stock aircraft) without flying the craft up from the runway.
        /// The pitch/heading/roll match those reported by the vessel's Flight, and the airspeed is set
        /// along the nose so the craft starts at zero angle of attack when level.
        /// </summary>
        /// <param name="body">Name of the body to fly over.</param>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="altitude">Altitude in meters above MSL.</param>
        /// <param name="speed">Airspeed in meters per second (surface-relative).</param>
        /// <param name="heading">Compass heading to point along, in degrees (90 = east).</param>
        /// <param name="pitch">Pitch above the horizon, in degrees. Defaults to 0 (level).</param>
        /// <param name="roll">Roll, in degrees. Defaults to 0 (wings level).</param>
        /// <param name="angleOfAttack">Angle of attack in degrees: how far the airspeed vector sits
        /// below the nose, in the pitch plane. 0 (the default) puts the airspeed along the nose;
        /// a positive value gives a nose-up attitude relative to the flight path (e.g. a re-entry
        /// hold), so the flight-path angle is <paramref name="pitch"/> minus this.</param>
        [KRPCProcedure]
        public static void SetFlight (
            string body, double latitude, double longitude, double altitude,
            double speed, double heading, double pitch = 0, double roll = 0, double angleOfAttack = 0)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            var vessel = FlightGlobals.ActiveVessel;

            // Build the world-space attitude for the requested pitch/heading/roll using the same
            // surface-frame convention (x = zenith, y = north, z = east) as the flight telemetry,
            // evaluated at the target location rather than the vessel's current one.
            var worldPosition = celestialBody.GetWorldSurfacePosition (latitude, longitude, altitude);
            var positionFromBody = worldPosition - celestialBody.position;
            var toNorthPole = (celestialBody.position + (Vector3d)celestialBody.transform.up * celestialBody.Radius) - worldPosition;
            var northPole = toNorthPole.normalized;
            var frameUp = Vector3d.Exclude (positionFromBody, northPole);
            var frameForward = Vector3d.Cross (positionFromBody, northPole);
            KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.OrthoNormalize2 (ref frameForward, ref frameUp);
            var frameRotation = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.LookRotation2 (frameForward, frameUp);
            var inFrame = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.QuaternionFromPitchHeadingRoll (
                new Vector3d (pitch, heading, roll));
            var worldRotation = frameRotation * inFrame;

            // The airspeed points along the flight path, which sits angleOfAttack degrees below the
            // nose in the pitch plane (so a positive angle of attack is a nose-up attitude). Build it
            // the same way as the attitude, from the flight-path pitch.
            var flightPathInFrame = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.QuaternionFromPitchHeadingRoll (
                new Vector3d (pitch - angleOfAttack, heading, roll));
            var flightPath = frameRotation * (flightPathInFrame * Vector3d.up);

            // Orbital velocity giving the requested airspeed: the co-rotating surface velocity at
            // this point plus the airspeed along the flight path.
            var surfaceVelocity = Vector3d.Cross (celestialBody.angularVelocity, positionFromBody);
            var worldVelocity = surfaceVelocity + speed * flightPath;

            // Teleport via a state-vector orbit (SetOrbit clears the landed/clamp state and packs the
            // vessel on rails), then set the attitude; physics resumes when it unpacks.
            var ut = Planetarium.GetUniversalTime ();
            var current = vessel.orbitDriver.orbit;
            var orbit = new Orbit (current.inclination, current.eccentricity, current.semiMajorAxis, current.LAN, current.argumentOfPeriapsis, current.meanAnomalyAtEpoch, current.epoch, current.referenceBody);
            orbit.UpdateFromStateVectors (positionFromBody.xzy, worldVelocity.xzy, celestialBody, ut);
            vessel.SetOrbit (orbit);
            vessel.SetRotation ((Quaternion)worldRotation);

            throw new YieldException<Action> (() => WaitForVesselSwitch (0));
        }

        /// <summary>
        /// Place the active vessel on the surface of a body at the given latitude and longitude,
        /// and wait for it to come to rest. Use this to set up landed scenarios away from a launch
        /// site, for example testing surface harvesters on an ore-rich biome.
        /// </summary>
        /// <param name="body">Name of the body to land on.</param>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="altitude">Height above the terrain to settle at, in meters.
        /// Defaults to 0, resting on the surface.</param>
        [KRPCProcedure]
        public static void SetLanded (string body, double latitude, double longitude, double altitude = 0)
        {
            var celestialBody = FlightGlobals.Bodies.First (b => b.bodyName == body);
            FlightGlobals.ActiveVessel.SetLanded(celestialBody, latitude, longitude, altitude);
            throw new YieldException<Action> (() => WaitForLanded(0));
        }

        /// <summary>
        /// Fill all resource tanks on the active vessel (or a given vessel) to their maximum
        /// capacity. Useful in tests that fire engines and need to restore propellant between runs.
        /// </summary>
        /// <param name="vessel">Vessel to operate on. Defaults to the active vessel.</param>
        [KRPCProcedure]
        public static void FillAllResources (KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            var internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            foreach (var part in internalVessel.parts) {
                foreach (PartResource resource in part.Resources)
                    resource.amount = resource.maxAmount;
            }
        }

        /// <summary>
        /// Fill tanks of a specific resource on the active vessel (or a given vessel) to their
        /// maximum capacity.
        /// </summary>
        /// <param name="resourceName">Name of the resource to fill, e.g. "LiquidFuel".</param>
        /// <param name="vessel">Vessel to operate on. Defaults to the active vessel.</param>
        [KRPCProcedure]
        public static void FillResources (string resourceName, KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            var internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            foreach (var part in internalVessel.parts) {
                foreach (PartResource resource in part.Resources) {
                    if (resource.resourceName == resourceName)
                        resource.amount = resource.maxAmount;
                }
            }
        }

        static Quaternion ZeroRotation {
            get {
                var vessel = FlightGlobals.ActiveVessel;
                var vesselCoM = vessel.CoM;
                var right = vesselCoM - vessel.mainBody.position;
                var northPole = vessel.mainBody.position + ((Vector3d)vessel.mainBody.transform.up) * vessel.mainBody.Radius - vesselCoM;
                northPole.Normalize ();
                var up = Vector3.Exclude (right, northPole);
                var forward = Vector3.Cross (right, northPole);
                Vector3.OrthoNormalize (ref forward, ref up);
                var rotation = Quaternion.LookRotation (forward, up);
                return Quaternion.AngleAxis (90, new Vector3 (0, -1, 0)) * rotation;
            }
        }

        /// <summary>
        /// Clear the rotational velocity of the given vessel.
        /// </summary>
        /// <param name="vessel">Vessel.</param>
        [KRPCProcedure]
        public static void ClearRotation (KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            Vessel internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            internalVessel.SetRotation (ZeroRotation);
            ZeroAngularVelocity (internalVessel);
        }

        // Remove the rotational motion of a vessel without changing its attitude. SetRotation only
        // reorients the part transforms; it leaves each rigidbody's angular velocity untouched, so
        // without SAS the vessel keeps tumbling from the new attitude. Make every part move with the
        // common centre-of-mass velocity and zero spin, so the assembly translates rigidly.
        static void ZeroAngularVelocity (Vessel internalVessel)
        {
            if (!internalVessel.loaded)
                return;
            var momentum = Vector3.zero;
            var totalMass = 0f;
            foreach (var part in internalVessel.parts) {
                var rb = part.rb;
                if (rb == null)
                    continue;
                momentum += rb.velocity * rb.mass;
                totalMass += rb.mass;
            }
            if (totalMass <= 0f)
                return;
            var comVelocity = momentum / totalMass;
            foreach (var part in internalVessel.parts) {
                var rb = part.rb;
                if (rb == null)
                    continue;
                rb.velocity = comVelocity;
                rb.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Reassign every crew member of the given vessel (default: the active vessel) to the Pilot
        /// profession at full experience level. The save's auto-crew fills the pod with whichever
        /// kerbal is next in the roster (often an engineer/scientist), which leaves the vessel on
        /// "partial control" — no in-game SAS and, after a rails warp, an unreliable control source.
        /// Overwriting the trait to Pilot gives deterministic full control for every test run without
        /// changing the craft (a kerbal's mass is the same for any profession, so the calibrated MOI
        /// and torque are unaffected).
        /// </summary>
        /// <param name="vessel">Vessel.</param>
        [KRPCProcedure]
        public static void SetCrewToPilot (KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            Vessel internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            foreach (var crew in internalVessel.GetVesselCrew ()) {
                KerbalRoster.SetExperienceTrait (crew, KerbalRoster.pilotTrait);
                KerbalRoster.SetExperienceLevel (crew, 5);
            }
            internalVessel.CrewListSetDirty ();
        }

        /// <summary>
        /// Apply a rotation to the given vessel.
        /// </summary>
        [KRPCProcedure]
        public static void ApplyRotation (float angle, Tuple<float,float,float> axis, KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            if (axis == null)
                throw new ArgumentNullException (nameof (axis));
            Vessel internalVessel = vessel == null ? FlightGlobals.ActiveVessel : vessel.InternalVessel;
            var axisVector = new Vector3 (axis.Item1, axis.Item2, axis.Item3).normalized;
            var rotation = internalVessel.transform.rotation * Quaternion.AngleAxis (angle, axisVector);
            internalVessel.SetRotation (rotation);
        }

        /// <summary>
        /// Set the absolute attitude of the given vessel (default: the active vessel) to the given
        /// pitch, heading and roll (degrees) in the given reference frame (default: the vessel's
        /// surface reference frame), and zero its rotational velocity. Lets a test start from a fixed,
        /// still pose without first flying the autopilot there. The angles match those reported by
        /// the vessel's Flight pitch/heading/roll.
        /// </summary>
        [KRPCProcedure]
        public static void SetPitchHeadingRoll (
            double pitch, double heading, double roll,
            KRPC.SpaceCenter.Services.ReferenceFrame referenceFrame = null,
            KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            var serviceVessel = vessel ?? new KRPC.SpaceCenter.Services.Vessel (FlightGlobals.ActiveVessel);
            var internalVessel = serviceVessel.InternalVessel;
            var frame = referenceFrame ?? serviceVessel.SurfaceReferenceFrame;
            var inFrame = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.QuaternionFromPitchHeadingRoll (
                new Vector3d (pitch, heading, roll));
            internalVessel.SetRotation ((Quaternion)frame.RotationToWorldSpace (inFrame));
            ZeroAngularVelocity (internalVessel);
        }

        /// <summary>
        /// Point the given vessel (default: the active vessel) along the given direction in the
        /// given reference frame (default: the vessel's surface reference frame), and zero its
        /// rotational velocity. The direction sets where the nose points; pass a real
        /// <paramref name="roll"/> (degrees) to also fix the roll, or NaN to leave it uncontrolled.
        /// This mirrors <see cref="SetPitchHeadingRoll"/> but takes a pointing vector instead of
        /// pitch/heading, matching the attitude the autopilot holds for the same target direction
        /// and roll.
        /// </summary>
        [KRPCProcedure]
        public static void SetDirectionAndRoll (
            Tuple<double,double,double> direction, double roll,
            KRPC.SpaceCenter.Services.ReferenceFrame referenceFrame = null,
            KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            if (direction == null)
                throw new ArgumentNullException (nameof (direction));
            var serviceVessel = vessel ?? new KRPC.SpaceCenter.Services.Vessel (FlightGlobals.ActiveVessel);
            var internalVessel = serviceVessel.InternalVessel;
            var frame = referenceFrame ?? serviceVessel.SurfaceReferenceFrame;
            var dir = new Vector3d (direction.Item1, direction.Item2, direction.Item3).normalized;
            // Point the vessel's forward (local up) along the target direction. When a roll is
            // requested, rebuild the rotation from the equivalent pitch/heading plus that roll, the
            // same chain the autopilot uses when a roll is set on top of a target direction.
            var inFrame = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.FromToRotation (
                Vector3d.up, dir);
            if (!double.IsNaN (roll)) {
                var phr = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.PitchHeadingRoll (inFrame);
                inFrame = KRPC.SpaceCenter.ExtensionMethods.GeometryExtensions.QuaternionFromPitchHeadingRoll (
                    new Vector3d (phr.x, phr.y, roll));
            }
            internalVessel.SetRotation ((Quaternion)frame.RotationToWorldSpace (inFrame));
            ZeroAngularVelocity (internalVessel);
        }

        /// <summary>
        /// Set the angular velocity of the given vessel (default: the active vessel), expressed in
        /// the given reference frame (default: the vessel's surface reference frame). The whole
        /// assembly is put into a rigid rotation about its centre of mass — every part rigidbody
        /// gets the commanded spin and the linear velocity it would have under that rotation — so
        /// the craft spins in place rather than shearing apart or translating. Intended for tests
        /// that need a deterministic, repeatable "nudge", e.g. injecting a tangential spin to probe
        /// the autopilot's precession / limit-cycle behaviour.
        /// </summary>
        [KRPCProcedure]
        public static void ApplyAngularVelocity (
            Tuple<double,double,double> angularVelocity,
            KRPC.SpaceCenter.Services.ReferenceFrame referenceFrame = null,
            KRPC.SpaceCenter.Services.Vessel vessel = null)
        {
            if (angularVelocity == null)
                throw new ArgumentNullException (nameof (angularVelocity));
            var serviceVessel = vessel ?? new KRPC.SpaceCenter.Services.Vessel (FlightGlobals.ActiveVessel);
            var internalVessel = serviceVessel.InternalVessel;
            if (!internalVessel.loaded)
                return;
            var frame = referenceFrame ?? serviceVessel.SurfaceReferenceFrame;
            var commanded = new Vector3d (angularVelocity.Item1, angularVelocity.Item2, angularVelocity.Item3);
            var worldAngularVelocity = (Vector3)frame.AngularVelocityToWorldSpace (commanded);

            // Centre of mass position and velocity of the loaded assembly.
            var momentum = Vector3.zero;
            var comPosition = Vector3.zero;
            var totalMass = 0f;
            foreach (var part in internalVessel.parts) {
                var rb = part.rb;
                if (rb == null)
                    continue;
                momentum += rb.velocity * rb.mass;
                comPosition += rb.worldCenterOfMass * rb.mass;
                totalMass += rb.mass;
            }
            if (totalMass <= 0f)
                return;
            var comVelocity = momentum / totalMass;
            comPosition /= totalMass;

            // Rigid-body kinematics: v_part = v_com + omega x (r_part - r_com). Setting the per-part
            // velocities consistently avoids injecting spurious internal stress that would excite
            // structural modes (important for the flexible test craft).
            foreach (var part in internalVessel.parts) {
                var rb = part.rb;
                if (rb == null)
                    continue;
                rb.angularVelocity = worldAngularVelocity;
                rb.velocity = comVelocity + Vector3.Cross (worldAngularVelocity, rb.worldCenterOfMass - comPosition);
            }
        }

        static void WaitForVesselSwitch (int tick)
        {
            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.packed)
                throw new YieldException<Action> (() => WaitForVesselSwitch(0));
            if (tick < 10)
                throw new YieldException<Action> (() => WaitForVesselSwitch(tick + 1));
        }

        static void WaitForLanded (int tick)
        {
            var vessel = FlightGlobals.ActiveVessel;
            // While packed (on rails) the vessel cannot make ground contact, so just wait.
            if (!vessel.packed) {
                if (vessel.LandedOrSplashed)
                    return;
                // Soft-land: bleed off velocity and ease the vessel down until it touches the
                // terrain, the same damping the HyperEdit lander uses.
                vessel.ChangeWorldVelocity ((vessel.srf_velocity + vessel.upAxis) * -0.5);
            }
            // Give up rather than hang the suite if the vessel never settles.
            if (tick < 1000)
                throw new YieldException<Action> (() => WaitForLanded(tick + 1));
        }
    }
}
