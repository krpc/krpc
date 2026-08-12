## [v0.7.0] - unreleased
- Fix servos being reported multiple times in a `ServoGroup` after switching to and from their
  vessel repeatedly (#1014)

## [v0.6.0]
- Add to `Servo`: `UID`, `Mode` (with a new `ServoMode` enum), `TargetPosition`, `TargetSpeed`,
  `CommandedPosition`, `DefaultPosition`, `ForceLimit`, `MaxForce`, `MaxAcceleration`, `MaxSpeed`,
  `ElectricChargeRequired`, `SpringPower`, `DampingPower`, `RotorAcceleration`, `IsLimited`,
  `IsRotational`, `IsServo`, `CanHaveLimits`, `HasSpring`, `IsRunning`, `PresetPositions`,
  `AddPreset`, `RemovePresetAt` and `SortPresets` (#942)
- Add to `ServoGroup`: `Vessel`, `MovingDirection`, `AdvancedMode`, `ElectricChargeRequired`,
  `BuildAid` and `IKActive` (#942)
- Fix `Servo.MinPosition` and `MaxPosition` to return the position limits set in the
  tweak menu, rather than the fixed range from the part configuration (#942)
- Support controlling servos on any loaded vessel, not just the active vessel (#943)
- Fix `Servo.MoveLeft`, `MoveCenter` and `MoveRight` failing with the latest version of IR (#941)
- Make `InfernalRobotics.Available` report correctly in all game scenes (#941)
- Fix `InfernalRobotics.Ready` reporting false when queried before IR has finished loading (#941)

## [v0.5.0]
- Update to work with Infernal Robotics Next v3.1.9
- Drop support for original Infernal Robotics mod
- Remove `Servo.MoveNextPreset` and `Servo.MovePrevPreset` as they no longer exist in the mod

## [v0.4.8]
- Add suport for both original Infernal Robotics and Infernal Robotics Next (#476)
- Add `InfernalRobotics.Ready` which indicates if the IR is on the current vessel and ready to receive commands
- Change `InfernalRobotics.Available` to return whether the IR mod is installed (not if the IR API is "ready")

## [v0.3.7]
- Add `InfernalRobotics.Available`

## [v0.3.5]
- Add vessel parameter to `ServoGroups`, `Servos` and `ServoWithName` (#282)
- Add `Servo.Part`
- Add `ServoGroup.Parts`

## [v0.3.3]
- Rename `ControlGroup` to `ServoGroup`

## [v0.1.9]
- Initial version - API for InfernalRobotics (http://forum.kerbalspaceprogram.com/threads/116064)
