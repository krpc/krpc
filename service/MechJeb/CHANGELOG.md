## [v0.8.0] - unreleased

- Move the MechJeb service into kRPC from Genhis' KRPC.MechJeb 0.7.1, and update it for
  MechJeb 2.15.4.0 (#1326)
- **Breaking:** Remove `AscentGT` and `AscentAutopilot.AscentPathGT`, as MechJeb 2.15 has no
  gravity turn ascent path (#1326)
- **Breaking:** Remove `NodeExecutor.Tolerance`, `ThrustController.SmoothThrottle` and
  `ThrustController.ThrottleSmoothingTime`, which MechJeb 2.15 removed (#1326)
- **Breaking:** Remove `OperationTransfer.PeriodOffset`, which MechJeb 2.15 replaced with a lag
  time (#1326)
- **Breaking:** Throw an exception on setting `AscentAutopilot.AscentPathIndex` to a value other
  than 0 or 2 (#1326)
- **Breaking:** Throw an exception on setting `SmartRCSMode.ZeroVelocity` or
  `TranslatronMode.KeepRelative`, as MechJeb 2.15 has no such mode (#1326)
- **Breaking:** Remove `MechJeb.APIReady`, in place of `MechJeb.Available` and `MechJeb.Ready`
  (#1326)
- **Breaking:** Remove `Operation.MakeNode`, deprecated in 0.7.1 (#1381)
- Make `OperationLan.NewLAN` and `OperationLongitude.NewSurfaceLongitude` set the same
  longitude (#1326)
- Release the autopilots of both ascent paths when the ascent autopilot is disabled (#1326)
- Log a MechJeb member the service cannot bind, and fail only the calls that use it, in place
  of an error pop-up (#1326)
