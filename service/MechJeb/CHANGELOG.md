## [v0.8.0]

- Move the MechJeb service into kRPC from Genhis' KRPC.MechJeb 0.7.1, and update it for
  MechJeb 2.15.4.0 (#1326)
- Log a MechJeb member the service cannot bind, and fail only the calls that use it, in place
  of an error pop-up (#1326)

- Layout
  - **Breaking:** Lay out the service like MechJeb's windows. The docs list each moved member
    (#1382)
  - **Breaking:** Rename `AscentAutopilot` to `AscentGuidance`, with its settings in
    `AscentGuidance.Settings`, `LandingAutopilot` to `LandingGuidance`, and `AirplaneAutopilot`
    to `AircraftAutopilot` (#1382)
  - **Breaking:** Replace `AscentPathClassic` and `AscentPathPVG` with
    `AscentGuidance.ClassicPath` and `PVGSettings`, and `AscentPathIndex` with `AscentType`
    (#1382)
  - **Breaking:** Move `ThrustController`, `StagingController` and the deployable controllers
    into `Utilities`, as `Thrust`, `Staging`, `SolarPanels` and `Antennas` (#1382)
  - **Breaking:** Move the members of `NodeExecutor` to `ManeuverPlanner`, of `RCSController` to
    `SmartRCS`, and the landing target members of `TargetController` to `LandingGuidance`
    (#1382)
  - **Breaking:** Spell out abbreviations in names, such as `LimitAoA` to `LimitAngleOfAttack`,
    `CourseCorrectFinalPeA` to `FinalPeriapsisAltitude` and `RcsAdjustment` to `RCSAdjustment`
    (#1382)
  - **Breaking:** Make `Autostaging.AutostagingOnce` read-only, and add `AutostageOnce` to set
    it (#1382)

- Removed for MechJeb 2.15
  - **Breaking:** Remove `AscentGT` and `AscentAutopilot.AscentPathGT`, as MechJeb 2.15 has no
    gravity turn ascent path (#1326)
  - **Breaking:** Remove `NodeExecutor.Tolerance`, `ThrustController.SmoothThrottle`,
    `ThrottleSmoothingTime` and `OperationTransfer.PeriodOffset` (#1326)
  - **Breaking:** Throw an exception on setting `SmartRCSMode.ZeroVelocity` or
    `TranslatronMode.KeepRelative`, as MechJeb 2.15 has no such mode (#1326)
  - **Breaking:** Remove `MechJeb.APIReady`, in place of `MechJeb.Available` and
    `MechJeb.Ready` (#1326)
  - **Breaking:** Remove `Operation.MakeNode`, deprecated in 0.7.1 (#1382)
  - **Breaking:** Remove `OperationTransfer.InterceptOnly` and `SimpleTransfer`, in place of
    `Capture` and `Coplanar` (#1382)

- New windows and operations
  - Add `AircraftApproach`, the Aircraft Approach & Autoland window, which lands a plane on a
    runway (#1382)
  - Add `RoverAutopilot`, the Rover Autopilot window, which drives a rover along waypoints
    (#1382)
  - Add `AttitudeAdjustment`, the Attitude Adjustment window, which selects the attitude
    controller (#1382)
  - Add `ManeuverPlanner.OperationAdvancedTransfer`, a transfer to another planet at the
    departure that needs the least delta-v (#1382)
  - Add `ManeuverPlanner.OperationEccentricity`, which changes the eccentricity (#1382)

- New settings
  - Add the launches to a longitude of the ascending node, `AscentGuidance.LaunchToLan` and
    `LaunchToMatchLan`, with `DesiredLan` (#1382)
  - Add `AscentGuidance.DesiredFPA` and `OverrideWarpToPlane`, and `AscentSettings.RollAltitude`,
    `LimitingAngleOfAttack` and `CurrentMaxAngleOfAttack` (#1382)
  - Add the stage, coast, spin-up and Qα limit settings of the PVG Settings window to
    `PVGSettings` (#1382)
  - Add `OperationTransfer.Capture`, `PlanCapture`, `Rendezvous`, `LagTime` and `Coplanar`
    (#1382)
  - Add `Autowarp` to `AscentSettings`, `LandingGuidance` and `RendezvousAutopilot` (#1382)
  - Add `ManeuverPlanner.RCSOnly` and `KillRollRotation`, `RendezvousAutopilot.MaxClosingSpeed`
    and `SmartASS.Roll` (#1382)
  - Add `ThrustLimiter.LimitToPreventUnstableIgnition`, `AutoRCSUllaging`, and `Limiter`, the
    limiter that limits the throttle (#1382)

- Fixes
  - Plan `Operation.MakeNodes` from the orbit after the last maneuver node, and remove the nodes
    it placed when it fails (#1382)
  - Set `Operation.ErrorMessage` to the error `MakeNodes` throws, and clear the message of an
    earlier call (#1382)
  - Apply `AscentSettings.LimitAngleOfAttack` and the `SmartASS` force settings without
    MechJeb's windows open (#1382)
  - Make `OperationLan.NewLAN` and `OperationLongitude.NewSurfaceLongitude` set the same
    longitude (#1326)
  - Release the autopilots of both ascent paths when the ascent autopilot is disabled (#1326)
  - Throw an exception from the members that need a target, a landing target or a maneuver
    node when there is none, and from the landing methods when landed (#1382)

