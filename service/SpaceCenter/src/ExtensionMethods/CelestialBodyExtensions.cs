namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class CelestialBodyExtensions
    {
        public static Vector3d GetWorldVelocity (this CelestialBody body)
        {
            if (body != body.referenceBody) {
                // Body orbits something
                return body.GetOrbit ().GetVel ();
            } else {
                // The root body has no orbit, so we take its frame velocity relative to the
                // active vessel's body, as Orbit.GetVel does
                var activeVessel = FlightGlobals.ActiveVessel;
                var frameVelocity = activeVessel != null ? activeVessel.orbitDriver.referenceBody.GetFrameVel () : Vector3d.zero;
                return (body.GetFrameVel () - frameVelocity).xzy;
            }
        }
    }
}
