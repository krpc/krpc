## core

- Add `MemberBinder.ReadOnlyField`, for a mod setting that the mod declares read-only (#1381)

## service/MechJeb

- Throw an exception from the `TargetController` members that need a target, when there is none
  (#1381)
- Throw an exception from `LandingGuidance.LandAtTarget` when there is no landing target, and
  from both landing methods when the vessel is landed (#1381)
- Throw an exception from `ManeuverPlanner.ExecuteNextNode` and `ExecuteAllNodes` when the
  vessel has no maneuver node (#1381)
- `Operation.MakeNodes`
  - Plan from the orbit after the vessel's last maneuver node (#1381)
  - Set `ErrorMessage` to the error it throws, and clear the message of an earlier call (#1381)
  - Remove the nodes it placed when it fails to place one (#1381)
- Apply `AscentSettings.LimitAngleOfAttack` on the classic ascent path without MechJeb's
  ascent window (#1381)
- Apply `SmartASS.ForcePitch`, `ForceYaw` and `ForceRoll` in the surface modes without the
  Smart A.S.S. window (#1381)
- **Breaking:** Rename `AscentAutopilot.LimitAoA`, `MaxAoA` and `AoALimitFadeoutPressure` to
  `LimitAngleOfAttack`, `MaxAngleOfAttack` and `AngleOfAttackLimitFadeoutPressure` (#1381)
- **Breaking:** Rename `OperationCourseCorrection.CourseCorrectFinalPeA` to
  `FinalPeriapsisAltitude` (#1381)
- **Breaking:** Lay out the service like MechJeb's windows. The docs list each moved member
  (#1381)
- **Breaking:** Rename `AscentAutopilot` to `AscentGuidance`, and move its settings to
  `AscentGuidance.Settings` (#1381)
- **Breaking:** Replace `AscentPathClassic` and `AscentPathPVG` with `AscentGuidance.ClassicPath`
  and `PVGSettings`, and `AscentPathIndex` with `AscentType` (#1381)
- **Breaking:** Move `ThrustController`, `StagingController` and the deployable controllers into
  `Utilities`, as `Thrust`, `Staging`, `SolarPanels` and `Antennas` (#1381)
- **Breaking:** Move the members of `NodeExecutor` to `ManeuverPlanner`, and of `RCSController`
  to `SmartRCS` (#1381)
- **Breaking:** Rename `LandingAutopilot` to `LandingGuidance`, with the landing target members
  of `TargetController` (#1381)
- **Breaking:** Rename `AirplaneAutopilot` to `AircraftAutopilot` (#1381)
- **Breaking:** Rename `LandingAutopilot.RcsAdjustment` to `LandingGuidance.RCSAdjustment` (#1381)
- **Breaking:** Make `Autostaging.AutostagingOnce` read-only, and add `AutostageOnce` to set it
  (#1381)
- Throw an exception from `LandingGuidance.GetTargetPosition` when there is no landing target
  (#1381)
- Add `Autowarp` to `AscentSettings`, `LandingGuidance` and `RendezvousAutopilot` (#1381)
- Add `ManeuverPlanner.OperationEccentricity`, the operation that changes the eccentricity
  (#1381)
- Add `OperationTransfer.Capture`, `PlanCapture`, `Rendezvous`, `LagTime` and `Coplanar` (#1381)
- **Breaking:** Remove `OperationTransfer.InterceptOnly` and `SimpleTransfer`, in place of
  `Capture` and `Coplanar` (#1381)
- Add `SmartASS.Roll`, the roll of the modes that point in a direction (#1381)
- Add the launches to a longitude of the ascending node, `AscentGuidance.LaunchToLan` and
  `LaunchToMatchLan`, with `DesiredLan` and their `AscentLaunchMode` values (#1381)
- Add `AscentGuidance.DesiredFPA` and `OverrideWarpToPlane` (#1381)
- Add `AscentSettings.RollAltitude`, `LimitingAngleOfAttack` and `CurrentMaxAngleOfAttack` (#1381)
- Add the stage, coast, spin-up and Qα limit settings of the PVG Settings window to
  `PVGSettings` (#1381)
- Add `ManeuverPlanner.OperationAdvancedTransfer`, a transfer to another planet at the departure
  that needs the least delta-v (#1381)
