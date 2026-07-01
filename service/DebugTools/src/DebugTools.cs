using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.DebugTools
{
    /// <summary>
    /// This service provides "cheat" functionality for debugging.
    /// </summary>
    [KRPCService (Id = 8, GameScene = GameScene.All)]
    public static class DebugTools
    {
        private static void PrepVesselTeleport(this Vessel vessel) {
            if (vessel.Landed) {
                vessel.Landed = false;
                UnityEngine.Debug.Log("Set ActiveVessel.Landed = false");
            }
            if (vessel.Splashed) {
                vessel.Splashed = false;
                UnityEngine.Debug.Log("Set ActiveVessel.Splashed = false");
            }
            if (vessel.landedAt != string.Empty) {
                vessel.landedAt = string.Empty;
                UnityEngine.Debug.Log("Set ActiveVessel.landedAt = \"\"");
            }
            var parts = vessel.parts;
            if (parts != null) {
                var killcount = 0;
                foreach (var part in parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList()) {
                killcount++;
                part.Die();
                }
                if (killcount != 0) {
                UnityEngine.Debug.Log($"Removed {killcount} launch clamps from {vessel.vesselName}");
                }
            }
        }
        
        private static void SetOrbit(this Vessel vessel, Orbit newOrbit)
        {
            var destinationMagnitude = newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude;
            if (destinationMagnitude > newOrbit.referenceBody.sphereOfInfluence)
            {
                UnityEngine.Debug.LogError("Destination position was above the sphere of influence");
                return;
            }
            if (destinationMagnitude < newOrbit.referenceBody.Radius)
            {
                UnityEngine.Debug.LogError("Destination position was below the surface");
                return;
            }

            vessel.PrepVesselTeleport();

            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
                UnityEngine.Debug.LogError("OrbitPhysicsManager.HoldVesselUnpack threw NullReferenceException");
            }

            var allVessels = FlightGlobals.fetch?.vessels ?? (IEnumerable<Vessel>)new[] { vessel };
            foreach (var v in allVessels)
                v.GoOnRails();

            var oldBody = vessel.orbitDriver.orbit.referenceBody;

            HardsetOrbit(vessel.orbitDriver, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;

            var newBody = vessel.orbitDriver.orbit.referenceBody;
            if (newBody != oldBody)
            {
                var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
                GameEvents.onVesselSOIChanged.Fire(evnt);
            }
        }
        
        private static void HardsetOrbit(OrbitDriver orbitDriver, Orbit newOrbit)
        {
            var orbit = orbitDriver.orbit;
            orbit.inclination = newOrbit.inclination;
            orbit.eccentricity = newOrbit.eccentricity;
            orbit.semiMajorAxis = newOrbit.semiMajorAxis;
            orbit.LAN = newOrbit.LAN;
            orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            orbit.epoch = newOrbit.epoch;
            orbit.referenceBody = newOrbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(Planetarium.GetUniversalTime());
            if (orbit.referenceBody != newOrbit.referenceBody)
            {
                orbitDriver.OnReferenceBodyChange?.Invoke(newOrbit.referenceBody);
            }
            UnityEngine.Debug.Log(
                $"Orbit changed to: inc={orbit.inclination} ecc={orbit.eccentricity} sma={orbit.semiMajorAxis} lan={orbit.LAN} argpe={orbit.argumentOfPeriapsis} mep={orbit.meanAnomalyAtEpoch} epoch={orbit.epoch} refbody={orbit.referenceBody}");
        }

        private static Orbit Clone(this Orbit o)
        {
            return new Orbit(o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN,
                o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, o.referenceBody);
        }


        private static Orbit CreateOrbit(double inc, double e, double sma, double lan, double w, double mEp, double epoch, CelestialBody body)
        {
			if (inc == 0)
				inc = 0.0001d;
            if (double.IsNaN(inc))
				inc = 0.0001d;
            if (double.IsNaN(e))
                e = 0;
            if (double.IsNaN(sma))
                sma = body.Radius + body.atmosphereDepth + 10000;
			if (double.IsNaN(lan))
				lan = 0.0001d;
			if (lan == 0)
				lan = 0.0001d;
            if (double.IsNaN(w))
                w = 0;
            if (double.IsNaN(mEp))
                mEp = 0;
            if (double.IsNaN(epoch))
                mEp = Planetarium.GetUniversalTime();

            if (Math.Sign(e - 1) == Math.Sign(sma))
                sma = -sma;

            if (Math.Sign(sma) >= 0)
            {
                while (mEp < 0)
                    mEp += Math.PI * 2;
                while (mEp > Math.PI * 2)
                    mEp -= Math.PI * 2;
            }

			// "inc" is probably inclination
			// "e" is probably eccentricity
			// "sma" is probably semi-major axis
			// "lan" is probably longitude of the ascending node
			// "w" is probably the argument of periapsis (omega)
			// mEp is probably a mean anomaly at some time, like epoch
			// t is probably current time

            return new Orbit(inc, e, sma, lan, w, mEp, epoch, body);
        }

        /// <summary>
        /// Moves vessel's center of mass to position in referenceFrame moving at velocity and with orientation rotation using raw position modification routines.
        /// </summary>
        [KRPCProcedure]
        public static void TeleportDirect(SpaceCenter.Services.Vessel vessel, SpaceCenter.Services.ReferenceFrame referenceFrame, Tuple3 position, Tuple3 velocity, Tuple4 rotation) {
            var worldPosition = referenceFrame.PositionToWorldSpace(position.ToVector());
            var worldVelocity = referenceFrame.VelocityToWorldSpace(position.ToVector(), velocity.ToVector());
            var worldRotation = referenceFrame.RotationToWorldSpace(GeometryExtensions.ToQuaternion(rotation));
            var realVessel = vessel.InternalVessel;
            PrepVesselTeleport(realVessel);
            realVessel.SetPosition(worldPosition);
            realVessel.SetWorldVelocity(worldVelocity);
            realVessel.SetRotation(worldRotation);
        }
        /// <summary>
        /// Moves vessel's center of mass to position in referenceFrame moving at velocity and with orientation rotation by modifying the vessel's orbital parameters.
        /// </summary>
        [KRPCProcedure]
        public static void TeleportUsingOrbit(SpaceCenter.Services.Vessel vessel, SpaceCenter.Services.ReferenceFrame referenceFrame, Tuple3 position, Tuple3 velocity, Tuple4 rotation) {
            var worldPosition = referenceFrame.PositionToWorldSpace(position.ToVector());
            var worldVelocity = referenceFrame.VelocityToWorldSpace(position.ToVector(), velocity.ToVector());
            var worldRotation = referenceFrame.RotationToWorldSpace(GeometryExtensions.ToQuaternion(rotation));
            var realVessel = vessel.InternalVessel;
            
            var orbit = realVessel.orbitDriver.orbit.Clone();
            var body = orbit.referenceBody;
            var teleportPosition = (worldPosition - body.transform.position).xzy;
            var teleportVelocity = worldVelocity.xzy;
            orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, body, Planetarium.GetUniversalTime());
            
            realVessel.SetOrbit(orbit);
            realVessel.SetRotation(worldRotation);
        }
        /// <summary>
        /// Changes the vessel's orbit to be that specified by the arguments.
        /// </summary>
        [KRPCProcedure]
        public static void SetOrbit(SpaceCenter.Services.Vessel vessel, double inc, double e, double sma, double lan, double w, double mEp, double epoch, SpaceCenter.Services.CelestialBody body) {
            var realVessel = vessel.InternalVessel;
            realVessel.SetOrbit(CreateOrbit(inc, e, sma, lan, w, mEp, epoch, body.InternalBody));
        }

        /// <summary>
        /// Sets the paused state of the game
        /// </summary>
        [KRPCProcedure]
        public static void SetPaused(bool paused) {
            FlightDriver.SetPause(paused);
        }

        /// <summary>
        /// Gets the paused state of the game
        /// </summary>
        [KRPCProcedure]
        public static bool GetPaused() {
            return FlightDriver.Pause;
        }
    }
}