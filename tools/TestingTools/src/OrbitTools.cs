using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TestingTools
{
    /**
     * The following is taken and adapted from the HyperEdit plugin licensed under the GPL, Copyright Erickson Swift, 2013.
     * As of writing, supported by Team HyperEdit, and Ezriilc.
     * Original HyperEdit concept and code by khyperia (no longer involved).
     */
    static class OrbitTools
    {
        public static Orbit CreateOrbit(CelestialBody body, double semiMajorAxis, double eccentricity, double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            if (Math.Sign(eccentricity - 1) == Math.Sign(semiMajorAxis))
                semiMajorAxis = -semiMajorAxis;
            if (Math.Sign(semiMajorAxis) >= 0)
            {
                while (meanAnomalyAtEpoch < 0)
                    meanAnomalyAtEpoch += Math.PI * 2;
                while (meanAnomalyAtEpoch > Math.PI * 2)
                    meanAnomalyAtEpoch -= Math.PI * 2;
            }
            return new Orbit(inclination, eccentricity, semiMajorAxis, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, body);
        }

        public static void SetOrbit(this Vessel vessel, Orbit newOrbit)
        {
            var destinationMagnitude = newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude;
            if (destinationMagnitude > newOrbit.referenceBody.sphereOfInfluence)
                throw new ArgumentException("Destination position was above the sphere of influence");
            if (destinationMagnitude < newOrbit.referenceBody.Radius)
                throw new ArgumentException("Destination position was below the surface");

            vessel.PrepTeleport();

            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
                // ignore
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

        public static void SetLanded(this Vessel vessel, CelestialBody body, double latitude, double longitude, double altitude)
        {
            if (body.pqsController == null)
                throw new ArgumentException("Cannot land on " + body.bodyName + ", it has no terrain");

            // Distance from the center of mass down to the vessel's lowest point, measured
            // now while the craft is loaded and its colliders are valid. KSP classifies a
            // craft teleported onto a near-surface orbit as landed wherever we place it (it
            // does not re-seat it on the terrain), so we must place the lowest point on the
            // ground ourselves rather than dropping it from a height.
            var clearance = GroundClearance(vessel);

            // Ideal (PQS) terrain height above sea level, clamped so we never target a
            // point below sea level when over water.
            var terrainHeight = body.pqsController.GetSurfaceHeight(body.GetRelSurfaceNVector(latitude, longitude)) - body.Radius;
            terrainHeight = Math.Max(terrainHeight, 0);
            // Rest the lowest point just above the surface (plus any requested offset). The
            // small gap absorbs the sub-meter difference between the PQS curve and the
            // collidable terrain mesh on flat ground.
            var spawnAltitude = terrainHeight + clearance + 0.5 + altitude;

            var ut = Planetarium.GetUniversalTime();
            var position = body.GetWorldSurfacePosition(latitude, longitude, spawnAltitude) - body.position;
            // Velocity of a point fixed to the rotating surface (omega x r), so the vessel
            // stays put relative to the ground instead of moving at orbital speed.
            var velocity = Vector3d.Cross(body.angularVelocity, position);
            // Convert from world space to orbit space.
            position = position.xzy;
            velocity = velocity.xzy;

            var current = vessel.orbitDriver.orbit;
            var orbit = new Orbit(current.inclination, current.eccentricity, current.semiMajorAxis, current.LAN, current.argumentOfPeriapsis, current.meanAnomalyAtEpoch, current.epoch, current.referenceBody);
            orbit.UpdateFromStateVectors(position, velocity, body, ut);
            vessel.SetOrbit(orbit);

            // Orient the vessel upright, with its "up" axis pointing away from the body center,
            // matching how a craft sits on the launch pad.
            var worldPosition = body.GetWorldSurfacePosition(latitude, longitude, spawnAltitude);
            var up = (worldPosition - body.position).normalized;
            var north = body.position + (Vector3d)body.transform.up * body.Radius - worldPosition;
            north = Vector3d.Exclude(up, north).normalized;
            vessel.SetRotation(Quaternion.LookRotation((Vector3)north, (Vector3)up));

            // Mark it landed so part modules that require a landed vessel (e.g. surface
            // harvesters) operate; physics confirms ground contact once it settles.
            vessel.Landed = true;
            vessel.situation = Vessel.Situations.LANDED;
        }

        // Distance from the vessel's center of mass to its lowest point, along the local
        // up axis. Launch clamps are excluded because they are removed when teleporting.
        static double GroundClearance(Vessel vessel)
        {
            var com = (Vector3d)vessel.CoM;
            var up = vessel.upAxis;
            double clearance = 0;
            foreach (var part in vessel.parts)
            {
                if (part.Modules.OfType<LaunchClamp>().Any())
                    continue;
                foreach (var collider in part.GetComponentsInChildren<Collider>())
                {
                    if (!collider.enabled || collider.isTrigger)
                        continue;
                    var b = collider.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = new Vector3d(
                            (i & 1) == 0 ? b.min.x : b.max.x,
                            (i & 2) == 0 ? b.min.y : b.max.y,
                            (i & 4) == 0 ? b.min.z : b.max.z);
                        var drop = Vector3d.Dot(com - corner, up);
                        if (drop > clearance)
                            clearance = drop;
                    }
                }
            }
            return clearance;
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
                orbitDriver.OnReferenceBodyChange?.Invoke(newOrbit.referenceBody);
        }

        public static void PrepTeleport(this Vessel vessel)
        {
            if (vessel.protoVessel.landed)
            {
                vessel.protoVessel.landed = false;
            }
            if (vessel.protoVessel.splashed)
            {
                vessel.protoVessel.splashed = false;
            }
            if (vessel.protoVessel.landedAt.Length != 0)
            {
                vessel.protoVessel.landedAt = String.Empty;
            }
            if (vessel.Landed)
            {
                vessel.Landed = false;
            }
            if (vessel.Splashed)
            {
                vessel.Splashed = false;
            }
            if (vessel.landedAt.Length != 0)
            {
                vessel.landedAt = string.Empty;
            }
            var parts = vessel.parts;
            if (parts != null)
            {
                var killcount = 0;
                foreach (var part in parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList())
                {
                    killcount++;
                    part.Die();
                }
            }
        }
    }
}
