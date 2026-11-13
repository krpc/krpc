## core

- Add `StdLib.ImageFormat`, the format of the images from the DockingCamera, ScanSat and
  RasterPropMonitor services (#1341)
- Add `ModAPI`, `ModBinder` and `MemberBinder`, for a service that binds to another mod by name,
  and fails only the calls that use a missing member (#1341)

## server

- Load a mod's API when some of its methods are missing, and fail only the calls that use them (#1341)
- Disable only the integration of a mod that fails to load, and give the reason in the errors of
  its RPCs (#1341)
- Load each supported mod whatever its version (#1341)

## service/SpaceCenter

- Keep the FAR, RealFuels, RealAntennas and Action Groups Extended RPCs working when a mod update
  removes members, and fail only the RPCs that use them (#1341)

## service/KerbalAlarmClock

- Keep working when a Kerbal Alarm Clock update removes members, and fail only the RPCs that use
  them (#1341)
- Limit the service to the scenes Kerbal Alarm Clock runs in, apart from `Available`, which
  reports whether it is installed in any scene (#1341)

## service/InfernalRobotics

- Keep working when an Infernal Robotics update removes members, and fail only the RPCs that
  use them (#1341)
- **Breaking:** Remove `Ready` (#1341)

## service/DockingCamera

- Keep working when a Docking Camera update removes members, and fail only the RPCs that use them (#1341)
