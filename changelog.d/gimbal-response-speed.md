## service/SpaceCenter

- Engines
  - Add `Engine.GimbalResponseSpeed` and `Engine.GimbalResponseSpeedEnabled`, the gimbal's
    response lag (#1360)
  - Add `Engine.GimbalPitchEnabled`, `Engine.GimbalYawEnabled` and `Engine.GimbalRollEnabled`
    (#1360)
  - Fix `Engine.AvailableTorque` counting the pitch and yaw of a gimbal with those axes
    disabled (#1360)
