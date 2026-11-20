## [v0.8.0]
- **Deprecated:** The `LIDAR` service, superseded by `SpaceCenter.RaycastDistance` (#1233)

## [v0.7.0]

- **Breaking:** Rename the service from `LiDAR` to `LIDAR` (#1149)
- A `Laser` whose part is destroyed, or that is no longer a laser, raises
  `KRPC.ObjectDestroyedException` (#1051)

## [v0.6.0]
- Fix `LiDAR.Available` incorrectly reporting false in game scenes other than flight (#937)

## [v0.5.0]
- Initial version
