## service/SpaceCenter

- Fix velocities, orbits and orbital reference frames throwing an exception in a Kopernicus
  system whose star is not named `Sun` (#1359)
- Fix `CelestialBody.SurfaceGravity` differing from the gravity the game applies by about
  0.03% (#1359)
