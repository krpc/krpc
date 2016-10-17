using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class TargetableExtensions
    {
        public static Vector3d GetWorldPosition (this ITargetable target)
        {
            var vessel = target as Vessel;
            if (vessel != null)
                return vessel.CoM;

            var celestialBody = target as CelestialBody;
            if (celestialBody != null)
                return celestialBody.position;

            var dockingPort = target as ModuleDockingNode;
            if (dockingPort != null)
                return dockingPort.part.transform.position;

            throw new ArgumentException ("Target is not a vessel, celestial body or docking port");
        }

        public static Vector3d GetWorldVelocity (this ITargetable target)
        {
            var vessel = target as Vessel;
            if (vessel != null)
                return vessel.GetOrbit ().GetVel ();

            var celestialBody = target as CelestialBody;
            if (celestialBody != null)
                return celestialBody.GetWorldVelocity ();

            var dockingPort = target as ModuleDockingNode;
            if (dockingPort != null)
                return dockingPort.part.orbit.GetVel ();

            throw new ArgumentException ("Target is not a vessel, celestial body or docking port");
        }
    }
}
