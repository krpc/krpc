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
                // Body does not orbit anything
                // Get a body that orbits the sun
                var orbitingBody = FlightGlobals.Bodies.Find (b => b.name != "Sun" && b.GetOrbit ().referenceBody == body);
                var orbit = orbitingBody.GetOrbit ();
                // Compute the velocity of the sun in world space from this body
                // Can't be done for from the sun object as it has no orbit object
                return orbit.GetVel () - orbit.GetRelativeVel ();
            }
        }
    }
}
