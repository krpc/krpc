## [v0.8.0]

- Move the MechJeb service into kRPC from Genhis' KRPC.MechJeb 0.7.1, and update it for
  MechJeb 2.15.4.0 (#1326)
- **Breaking:** Remove `AscentGT` and `AscentAutopilot.AscentPathGT`, as MechJeb 2.15 has no
  gravity turn ascent path (#1326)
- **Breaking:** Remove `NodeExecutor.Tolerance`, `ThrustController.SmoothThrottle` and
  `ThrustController.ThrottleSmoothingTime`, which MechJeb 2.15 removed (#1326)
- **Breaking:** Remove `OperationTransfer.PeriodOffset`, which MechJeb 2.15 replaced with a lag
  time (#1326)
- **Breaking:** Throw an exception on setting `SmartRCSMode.ZeroVelocity` or
  `TranslatronMode.KeepRelative`, as MechJeb 2.15 has no such mode (#1326)
- **Breaking:** Remove `MechJeb.APIReady`, in place of `MechJeb.Available` and `MechJeb.Ready`
  (#1326)
- **Breaking:** Remove `Operation.MakeNode`, deprecated in 0.7.1 (#1382)
- Make `OperationLan.NewLAN` and `OperationLongitude.NewSurfaceLongitude` set the same
  longitude (#1326)
- Release the autopilots of both ascent paths when the ascent autopilot is disabled (#1326)
- Log a MechJeb member the service cannot bind, and fail only the calls that use it, in place
  of an error pop-up (#1326)
- Throw an exception from the `TargetController` members that need a target, when there is none
  (#1382)
- Throw an exception from `LandingGuidance.LandAtTarget` when there is no landing target, and
  from both landing methods when the vessel is landed (#1382)
- Throw an exception from `ManeuverPlanner.ExecuteNextNode` and `ExecuteAllNodes` when the
  vessel has no maneuver node (#1382)

- `Operation.MakeNodes`
  - Plan from the orbit after the vessel's last maneuver node (#1382)
  - Set `ErrorMessage` to the error it throws, and clear the message of an earlier call (#1382)
  - Remove the nodes it placed when it fails to place one (#1382)

- Apply `AscentSettings.LimitAngleOfAttack` on the classic ascent path without MechJeb's
  ascent window (#1382)
- Apply `SmartASS.ForcePitch`, `ForceYaw` and `ForceRoll` in the surface modes without the
  Smart A.S.S. window (#1382)
- **Breaking:** Rename `AscentAutopilot.LimitAoA`, `MaxAoA` and `AoALimitFadeoutPressure` to
  `LimitAngleOfAttack`, `MaxAngleOfAttack` and `AngleOfAttackLimitFadeoutPressure` (#1382)
- **Breaking:** Rename `OperationCourseCorrection.CourseCorrectFinalPeA` to
  `FinalPeriapsisAltitude` (#1382)
- **Breaking:** Lay out the service like MechJeb's windows. The docs list each moved member
  (#1382)
- **Breaking:** Rename `AscentAutopilot` to `AscentGuidance`, and move its settings to
  `AscentGuidance.Settings` (#1382)
- **Breaking:** Replace `AscentPathClassic` and `AscentPathPVG` with `AscentGuidance.ClassicPath`
  and `PVGSettings`, and `AscentPathIndex` with `AscentType` (#1382)
- **Breaking:** Move `ThrustController`, `StagingController` and the deployable controllers into
  `Utilities`, as `Thrust`, `Staging`, `SolarPanels` and `Antennas` (#1382)
- **Breaking:** Move the members of `NodeExecutor` to `ManeuverPlanner`, and of `RCSController`
  to `SmartRCS` (#1382)
- **Breaking:** Rename `LandingAutopilot` to `LandingGuidance`, with the landing target members
  of `TargetController` (#1382)
- **Breaking:** Rename `AirplaneAutopilot` to `AircraftAutopilot` (#1382)
- **Breaking:** Rename `LandingAutopilot.RcsAdjustment` to `LandingGuidance.RCSAdjustment` (#1382)
- **Breaking:** Make `Autostaging.AutostagingOnce` read-only, and add `AutostageOnce` to set it
  (#1382)
- Throw an exception from `LandingGuidance.GetTargetPosition` when there is no landing target
  (#1382)
- Add `Autowarp` to `AscentSettings`, `LandingGuidance` and `RendezvousAutopilot` (#1382)
- Add `ManeuverPlanner.OperationEccentricity`, the operation that changes the eccentricity
  (#1382)
- Add `OperationTransfer.Capture`, `PlanCapture`, `Rendezvous`, `LagTime` and `Coplanar` (#1382)
- **Breaking:** Remove `OperationTransfer.InterceptOnly` and `SimpleTransfer`, in place of
  `Capture` and `Coplanar` (#1382)
- Add `SmartASS.Roll`, the roll of the modes that point in a direction (#1382)
- Add the launches to a longitude of the ascending node, `AscentGuidance.LaunchToLan` and
  `LaunchToMatchLan`, with `DesiredLan` and their `AscentLaunchMode` values (#1382)
- Add `AscentGuidance.DesiredFPA` and `OverrideWarpToPlane` (#1382)
- Add `AscentSettings.RollAltitude`, `LimitingAngleOfAttack` and `CurrentMaxAngleOfAttack` (#1382)
- Add the stage, coast, spin-up and Qα limit settings of the PVG Settings window to
  `PVGSettings` (#1382)
- Add `ManeuverPlanner.OperationAdvancedTransfer`, a transfer to another planet at the departure
  that needs the least delta-v (#1382)
- Add `AircraftApproach`, the Aircraft Approach & Autoland window, which lands a plane on a
  runway (#1382)
- Add `RoverAutopilot`, the Rover Autopilot window, which drives a rover along waypoints (#1382)
- Add `ManeuverPlanner.RCSOnly` and `KillRollRotation`, and `RendezvousAutopilot.MaxClosingSpeed`
  (#1382)
- Add `ThrustLimiter.LimitToPreventUnstableIgnition`, `AutoRCSUllaging`, and `Limiter`, the
  limiter that limits the throttle (#1382)
- Add `AttitudeAdjustment`, the Attitude Adjustment window, which selects the attitude
  controller (#1382)
