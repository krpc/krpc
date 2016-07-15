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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public static Orbit CreateOrbit (CelestialBody body, double semiMajorAxis, double eccentricity, double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            if (Math.Sign (eccentricity - 1) == Math.Sign (semiMajorAxis))
                semiMajorAxis = -semiMajorAxis;
            if (Math.Sign (semiMajorAxis) >= 0) {
                while (meanAnomalyAtEpoch < 0)
                    meanAnomalyAtEpoch += Math.PI * 2;
                while (meanAnomalyAtEpoch > Math.PI * 2)
                    meanAnomalyAtEpoch -= Math.PI * 2;
            }
            return new Orbit (inclination, eccentricity, semiMajorAxis, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, body);
        }

        public static OrbitDriver OrbitDriver {
            get {
                if (FlightGlobals.fetch == null || FlightGlobals.fetch.activeVessel == null)
                    throw new InvalidOperationException ("No active vessel");
                return FlightGlobals.fetch.activeVessel.orbitDriver;
            }
        }

        public static void Set (this Orbit orbit, Orbit newOrbit)
        {
            var vessel = FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.FirstOrDefault (v => v.orbitDriver != null && v.orbit == orbit);
            var body = FlightGlobals.fetch == null ? null : FlightGlobals.Bodies.FirstOrDefault (v => v.orbitDriver != null && v.orbit == orbit);
            if (vessel != null)
                WarpShip (vessel, newOrbit);
            else if (body != null)
                WarpPlanet (body, newOrbit);
            else
                HardSet (orbit, newOrbit);
        }

        static void WarpShip (Vessel vessel, Orbit newOrbit)
        {
            if (newOrbit.getRelativePositionAtUT (Planetarium.GetUniversalTime ()).magnitude > newOrbit.referenceBody.sphereOfInfluence)
                throw new ArgumentException ("Destination position was above the sphere of influence");

            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = string.Empty;
            var parts = vessel.parts;
            if (parts != null) {
                var clamps = parts.Where (p => p.Modules != null && p.Modules.OfType<LaunchClamp> ().Any ()).ToList ();
                foreach (var clamp in clamps)
                    clamp.Die ();
            }

            try {
                OrbitPhysicsManager.HoldVesselUnpack (60);
            } catch (NullReferenceException) {
            }

            foreach (var v in (FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels).Where(v => !v.packed))
                v.GoOnRails ();

            HardSet (vessel.orbit, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        static void WarpPlanet (CelestialBody body, Orbit newOrbit)
        {
            var oldBody = body.referenceBody;
            HardSet (body.orbit, newOrbit);
            if (oldBody != newOrbit.referenceBody) {
                oldBody.orbitingBodies.Remove (body);
                newOrbit.referenceBody.orbitingBodies.Add (body);
            }
            body.CBUpdate ();
        }

        static void HardSet (Orbit orbit, Orbit newOrbit)
        {
            orbit.inclination = newOrbit.inclination;
            orbit.eccentricity = newOrbit.eccentricity;
            orbit.semiMajorAxis = newOrbit.semiMajorAxis;
            orbit.LAN = newOrbit.LAN;
            orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            orbit.epoch = newOrbit.epoch;
            orbit.referenceBody = newOrbit.referenceBody;
            orbit.Init ();
            orbit.UpdateFromUT (Planetarium.GetUniversalTime ());
        }
    }
}
