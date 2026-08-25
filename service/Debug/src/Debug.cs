using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Debug
{
    /// <summary>
    /// Provides functionality to modify the state of the simulation directly, in ways
    /// the game normally does not allow: teleporting a vessel, setting its attitude,
    /// refilling its tanks and enabling the game's cheat options.
    /// </summary>
    /// <remarks>
    /// Everything in this service is cheating. It has the same effect on a save as the
    /// game's own debug menu (Alt+F12), and the same consequences: contracts, records and
    /// career progress made with it are not earned. It is intended for setting up a
    /// scenario to fly or a script to test, without flying there first.
    /// </remarks>
    [KRPCService (Id = 12, GameScene = GameScene.All)]
    public static class Debug
    {
        /// <summary>
        /// Move the active vessel onto a circular orbit around a body.
        /// </summary>
        /// <param name="body">Body to orbit.</param>
        /// <param name="altitude">Altitude of the orbit, in meters above sea level.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetCircularOrbit (SpaceCenter.Services.CelestialBody body, double altitude)
        {
            if (body == null)
                throw new ArgumentNullException (nameof (body));
            SetShipOrbit (body.InternalBody, body.InternalBody.Radius + altitude, 0, 0, 0, 0, 0);
            throw new YieldException<Action> (() => WaitForTeleport (0));
        }

        /// <summary>
        /// Move the active vessel onto the orbit with the given orbital elements. The angles use
        /// the same units and conventions as <see cref="SpaceCenter.Services.Orbit"/>.
        /// </summary>
        /// <param name="body">Body to orbit.</param>
        /// <param name="semiMajorAxis">Semi-major axis of the orbit, in meters.</param>
        /// <param name="eccentricity">Eccentricity of the orbit.</param>
        /// <param name="inclination">Inclination of the orbit, in radians.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of the ascending node, in radians.</param>
        /// <param name="argumentOfPeriapsis">Argument of periapsis, in radians.</param>
        /// <param name="meanAnomalyAtEpoch">Mean anomaly at epoch, in radians.</param>
        /// <param name="epoch">Universal time of the epoch, in seconds.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetOrbit (
            SpaceCenter.Services.CelestialBody body, double semiMajorAxis, double eccentricity,
            double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis,
            double meanAnomalyAtEpoch, double epoch)
        {
            if (body == null)
                throw new ArgumentNullException (nameof (body));
            SetShipOrbit (
                body.InternalBody, SignedSemiMajorAxis (semiMajorAxis, eccentricity), eccentricity,
                GeometryExtensions.ToDegrees (inclination),
                GeometryExtensions.ToDegrees (longitudeOfAscendingNode),
                GeometryExtensions.ToDegrees (argumentOfPeriapsis),
                MeanAnomalyNow (
                    body.InternalBody, semiMajorAxis, eccentricity, meanAnomalyAtEpoch, epoch));
            throw new YieldException<Action> (() => WaitForTeleport (0));
        }

        /// <summary>
        /// Place the active vessel on the surface of a body at the given latitude and longitude,
        /// and wait for it to come to rest.
        /// </summary>
        /// <param name="body">Body to land on.</param>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="altitude">Height above the terrain to settle at, in meters.
        /// Defaults to 0, resting on the surface.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetLanded (
            SpaceCenter.Services.CelestialBody body, double latitude, double longitude,
            double altitude = 0)
        {
            if (body == null)
                throw new ArgumentNullException (nameof (body));
            var internalVessel = ActiveVessel ();
            Vector3d positionFromBody;
            Vector3d velocity;
            Quaternion rotation;
            Landing.SurfacePose (
                internalVessel, body.InternalBody, latitude, longitude, altitude,
                out positionFromBody, out velocity, out rotation);
            SetStateVectors (body.InternalBody, positionFromBody, velocity);
            internalVessel.SetRotation (rotation);
            // Part modules that require a landed vessel (surface harvesters, for example) read
            // these before physics has confirmed ground contact.
            internalVessel.Landed = true;
            internalVessel.situation = global::Vessel.Situations.LANDED;
            throw new YieldException<Action> (() => WaitForLanded (0));
        }

        /// <summary>
        /// Place the active vessel in atmospheric flight over the given point: at the given altitude
        /// and airspeed, pointing along the given heading at the given pitch and roll, and
        /// let physics resume so that it is flying. Use this to set up an in-air scenario
        /// without flying the craft up from the runway.
        /// </summary>
        /// <remarks>
        /// The pitch, heading and roll match those reported by the vessel's
        /// <see cref="SpaceCenter.Services.Flight"/>.
        /// </remarks>
        /// <param name="body">Body to fly over.</param>
        /// <param name="latitude">Latitude in degrees.</param>
        /// <param name="longitude">Longitude in degrees.</param>
        /// <param name="altitude">Altitude in meters above sea level.</param>
        /// <param name="speed">Airspeed in meters per second, relative to the surface.</param>
        /// <param name="heading">Compass heading to point along, in degrees (90 is east).</param>
        /// <param name="pitch">Pitch above the horizon, in degrees. Defaults to 0 (level).</param>
        /// <param name="roll">Roll, in degrees. Defaults to 0 (wings level).</param>
        /// <param name="angleOfAttack">Angle of attack in degrees: how far the airspeed vector sits
        /// below the nose, in the pitch plane. 0 (the default) puts the airspeed along the nose;
        /// a positive value gives a nose-up attitude relative to the flight path, so the
        /// flight-path angle is <paramref name="pitch"/> minus this.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetFlight (
            SpaceCenter.Services.CelestialBody body, double latitude, double longitude,
            double altitude, double speed, double heading, double pitch = 0, double roll = 0,
            double angleOfAttack = 0)
        {
            if (body == null)
                throw new ArgumentNullException (nameof (body));
            var internalVessel = ActiveVessel ();
            var celestialBody = body.InternalBody;

            // Build the world-space attitude for the requested pitch, heading and roll, using the
            // same surface-frame convention as the flight telemetry (x = zenith, y = north,
            // z = east), evaluated at the target location
            var worldPosition = celestialBody.GetWorldSurfacePosition (latitude, longitude, altitude);
            var positionFromBody = worldPosition - celestialBody.position;
            var toNorthPole = (celestialBody.position + (Vector3d)celestialBody.transform.up * celestialBody.Radius) - worldPosition;
            var northPole = toNorthPole.normalized;
            var frameUp = Vector3d.Exclude (positionFromBody, northPole);
            var frameForward = Vector3d.Cross (positionFromBody, northPole);
            GeometryExtensions.OrthoNormalize2 (ref frameForward, ref frameUp);
            var frameRotation = GeometryExtensions.LookRotation2 (frameForward, frameUp);
            var inFrame = GeometryExtensions.QuaternionFromPitchHeadingRoll (
                new Vector3d (pitch, heading, roll));
            var worldRotation = frameRotation * inFrame;

            // The airspeed points along the flight path, which sits angleOfAttack degrees below the
            // nose in the pitch plane (so a positive angle of attack is a nose-up attitude). Build it
            // the same way as the attitude, from the flight-path pitch.
            var flightPathInFrame = GeometryExtensions.QuaternionFromPitchHeadingRoll (
                new Vector3d (pitch - angleOfAttack, heading, roll));
            var flightPath = frameRotation * (flightPathInFrame * Vector3d.up);

            // Orbital velocity giving the requested airspeed: the co-rotating surface velocity at
            // this point plus the airspeed along the flight path.
            var surfaceVelocity = Vector3d.Cross (celestialBody.angularVelocity, positionFromBody);
            var worldVelocity = surfaceVelocity + speed * flightPath;

            // Teleport via a state-vector orbit, then set the attitude; physics resumes when the
            // vessel unpacks.
            SetStateVectors (celestialBody, positionFromBody, worldVelocity);
            internalVessel.SetRotation ((Quaternion)worldRotation);

            throw new YieldException<Action> (() => WaitForTeleport (0));
        }

        /// <summary>
        /// Move the active vessel to the given position, leaving its velocity as it is. To put it
        /// alongside another vessel, pass a position in that vessel's reference frame.
        /// </summary>
        /// <remarks>
        /// This is the inverse of <see cref="SpaceCenter.Services.Vessel.Position"/> in the same
        /// reference frame.
        /// </remarks>
        /// <param name="position">Position of the vessel's center of mass, in meters.</param>
        /// <param name="referenceFrame">Reference frame the position is in. Defaults to the
        /// reference frame of the body being orbited.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetPosition (
            Tuple3 position, SpaceCenter.Services.ReferenceFrame referenceFrame = null)
        {
            if (position == null)
                throw new ArgumentNullException (nameof (position));
            var internalVessel = ActiveVessel ();
            var celestialBody = internalVessel.orbit.referenceBody;
            var frame = referenceFrame ?? ActiveVesselService ().Orbit.Body.ReferenceFrame;
            var worldPosition = frame.PositionToWorldSpace (
                new Vector3d (position.Item1, position.Item2, position.Item3));
            SetStateVectors (
                celestialBody, worldPosition - celestialBody.position,
                internalVessel.orbit.GetVel ());
            throw new YieldException<Action> (() => WaitForTeleport (0));
        }

        /// <summary>
        /// Set the velocity of the active vessel, leaving it where it is. The vessel is moved onto the
        /// orbit that passes through its current position at the new velocity, so this works
        /// in orbit and in the atmosphere alike.
        /// </summary>
        /// <remarks>
        /// This is the inverse of <see cref="SpaceCenter.Services.Vessel.Velocity"/> in the same
        /// reference frame.
        /// </remarks>
        /// <param name="velocity">Velocity in meters per second.</param>
        /// <param name="referenceFrame">Reference frame the velocity is in. Defaults to the
        /// reference frame of the body being orbited, which rotates with it, so a zero velocity
        /// leaves the vessel at rest relative to the ground.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetVelocity (
            Tuple3 velocity, SpaceCenter.Services.ReferenceFrame referenceFrame = null)
        {
            if (velocity == null)
                throw new ArgumentNullException (nameof (velocity));
            var internalVessel = ActiveVessel ();
            var celestialBody = internalVessel.orbit.referenceBody;
            var frame = referenceFrame ?? ActiveVesselService ().Orbit.Body.ReferenceFrame;
            var worldCoM = (Vector3d)internalVessel.CoM;
            var worldVelocity = frame.VelocityToWorldSpace (
                worldCoM, new Vector3d (velocity.Item1, velocity.Item2, velocity.Item3));
            SetStateVectors (
                celestialBody, worldCoM - celestialBody.position, worldVelocity);
            throw new YieldException<Action> (() => WaitForTeleport (0));
        }

        /// <summary>
        /// Set the attitude of a vessel to the given pitch, heading and roll in the given
        /// reference frame, and stop it rotating. Lets a script start from a fixed, still
        /// pose without first flying the autopilot there.
        /// </summary>
        /// <remarks>
        /// The angles match those reported by the vessel's
        /// <see cref="SpaceCenter.Services.Flight"/>.
        /// </remarks>
        /// <param name="pitch">Target pitch, in degrees.</param>
        /// <param name="heading">Target heading, in degrees.</param>
        /// <param name="roll">Target roll, in degrees.</param>
        /// <param name="referenceFrame">Reference frame the angles are in. Defaults to the
        /// vessel's surface reference frame.</param>
        /// <param name="vessel">Vessel to rotate. Defaults to the active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetPitchHeadingRoll (
            double pitch, double heading, double roll,
            SpaceCenter.Services.ReferenceFrame referenceFrame = null,
            SpaceCenter.Services.Vessel vessel = null)
        {
            var target = ResolveVessel (vessel);
            var frame = referenceFrame ?? target.SurfaceReferenceFrame;
            var inFrame = GeometryExtensions.QuaternionFromPitchHeadingRoll (
                new Vector3d (pitch, heading, roll));
            target.InternalVessel.SetRotation ((Quaternion)frame.RotationToWorldSpace (inFrame));
            ZeroAngularVelocity (target.InternalVessel);
        }

        /// <summary>
        /// Point a vessel along the given direction in the given reference frame, and stop it
        /// rotating. This is <see cref="SetPitchHeadingRoll"/> with a pointing vector instead
        /// of a pitch and heading, matching the attitude the autopilot holds for the same
        /// target direction and roll.
        /// </summary>
        /// <param name="direction">Direction to point the nose of the vessel in.</param>
        /// <param name="roll">Target roll, in degrees, or <c>NaN</c> to leave the roll
        /// uncontrolled.</param>
        /// <param name="referenceFrame">Reference frame the direction and roll are in. Defaults
        /// to the vessel's surface reference frame.</param>
        /// <param name="vessel">Vessel to rotate. Defaults to the active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetDirection (
            Tuple3 direction, double roll,
            SpaceCenter.Services.ReferenceFrame referenceFrame = null,
            SpaceCenter.Services.Vessel vessel = null)
        {
            if (direction == null)
                throw new ArgumentNullException (nameof (direction));
            var target = ResolveVessel (vessel);
            var frame = referenceFrame ?? target.SurfaceReferenceFrame;
            var dir = new Vector3d (direction.Item1, direction.Item2, direction.Item3).normalized;
            // Point the vessel's forward (local up) along the target direction. When a roll is
            // requested, rebuild the rotation from the equivalent pitch/heading plus that roll, the
            // same chain the autopilot uses when a roll is set on top of a target direction.
            var inFrame = GeometryExtensions.FromToRotation (Vector3d.up, dir);
            if (!double.IsNaN (roll)) {
                var phr = GeometryExtensions.PitchHeadingRoll (inFrame);
                inFrame = GeometryExtensions.QuaternionFromPitchHeadingRoll (
                    new Vector3d (phr.x, phr.y, roll));
            }
            target.InternalVessel.SetRotation ((Quaternion)frame.RotationToWorldSpace (inFrame));
            ZeroAngularVelocity (target.InternalVessel);
        }

        /// <summary>
        /// Rotate a vessel about the given axis, in its own reference frame, by the given angle.
        /// </summary>
        /// <param name="angle">Angle to rotate by, in degrees.</param>
        /// <param name="axis">Axis to rotate about.</param>
        /// <param name="vessel">Vessel to rotate. Defaults to the active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void ApplyRotation (
            double angle, Tuple3 axis, SpaceCenter.Services.Vessel vessel = null)
        {
            if (axis == null)
                throw new ArgumentNullException (nameof (axis));
            var internalVessel = ResolveVessel (vessel).InternalVessel;
            var axisVector = new Vector3 ((float)axis.Item1, (float)axis.Item2, (float)axis.Item3).normalized;
            var rotation = internalVessel.transform.rotation * Quaternion.AngleAxis ((float)angle, axisVector);
            internalVessel.SetRotation (rotation);
        }

        /// <summary>
        /// Set the angular velocity of a vessel, expressed in the given reference frame. Pass a
        /// zero vector to stop the vessel rotating.
        /// </summary>
        /// <remarks>
        /// The whole vessel is put into a rigid rotation about its center of mass, so it spins in
        /// place rather than shearing apart or translating.
        /// </remarks>
        /// <param name="angularVelocity">Angular velocity, in radians per second. The direction
        /// is the axis of rotation and the magnitude is the rate.</param>
        /// <param name="referenceFrame">Reference frame the angular velocity is in. Defaults to
        /// the vessel's surface reference frame.</param>
        /// <param name="vessel">Vessel to rotate. Defaults to the active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void SetAngularVelocity (
            Tuple3 angularVelocity,
            SpaceCenter.Services.ReferenceFrame referenceFrame = null,
            SpaceCenter.Services.Vessel vessel = null)
        {
            if (angularVelocity == null)
                throw new ArgumentNullException (nameof (angularVelocity));
            var target = ResolveVessel (vessel);
            var frame = referenceFrame ?? target.SurfaceReferenceFrame;
            var commanded = new Vector3d (angularVelocity.Item1, angularVelocity.Item2, angularVelocity.Item3);
            SetWorldAngularVelocity (
                target.InternalVessel,
                (Vector3)frame.AngularVelocityToWorldSpace (commanded));
        }

        /// <summary>
        /// Fill every resource tank on a vessel to its maximum capacity.
        /// </summary>
        /// <param name="vessel">Vessel to fill. Defaults to the active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void FillAllResources (SpaceCenter.Services.Vessel vessel = null)
        {
            foreach (var part in ResolveVessel (vessel).InternalVessel.parts) {
                foreach (PartResource resource in part.Resources)
                    resource.amount = resource.maxAmount;
            }
        }

        /// <summary>
        /// Fill the tanks of a single resource on a vessel to their maximum capacity.
        /// </summary>
        /// <param name="resourceName">Name of the resource to fill, for example
        /// <c>"LiquidFuel"</c>.</param>
        /// <param name="vessel">Vessel to fill. Defaults to the active vessel.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static void FillResources (
            string resourceName, SpaceCenter.Services.Vessel vessel = null)
        {
            foreach (var part in ResolveVessel (vessel).InternalVessel.parts) {
                foreach (PartResource resource in part.Resources) {
                    if (resource.resourceName == resourceName)
                        resource.amount = resource.maxAmount;
                }
            }
        }

        /// <summary>
        /// Whether engines and RCS thrusters draw no propellant.
        /// </summary>
        [KRPCProperty]
        public static bool InfinitePropellant {
            get { return CheatOptions.InfinitePropellant; }
            set { CheatOptions.InfinitePropellant = value; }
        }

        /// <summary>
        /// Whether parts draw no electric charge.
        /// </summary>
        [KRPCProperty]
        public static bool InfiniteElectricity {
            get { return CheatOptions.InfiniteElectricity; }
            set { CheatOptions.InfiniteElectricity = value; }
        }

        /// <summary>
        /// Whether parts survive collisions that would otherwise destroy them.
        /// </summary>
        [KRPCProperty]
        public static bool NoCrashDamage {
            get { return CheatOptions.NoCrashDamage; }
            set { CheatOptions.NoCrashDamage = value; }
        }

        /// <summary>
        /// Whether the joints between parts are rigid and cannot break.
        /// </summary>
        [KRPCProperty]
        public static bool UnbreakableJoints {
            get { return CheatOptions.UnbreakableJoints; }
            set { CheatOptions.UnbreakableJoints = value; }
        }

        /// <summary>
        /// Whether parts survive temperatures above their maximum, instead of exploding.
        /// </summary>
        [KRPCProperty]
        public static bool IgnoreMaxTemperature {
            get { return CheatOptions.IgnoreMaxTemperature; }
            set { CheatOptions.IgnoreMaxTemperature = value; }
        }

        /// <summary>
        /// Whether parts may be placed so that they intersect one another in the editor.
        /// </summary>
        [KRPCProperty]
        public static bool AllowPartClipping {
            get { return CheatOptions.AllowPartClipping; }
            set { CheatOptions.AllowPartClipping = value; }
        }

        /// <summary>
        /// Whether parts may be attached in any orientation, rather than only the ones their
        /// attachment nodes allow.
        /// </summary>
        [KRPCProperty]
        public static bool NonStrictAttachmentOrientation {
            get { return CheatOptions.NonStrictAttachmentOrientation; }
            set { CheatOptions.NonStrictAttachmentOrientation = value; }
        }

        /// <summary>
        /// Whether a kerbal's inventory may hold more than its slot and mass limits allow.
        /// </summary>
        [KRPCProperty]
        public static bool IgnoreKerbalInventoryLimits {
            get { return CheatOptions.IgnoreKerbalInventoryLimits; }
            set { CheatOptions.IgnoreKerbalInventoryLimits = value; }
        }

        /// <summary>
        /// Whether EVA construction may move parts heavier than a kerbal can normally carry.
        /// </summary>
        [KRPCProperty]
        public static bool IgnoreEVAConstructionMassLimit {
            get { return CheatOptions.IgnoreEVAConstructionMassLimit; }
            set { CheatOptions.IgnoreEVAConstructionMassLimit = value; }
        }

        /// <summary>
        /// Whether contracts are offered regardless of the agency mindset that normally
        /// restricts which ones appear.
        /// </summary>
        [KRPCProperty]
        public static bool IgnoreAgencyMindsetOnContracts {
            get { return CheatOptions.IgnoreAgencyMindsetOnContracts; }
            set { CheatOptions.IgnoreAgencyMindsetOnContracts = value; }
        }

        /// <summary>
        /// Whether the game pauses whenever a vessel comes off rails.
        /// </summary>
        [KRPCProperty]
        public static bool PauseOnVesselUnpack {
            get { return CheatOptions.PauseOnVesselUnpack; }
            set { CheatOptions.PauseOnVesselUnpack = value; }
        }

        /// <summary>
        /// Whether biome boundaries are drawn on the surface of bodies.
        /// </summary>
        [KRPCProperty]
        public static bool BiomesVisible {
            get { return CheatOptions.BiomesVisible; }
            set { CheatOptions.BiomesVisible = value; }
        }

        /// <summary>
        /// Multiplier applied to the force of gravity on every vessel. 1 is normal gravity,
        /// 0 is weightlessness.
        /// </summary>
        [KRPCProperty]
        public static double GravityMultiplier {
            get { return PhysicsGlobals.GraviticForceMultiplier; }
            set { PhysicsGlobals.GraviticForceMultiplier = value; }
        }

        /// <summary>
        /// The number of funds the player has. Only available in career mode.
        /// </summary>
        [KRPCProperty]
        public static double Funds {
            get { return CheckFunding ().Funds; }
            set { CheckFunding ().SetFunds (value, TransactionReasons.None); }
        }

        /// <summary>
        /// The amount of science the player has. Only available in career and science mode.
        /// </summary>
        [KRPCProperty]
        public static float Science {
            get { return CheckResearchAndDevelopment ().Science; }
            set { CheckResearchAndDevelopment ().SetScience (value, TransactionReasons.None); }
        }

        /// <summary>
        /// The player's reputation. Only available in career mode.
        /// </summary>
        [KRPCProperty]
        public static float Reputation {
            get { return CheckReputation ().reputation; }
            set { CheckReputation ().SetReputation (value, TransactionReasons.None); }
        }

        /// <summary>
        /// Research every node in the technology tree and purchase every part in it.
        /// Only available in career and science mode.
        /// </summary>
        [KRPCProcedure]
        public static void UnlockTechnologyTree ()
        {
            CheckResearchAndDevelopment ().CheatTechnology ();
        }

        /// <summary>
        /// Upgrade every building at the space center to its highest level.
        /// Only available in career mode.
        /// </summary>
        [KRPCProcedure]
        public static void UpgradeFacilities ()
        {
            if (ReferenceEquals (ScenarioUpgradeableFacilities.Instance, null))
                throw new InvalidOperationException ("Facility upgrades not available");
            ScenarioUpgradeableFacilities.Instance.CheatFacilities ();
        }

        /// <summary>
        /// Raise every kerbal in the roster to the highest experience level.
        /// </summary>
        [KRPCProcedure]
        public static void MaxKerbalExperience ()
        {
            KerbalRoster.CheatExperience ();
        }

        static Funding CheckFunding ()
        {
            if (ReferenceEquals (Funding.Instance, null))
                throw new InvalidOperationException ("Funding not available");
            return Funding.Instance;
        }

        static ResearchAndDevelopment CheckResearchAndDevelopment ()
        {
            if (ReferenceEquals (ResearchAndDevelopment.Instance, null))
                throw new InvalidOperationException ("Science not available");
            return ResearchAndDevelopment.Instance;
        }

        static global::Reputation CheckReputation ()
        {
            if (ReferenceEquals (global::Reputation.Instance, null))
                throw new InvalidOperationException ("Reputation not available");
            return global::Reputation.Instance;
        }

        // The vessel an RPC acts on: the one passed in, or the active vessel when none was given.
        static SpaceCenter.Services.Vessel ResolveVessel (SpaceCenter.Services.Vessel vessel)
        {
            return vessel ?? ActiveVesselService ();
        }

        static SpaceCenter.Services.Vessel ActiveVesselService ()
        {
            return new SpaceCenter.Services.Vessel (ActiveVessel ());
        }

        static global::Vessel ActiveVessel ()
        {
            var active = FlightGlobals.ActiveVessel;
            if (active == null)
                throw new InvalidOperationException ("There is no active vessel");
            return active;
        }

        // Hand a set of orbital elements to the game's own vessel teleport, which wraps writing
        // them with everything needed to make the move stick: taking the vessels off physics,
        // clearing the landed and ground-contact state, moving the floating origin to the new
        // position, suppressing the collision and g-force checks for a few frames, and firing the
        // sphere-of-influence change event. The orientation angles are in degrees and the mean
        // anomaly is in radians, at the current time. Only the active vessel can be moved.
        static void SetShipOrbit (
            global::CelestialBody body, double semiMajorAxis, double eccentricity,
            double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis,
            double meanAnomaly)
        {
            RemoveLaunchClamps (ActiveVessel ());
            FlightGlobals.fetch.SetShipOrbit (
                FlightGlobals.Bodies.IndexOf (body), eccentricity, semiMajorAxis, inclination,
                longitudeOfAscendingNode, meanAnomaly, argumentOfPeriapsis, 0);
        }

        // Move the active vessel onto the orbit that passes through the given position, relative
        // to the body, at the given world-space velocity. The elements are read back off a
        // throwaway orbit built from the state vectors, since the teleport takes elements.
        static void SetStateVectors (
            global::CelestialBody body, Vector3d positionFromBody, Vector3d worldVelocity)
        {
            var ut = Planetarium.GetUniversalTime ();
            var orbit = new Orbit ();
            orbit.UpdateFromStateVectors (positionFromBody.xzy, worldVelocity.xzy, body, ut);
            // The throwaway orbit's epoch is now, so its mean anomaly at epoch is the mean anomaly
            // at the current time, which is what the teleport takes
            SetShipOrbit (
                body, orbit.semiMajorAxis, orbit.eccentricity, orbit.inclination, orbit.LAN,
                orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch);
        }

        // Mean anomaly at the current time for an orbit given by its mean anomaly at an epoch.
        // The mean anomaly advances linearly with time at the mean motion, sqrt(mu / |a|^3).
        static double MeanAnomalyNow (
            global::CelestialBody body, double semiMajorAxis, double eccentricity,
            double meanAnomalyAtEpoch, double epoch)
        {
            var axis = Math.Abs (semiMajorAxis);
            var meanMotion = Math.Sqrt (body.gravParameter / (axis * axis * axis));
            var meanAnomaly =
                meanAnomalyAtEpoch + meanMotion * (Planetarium.GetUniversalTime () - epoch);
            if (eccentricity < 1) {
                // A closed orbit repeats every revolution, so wrap the angle. An epoch far in the
                // past otherwise gives the game many thousands of radians
                meanAnomaly %= 2 * Math.PI;
                if (meanAnomaly < 0)
                    meanAnomaly += 2 * Math.PI;
            }
            return meanAnomaly;
        }

        // A closed orbit (eccentricity below 1) has a positive semi-major axis and an open one has
        // a negative semi-major axis. Given the two together the game cannot solve the orbit, its
        // patched-conic solver fills with NaN, and the flight scene goes down. Derive the sign from
        // the eccentricity.
        static double SignedSemiMajorAxis (double semiMajorAxis, double eccentricity)
        {
            var axis = Math.Abs (semiMajorAxis);
            return eccentricity > 1 ? -axis : axis;
        }

        // Launch clamps anchor a vessel to the pad, so a craft teleported while still clamped is
        // dragged back. They cannot come along, so destroy them.
        static void RemoveLaunchClamps (global::Vessel internalVessel)
        {
            foreach (var part in internalVessel.parts.ToList ()) {
                if (part.Modules.OfType<LaunchClamp> ().Any ())
                    part.Die ();
            }
        }

        // Stop a vessel rotating without changing its attitude. SetRotation only reorients the part
        // transforms and leaves each rigidbody's angular velocity untouched, so without SAS the
        // vessel keeps tumbling from the new attitude
        static void ZeroAngularVelocity (global::Vessel internalVessel)
        {
            SetWorldAngularVelocity (internalVessel, Vector3.zero);
        }

        // Put the whole loaded assembly into a rigid rotation about its center of mass at the given
        // world-space angular velocity, leaving the center of mass moving as it was.
        static void SetWorldAngularVelocity (global::Vessel internalVessel, Vector3 worldAngularVelocity)
        {
            if (!internalVessel.loaded)
                return;

            // Center of mass position and velocity of the loaded assembly.
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
            // structural modes.
            foreach (var part in internalVessel.parts) {
                var rb = part.rb;
                if (rb == null)
                    continue;
                rb.angularVelocity = worldAngularVelocity;
                rb.velocity = comVelocity + Vector3.Cross (worldAngularVelocity, rb.worldCenterOfMass - comPosition);
            }
        }

        // A teleported vessel is packed (on rails) until the game settles the scene around its new
        // position. Wait for the active vessel to unpack, then a few more ticks for the physics to
        // start, so that the RPC returns with the vessel in its new state.
        static void WaitForTeleport (int tick)
        {
            var active = FlightGlobals.ActiveVessel;
            if (active == null || active.packed)
                throw new YieldException<Action> (() => WaitForTeleport (0));
            if (tick < 10)
                throw new YieldException<Action> (() => WaitForTeleport (tick + 1));
        }

        // The vessel is placed with its lowest point just clear of the terrain, then falls the last
        // fraction of a meter. Damp its motion each tick so it settles quickly
        static void WaitForLanded (int tick)
        {
            var active = FlightGlobals.ActiveVessel;
            // While packed (on rails) the vessel cannot make ground contact, so just wait.
            if (active != null && !active.packed) {
                if (active.LandedOrSplashed)
                    return;
                active.ChangeWorldVelocity ((active.srf_velocity + active.upAxis) * -0.5);
            }
            // Give up if the vessel never settles
            if (tick < 1000)
                throw new YieldException<Action> (() => WaitForLanded (tick + 1));
        }
    }
}
