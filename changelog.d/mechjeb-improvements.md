## service/MechJeb

- Throw an exception from the `TargetController` members that need a target, when there is none
  (#1381)
- Throw an exception from `LandingAutopilot.LandAtPositionTarget` when there is no position
  target, and from both landing methods when the vessel is landed (#1381)
- Throw an exception from `NodeExecutor.ExecuteOneNode` and `ExecuteAllNodes` when the vessel
  has no maneuver node (#1381)
