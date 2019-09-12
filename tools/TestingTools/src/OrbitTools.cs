using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TestingTools
{
    /**
     * The following is taken and adapted from the HyperEdit plugin licensed under the GPL, Copyright Erickson Swift, 2013.
     * As of writing, supported by Team HyperEdit, and Ezriilc.
     * Original HyperEdit concept and code by khyperia (no longer involved).
     */
    static class OrbitTools
    {
        [SuppressMessage("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
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

        [SuppressMessage("Gendarme.Rules.Interoperability", "DelegatesPassedToNativeCodeMustIncludeExceptionHandlingRule")]
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
