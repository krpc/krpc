## service/SpaceCenter

- Fix velocities, orbits and orbital reference frames throwing an exception in a Kopernicus
  system whose star is not named `Sun` (#1359)
- Fix `CelestialBody.SurfaceGravity` differing from the gravity the game applies by about
  0.03% (#1359)
- Test with Kopernicus, Real Solar System, Realism Overhaul and Procedural Parts, and list their
  tested versions in the documentation (#1359)
