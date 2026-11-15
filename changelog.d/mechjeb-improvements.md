## service/MechJeb

- Throw an exception from the `TargetController` members that need a target, when there is none
  (#1381)
- Throw an exception from `LandingAutopilot.LandAtPositionTarget` when there is no position
  target, and from both landing methods when the vessel is landed (#1381)
- Throw an exception from `NodeExecutor.ExecuteOneNode` and `ExecuteAllNodes` when the vessel
  has no maneuver node (#1381)
- `Operation.MakeNodes`
  - Plan from the orbit after the vessel's last maneuver node (#1381)
  - Set `ErrorMessage` to the error it throws, and clear the message of an earlier call (#1381)
  - Remove the nodes it placed when it fails to place one (#1381)
- Apply `AscentAutopilot.LimitAngleOfAttack` on the classic ascent path without MechJeb's
  ascent window (#1381)
- Apply `SmartASS.ForcePitch`, `ForceYaw` and `ForceRoll` in the surface modes without the
  Smart A.S.S. window (#1381)
- **Breaking:** Rename `AscentAutopilot.LimitAoA`, `MaxAoA` and `AoALimitFadeoutPressure` to
  `LimitAngleOfAttack`, `MaxAngleOfAttack` and `AngleOfAttackLimitFadeoutPressure` (#1381)
- **Breaking:** Rename `OperationCourseCorrection.CourseCorrectFinalPeA` to
  `FinalPeriapsisAltitude` (#1381)
