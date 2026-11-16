## service/SpaceCenter

- Wheels and landing legs
  - Add `Leg.SuspensionSpring`, `Leg.SuspensionDamper`, `Leg.SuspensionTravel` and
    `Leg.Load`, the live state of the suspension, and the same on `Wheel` (#1361)
  - Add `Leg.ImpactTolerance` and `Leg.ExplodeMultiplier`, the load that breaks or destroys
    the leg, and the same on `Wheel` (#1361)
  - Add `Leg.ContactPosition` and `Wheel.ContactPosition`, the bottom of the foot or wheel
    (#1361)
  - Add `Leg.SuspensionSpringStrength` and `Leg.SuspensionDamperStrength` (#1361)
