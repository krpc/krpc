using System;
using System.Linq;
using UnityEngine;

namespace KRPC.Debug
{
    static class Landing
    {
        /// <summary>
        /// The point to place a vessel's center of mass so that the vessel rests on the surface at
        /// the given latitude and longitude, and the orientation that stands it upright there.
        /// </summary>
        public static void SurfacePose (
            global::Vessel internalVessel, global::CelestialBody body,
            double latitude, double longitude, double altitude,
            out Vector3d positionFromBody, out Vector3d velocity, out Quaternion rotation)
        {
            if (body.pqsController == null)
                throw new ArgumentException ("Cannot land on " + body.bodyName + ", it has no terrain");

            // Distance from the center of mass down to the vessel's lowest point, measured now
            // while the craft is loaded and its colliders are valid. The game classifies a craft
            // teleported onto a near-surface orbit as landed wherever we place it, and does not
            // re-seat it on the terrain, so we place the lowest point on the ground ourselves
            var clearance = GroundClearance (internalVessel);

            // Ideal (PQS) terrain height above sea level, clamped so we never target a
            // point below sea level when over water.
            var terrainHeight = body.pqsController.GetSurfaceHeight (
                body.GetRelSurfaceNVector (latitude, longitude)) - body.Radius;
            terrainHeight = Math.Max (terrainHeight, 0);
            // Rest the lowest point just above the surface (plus any requested offset). The
            // small gap absorbs the sub-meter difference between the PQS curve and the
            // collidable terrain mesh on flat ground.
            var spawnAltitude = terrainHeight + clearance + 0.5 + altitude;

            var worldPosition = body.GetWorldSurfacePosition (latitude, longitude, spawnAltitude);
            positionFromBody = worldPosition - body.position;
            // Velocity of a point fixed to the rotating surface (omega x r), so the vessel
            // stays put relative to the ground instead of moving at orbital speed.
            velocity = Vector3d.Cross (body.angularVelocity, positionFromBody);

            // Orient the vessel upright, with its "up" axis pointing away from the body center,
            // matching how a craft sits on the launch pad.
            var up = (worldPosition - body.position).normalized;
            var north = body.position + (Vector3d)body.transform.up * body.Radius - worldPosition;
            north = Vector3d.Exclude (up, north).normalized;
            rotation = Quaternion.LookRotation ((Vector3)north, (Vector3)up);
        }

        // Distance from the vessel's center of mass to its lowest point, along the local
        // up axis. Launch clamps are excluded because they are removed when teleporting.
        static double GroundClearance (global::Vessel vessel)
        {
            var com = (Vector3d)vessel.CoM;
            var up = vessel.upAxis;
            double clearance = 0;
            foreach (var part in vessel.parts) {
                if (part.Modules.OfType<LaunchClamp> ().Any ())
                    continue;
                foreach (var collider in part.GetComponentsInChildren<Collider> ()) {
                    if (!collider.enabled || collider.isTrigger)
                        continue;
                    var b = collider.bounds;
                    for (int i = 0; i < 8; i++) {
                        var corner = new Vector3d (
                            (i & 1) == 0 ? b.min.x : b.max.x,
                            (i & 2) == 0 ? b.min.y : b.max.y,
                            (i & 4) == 0 ? b.min.z : b.max.z);
                        var drop = Vector3d.Dot (com - corner, up);
                        if (drop > clearance)
                            clearance = drop;
                    }
                }
            }
            return clearance;
        }
    }
}
