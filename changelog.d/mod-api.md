## core

- Add `StdLib.ImageFormat`, the format of the images from the DockingCamera, ScanSat and
  RasterPropMonitor services (#1341)
- Add `ModAPI`, `ModBinder` and `MemberBinder`, for a service that binds to another mod by name,
  and fails only the calls that use a missing member (#1341)

## server

- Load a mod's API when some of its methods are missing, and fail only the calls that use them (#1341)
- Disable only the integration of a mod that fails to load, and give the reason in the errors of
  its RPCs (#1341)
