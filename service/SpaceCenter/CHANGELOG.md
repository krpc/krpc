## [v0.6.0]

- Space center
  - Add `SpaceCenter.Expansions` to report the installed KSP expansions (#985)
  - Add `SpaceCenter.AltimeterMode` and an `AltimeterMode` enumeration to get and set the
    mode of the in-game altimeter (#808)
  - `SpaceCenter.ActiveVessel` now returns null when there is no active vessel, instead of
    raising an error (#821)
  - **Breaking:** Make `LaunchVessel` crew parameter non-optional to work around protocol issue (#824)
  - **Deprecated:** `SpaceCenter.LoadSpaceCenter`, superseded by setting `KRPC.GameScene` (#897)

- Vessels and crew
  - Add `Vessel.Loaded` and `Vessel.Packed` to report a vessel's loading and on-rails state (#967)
  - Add `Vessel.PhysicsRange` to set how far from the active vessel a vessel keeps running its
    physics simulation, so that it stays controllable beyond the game's default range. The
    range applies to that vessel only, and persists until it is set back to zero or the game
    is exited (#987)
  - Add RPCs for getting acceleration to `Flight` and `Vessel` classes (#581)
  - Fix `Vessel.AvailableTorque` due to sign inconsistency in KSP's `ITorqueProvider` implementations (#525)
  - Add `CrewMember.Part` and per-part `CrewCapacity` and `Crew` properties (#1001)

- Staging
  - Add staging API giving per-stage delta-v and resource information (#920)
  - **Deprecated:** `Parts.InStage`, `Parts.InDecoupleStage` and `Vessel.ResourcesInDecoupleStage`,
    superseded by the staging API (#920)
  - Fix `Part.DecoupledAt` not considering if a decoupler is enabled (#818)
  - Fix `Part.DecoupledAt` returning the wrong stage when the part is decoupled by a
    decoupler on its parent part (#800)
  - Fix `Decoupler.AttachedPart` raising an error when no part is attached to the
    decoupler's node; it now returns null (#822)

- Autopilot
  - Rework the `AutoPilot` for smoother, more accurate pointing across a wide range of craft,
    from small probes to large flexible launchers, with no per-craft tuning (#882, #571,
    #394, #220, #248, #542)
  - The `AutoPilot` now controls pitch and yaw together, so the nose tracks a straight path to
    the target and holding a roll no longer curves that path (#882)
  - The `AutoPilot` now detects and damps structural oscillations on long or flexible craft
    automatically; rigid craft are unaffected (#882)
  - Replace `AutoPilot.Engage` and `Disengage` with the `AutoPilot.Engaged` property (#882)
  - Add `AutoPilot.TargetRotation` to command a full target orientation (direction and roll) (#882)
  - Add `AutoPilot.SetDirectionAndUp` to aim the nose along a direction while keeping the roof
    aligned to an up vector, so an orientation can be held through a maneuver without the
    singularity the old roll control hit at the vertical (#882)
  - Add `AutoPilot.UpReference`, a persistent reference that `AutoPilot.TargetRoll` is measured
    against, and make `TargetRoll` well-defined through the vertical (#882)
  - Add `AutoPilot.AttitudeError` for a singularity-free (pitch, yaw, roll) attitude error;
    `PitchError` and `HeadingError` now use the same well-defined decomposition (#882)
  - Fix `AutoPilot.RollError` returning spurious large values near vertical (#564)
  - Document `Flight.Pitch`, `Heading` and `Roll` and the scalar target pitch/heading/roll as
    ill-defined near vertical, pointing to the rotation and error readouts instead (#882)
  - Add `AutoPilot.TargetSmoothingTime` to slew the target smoothly when it changes, with
    matching `Current*` readouts of the smoothed target actually being tracked (#882)
  - Add `AutoPilot.SoftStartTime` to fade in control output on engagement (#882)
  - Add `AutoPilot.Reset` to disengage and restore all settings to their defaults (#882)
  - Add `AutoPilot.ShowInfoUI` to show an in-game autopilot info window (#882)
  - Replace `AutoPilot.RollThreshold` with `RollStartAngle` and `RollEngageAngle` (#882)
  - Replace `AutoPilot.StoppingTime` and `DecelerationTime` with `AutoPilot.MaxAngularVelocity` (#882)
  - Replace `AutoPilot.AttenuationAngle` with `PitchYawAttenuationAngle` and `RollAttenuationAngle` (#882)
  - Add controls to tune and observe the `AutoPilot`'s oscillation mitigation (rate filter,
    bandwidth floor, feedforward-cut and output-filter modes, and manual structural-frequency
    settings) (#882)
  - Add `AutoPilot.StoppingAngleThreshold` and `StoppingVelocityThreshold` to control when
    `AutoPilot.Wait()` returns (#882)
  - Add an optional timeout to `AutoPilot.Wait()`, throwing `TimeoutException` if it expires (#882)
  - Add `AutoPilot.DiagnosticLogging` and `DiagnosticLog` for per-tick controller diagnostics (#882)
  - On client disconnect the `AutoPilot` is now disengaged but its settings and target are kept (#882)
  - Fix `AutoPilot.Error` to return the absolute value of the normalized angle (#893)
  - Fix the auto-pilot no longer controlling the vessel after a Revert to Launch when a
    client keeps its auto-pilot object across the reload (it appeared engaged but stopped
    driving the vessel until the game was restarted) (#958)
  - Fix `AutoPilot.ReferenceFrame` raising an error when set to a docking port reference frame (#882)

- Reference frames
  - Fix `Maneuver` and `ManeuverOrbital` reference frame velocity (previously returned zero; now returns the orbital velocity at the node UT) (#880)
  - Fix angular velocity for `VesselSurface`, `VesselSurfaceVelocity`, `CelestialBodyOrbital`, `Maneuver`, `ManeuverOrbital`, `Part`, `PartCenterOfMass`, `DockingPort` and `Thrust` reference frames (previously returned zero) (#880)
  - Fix `VesselOrbital` reference frame angular velocity (missing Coriolis correction) (#823)
  - Fix reference frame velocity being incorrect at non-equatorial latitudes (#454)
  - Fix `Vessel`/`Part`/`Hybrid` reference frame rotation precision (#880)
  - Fix `ReferenceFrame.CreateHybrid` using velocity frame's angular velocity instead of the `angularVelocity` frame (#880)
  - Fix `VesselSurfaceVelocity` reference frame producing NaN when surface velocity is near zero; now throws `InvalidOperationException` (#880)
  - Fix `PartCenterOfMass` reference frame throwing `InvalidOperationException` on velocity transforms (#880)
  - Fix `Maneuver` reference frame forward-vector fallback using unnormalized burn vector in collinearity check (#880)
  - Fix `LookRotation2` producing NaN for rotations near 180° by using Shepperd's method (#880)
  - Fix maneuver node reference frame positions being computed relative to the
    active vessel, giving wrong positions for maneuver nodes on other vessels (#880)
  - Improve performance of `CommNode.IsVessel`, `CommNode.Vessel`,
    `Vessel.AngularVelocity` and reference frame angular velocities (#975)

- Orbits, nodes and bodies
  - Add `Orbit.OrbitalEnergy` (#303)
  - Add `Orbit.NextClosestApproach` and `Orbit.ClosestApproaches`, and a `ClosestApproach`
    class exposing the time, distance, positions, velocities, relative position/velocity
    and relative speed at a close approach with a target orbit, plus the vessels or bodies
    involved (#334)
  - **Deprecated:** `Orbit.TimeOfClosestApproach`, `Orbit.DistanceAtClosestApproach` and
    `Orbit.ListClosestApproaches`, superseded by `Orbit.NextClosestApproach` and
    `Orbit.ClosestApproaches` (#334)
  - Add `Node.Rotation` (#880)
  - Fix `Node` objects whose maneuver node was removed by `Control.RemoveNodes`,
    another `Node` object or the in-game UI: all members now raise an error,
    instead of silently reading and writing the detached node (#975)
  - Fix `CelestialBody.Rotation` returning incorrect values before a flight scene loads (#890)

- Flight and aerodynamics
  - **Breaking:** Add a rotation parameter to `Flight.SimulateAerodynamicForceAt` to set the vessel
    attitude (angle of attack and sideslip) independently of velocity. Pass the current
    rotation for the previous behavior (#915)
  - Add `Flight.SurfacePrograde` and `Flight.SurfaceRetrograde` direction vectors (surface-relative velocity direction, matching navball surface mode) (#582)
  - Fix `Flight.AngleOfAttack` and `Flight.SideslipAngle` treating dot product as radians instead of using `asin` (#880)
  - Add `Flight.AerodynamicTorque`, the live counterpart to `Flight.AerodynamicForce`, giving
    the net aerodynamic torque acting on the vessel about its center of mass on the current
    physics frame; not available with FAR (#914)
  - Add `Flight.SimulateAerodynamicTorqueAt` to simulate the aerodynamic torque about the
    vessel's center of mass, for a hypothetical attitude and angular velocity, with both
    stock aerodynamics and FAR, in newton-meters. The torque includes both damping
    mechanisms the game applies: the change in each part's local airflow due to rotation,
    and the per-part rigidbody angular drag (#825, #914, #429)
  - Add `Flight.SimulateAerodynamicWrenchAt` to evaluate stock aerodynamic force and torque
    together at a hypothetical center-of-mass state, attitude, body rate and atmospheric
    ephemeris time (#914)
  - Add `Part.Drag` and `Part.Lift` returning the aerodynamic drag and lift forces acting on an
    individual part (#459)
  - Fix `Flight.SimulateAerodynamicForceAt` ignoring the current deflection of
    aerodynamic control surfaces, including deployed airbrakes (#622)
  - Fix `Flight.SimulateAerodynamicForceAt` overestimating drag at low altitude and
    subsonic speed by applying KSP's pseudo-Reynolds drag multiplier to cube drag (#912)
  - Fix `Flight.SimulateAerodynamicForceAt` returning force in kilonewtons instead of
    newtons when FAR is installed (#915)
  - Fix `Flight.SimulateAerodynamicForceAt` computing incorrect lift for command pods and
    other parts with a perpendicular-only lifting-surface module: cube body lift was
    applied even where the game disables it (a pod with a heatshield occupying its bottom
    node has none), and lifting-surface modules were evaluated against stale internal
    airflow state, so results depended on the game context the call was made from (landed,
    in vacuum, or in atmospheric flight) (#914)
  - Fix `Part.Lift`, `Flight.Lift` and `Flight.AerodynamicForce` including stale body lift for
    parts whose body lift the game suppresses, e.g. a pod with a heatshield attached (#971)

- Control
  - **Breaking:** `Control.SAS` now throws an exception when set to true while the autopilot is
    engaged, matching `AutoPilot.SAS`. Previously the write was accepted and the autopilot
    silently switched SAS back off on the next physics tick (#492)
  - Fix `Control.SASMode` being dropped when set in the same physics tick as enabling SAS (#236)
  - Add `Control.PitchTrim`, `Control.YawTrim` and `Control.RollTrim` to get/set the persistent
    pitch, yaw and roll trim of the active vessel (#863)
  - Add `Control.AeroSurfaces` to enable/disable pitch, yaw and roll on all control surfaces (#827)
  - Add `Control.EngineGimbals` to enable/disable gimbal on all gimballed engines (#827)
  - Add `Control.ThrustReversers` to engage/disengage thrust reversers on all engines (#865)
  - Add `Control.GetActionGroupActions` to list the part actions assigned to an action group (#864)
  - Add direct control of engine gimbals, RCS and control surfaces (#917)
  - Fix `Control.CustomAxis01-04` being applied to the active vessel instead of the
    target vessel, and the getters not reading back the value that was set (#938)
  - Fix throttle being applied to a vessel with no RemoteTech control connection (#938)

- Engines
  - Add `Engine.Flameout` (#311)
  - Add `Engine.CanReverseThrust`, `Engine.ThrustReversed` and `Engine.ToggleThrustReversal` to control thrust reversal on stock and recognized mod engines (#865)
  - Fix `Engine.Propellants` failing for non-active mode on multi-mode engines with different propellants (#833)
  - Fix `Engine.AvailableThrust` and `MaxThrust` for air-breathing engines at supersonic speeds (#924)

- Reaction wheels, RCS and control surfaces
  - Add `ReactionWheel.AuthorityLimiter` (#909)
  - Fix `ReactionWheel.AvailableTorque` not considering authority limiter setting (#909)
  - Fix RCS force and torque vector calculations using max thrust rather than available thrust (#909)
  - Fix `RCS.MaxTorque` documentation error - it considers thrust limit set to 100% (#909)
  - Fix `RCS.Active` reporting a shielded thruster as inactive when it is able to
    thrust while shielded (#975)
  - Fix `ControlSurface.AvailableTorque` returning inconsistent signs (`ModuleControlSurface` negates the roll axis); now normalized to positive/negative torque like other parts (#881)

- Parachutes and wheels
  - Fix `Parachute.Deploy` being a silent no-op for RealChute parachutes (#382)
  - Add `Parachute.SafeState` reporting whether it is currently safe to deploy the
    parachute (safe, risky, unsafe, or not applicable) (#977)
  - Add `Wheel.SteeringAngleAuto` to get/set automatic speed-based steering angle limiting (#975)

- Deployable parts
  - **Breaking:** Replace the `RadiatorState`, `SolarPanelState`, `AntennaState`, `LegState`,
    `WheelState` and `CargoBayState` enumerations with a single `DeployableState` enumeration,
    whose members are `Deployed`, `Retracted`, `Deploying`, `Retracting` and `Broken`. `Radiator.State`
    and `SolarPanel.State` now report `Deployed` and `Deploying` in place of `Extended` and
    `Extending`, and `CargoBay.State` reports `Deployed`, `Retracted`, `Deploying` and `Retracting` in
    place of `Open`, `Closed`, `Opening` and `Closing`. The underlying values are unchanged, so only
    the enumeration and member names need updating (#986)
  - **Breaking:** `ResourceHarvester.State` is now a `DeployableState` and no longer reports
    `Active`; use `ResourceHarvester.Active` to check whether the harvester is drilling. The
    `ResourceHarvesterState` enumeration is removed and its values renumbered, so clients
    using it must be regenerated (#986)
  - Fix `CargoBay.State` reporting `Deployed` for a closed cargo ramp, and reporting a resting
    state while the bay was still moving (#986)

- Robotic parts
  - **Breaking:** `RoboticController.AddAxis`, `AddKeyFrame` and `ClearAxis` now identify an axis by
    the name of its field, for example `targetAngle`, instead of by the label shown in the part's
    menu, for example "Target Angle". The names now match those returned by
    `RoboticController.Axes`, which previously could not be passed back in, and no longer change
    with the language the game is running in (#993)
  - Add `Parts.RoboticControllers` (#985)
  - Add to `RoboticController`: `Enabled`, `Playing`, `Position`, `Length`, `PlaySpeed`, `Play` and `Stop` (#985)
  - Add to `RoboticHinge`: `MinAngle`, `MaxAngle` and `IsMoving` (#985)
  - Add to `RoboticPiston`: `MinExtension`, `MaxExtension` and `IsMoving` (#985)
  - Add to `RoboticRotation`: `MinAngle`, `MaxAngle`, `AllowFullRotation` and `IsMoving` (#985)
  - Add to `RoboticRotor`: `MaxTorque`, `BrakePercentage` and `IsMoving` (#985)
  - Fix `RoboticRotation.CurrentAngle` and `RoboticRotor.CurrentRPM` returning stale values in
    flight (the KSP fields they read are only refreshed while the part action window is open) (#985)

- Resource harvesters and converters
  - Fix `ResourceHarvester.Active` being silently ignored when set while the harvester is deploying; the requested state is now applied when the deploy completes (#983)
  - Fix `ResourceHarvester.ExtractionRate` returning 0 when part action window is not open (#889)
  - Fix `ResourceConverter.State` and `ResourceHarvester.ThermalEfficiency` misreading the KSP status string (#892)
  - Fix `Sensor.Value` returning zero when the part action window is not open (#883)

- Part modules, fields and configuration
  - Add `PartField`, `PartEvent` and `PartAction` API (#859)
  - **Deprecated:** the string-dictionary `Module` field, event and action members (#859)
  - Allow `Module.HasFieldWithId`, `GetFieldById`, `SetFieldIntById`, `SetFieldFloatById`,
    `SetFieldStringById`, `SetFieldBoolById` and `ResetFieldById` to also access fields not
    visible in the UI (#858)
  - Add `Part.Config`, `Module.Config` and `ConfigNode` for accessing part and module static
    configuration (#831)

- Science, contracts and waypoints
  - Fix `Experiment.Reset` not clearing the science data stored by DMagic experiments,
    which caused the next call to `Run` to fail with "Experiment already contains data" (#550)
  - Add `Contract.DateAccepted`, `DateDeadline`, `DateExpire` and `DateFinished` (#977)
  - Guard `ContractManager` against null `ContractSystem` in non-career game modes (#819)
  - Fix `Waypoint.MeanAltitude` returning the altitude relative to the terrain below the
    waypoint instead of relative to sea level (#801)
  - Fix `Waypoint.MeanAltitude`, `SurfaceAltitude` and `BedrockAltitude` not round-tripping:
    the setters did not account for KSP storing waypoint altitudes relative to the
    bedrock height, so the altitude read back differed from the value set (#891)

- Alarms
  - **Breaking:** `Alarm.Type` now returns an `AlarmType` enumeration value instead of a string (#853)
  - Add to `Alarm`: `Node`, `OriginBody`, `DestinationBody`, `WarpAction`, `MessageAction`,
    `PlaySound`, `DeleteOnDismiss`, `Triggered` and `Actioned` (#853)
  - Add `AlarmType`, `AlarmWarpAction` and `AlarmMessageAction` enumerations (#853)
  - Add `Alarm.Remove` to delete an alarm (#853)
  - Add `AlarmManager.AlarmWithName`, `AlarmManager.AlarmsWithType` and
    `AlarmManager.AddTransferWindowAlarm` (#853)
  - Fix `NullReferenceException` raised every frame after creating or removing an alarm (#853)

- Camera
  - Add `Camera.FoV` to get and set the camera field of view, and read-only `Camera.MinFoV`,
    `Camera.MaxFoV` and `Camera.DefaultFoV` (#804)
  - Add `Camera.NextCamera`, `Camera.PreviousCamera` and `Camera.FocussedCrewMember` to cycle
    and select IVA cameras (#804)
  - Fix `Camera` `Distance`, `Pitch`, `Heading` and `FoV` setters being lost when set during a camera mode switch (#318)

- Client disconnect cleanup
  - Remove forces added by `Part.AddForce` when the client that added them disconnects (#934)
  - Cancel in-progress resource transfers when the client that started them disconnects;
    a canceled transfer is marked as complete (#934)
  - Fix part highlighting leaking a client-disconnect event handler each time the flight
    scene is loaded (#934)

- Localization
  - Fix `DockingPort.Undock` doing nothing when the game is not running in English (#993)
  - Fix `Fairing.Jettison` doing nothing, and `Fairing.Jettisoned` always reporting true, when the
    game is not running in English (#993)
  - Fix `CargoBay.Open` neither opening nor closing the bay when the game is not running in English (#993)
  - Fix `ResourceConverter.State` only ever reporting `Running` when the game is not running in
    English, leaving `MissingResource`, `StorageFull` and `Capacity` unreachable (#993)
  - Fix `DockingPort.State` reporting `Shielded` instead of `Moving` while a shield is opening or
    closing, when the game is not running in English (#993)
  - Fix part module field values being formatted using the machine's locale, so that
    `Module.Fields`, `Module.GetField` and `PartField.Value` returned numbers that clients running
    under a locale that writes decimals with a comma could not parse (#993)
## [v0.5.4]
- Fix vessel recovery (#782)
- Allow vessel switch from any scene (#781)
- Fix memory leaks on each scene switch (#779)
- Force throttle to zero when timewarping (#792)
- Add `SpaceCenter.Decoupler.AttachedPart` (#789, #791)
- Add `SpaceCenter.Decoupler.IsOmniDecoupler` (#789)
- Fix decoupler crashes with stack separators (#790)
- Make `CelestialBody` parameters doulbe precision (#786)
- Fix scaled camera distance clamp (#785)

## [v0.5.2]
- Fix null reference exception when `SpaceCenter.LaunchVesselFromVAB` is called with no crew (#599)
- Add `Engine.IndependentThrottle` to check whether it is enabled
- Allow setting the independent throttle using `Engine.Throttle`
- Fix `Engine.HasFuel` sometimes returning true for flamed-out SRBs (#631)
- Add functions to manipulate a part modules fields based on identifier
- `Module.Fields` now throws an exception if there are multiple fields with the same name
- Add `Module.FieldsById` to get a dictionary of field values keyed by their identifier
- Add `Module.SetFieldBool` and `Module.SetFieldBoolById` to set a field with a boolean value
- `Module.SetField*` methods now raise an exception if setting the field to the wrong value type
- Change default names for alarms created by `AlarmManager`

## [v0.5.1]
- Fix null reference exception in autopilot for vessels without custom axis controls (#649)

## [v0.5.0]
- Update to work with KSP 1.8+
- Add to `SpaceCenter`:
  - `LaunchSites`
  - `CanRevertToLaunch`
  - `RevertToLaunch`
  - `TransferCrew`
  - `CreateKerbal`
  - `GetKerbal`
  - `LoadSpaceCenter`
  - `MapFilter`
  - `Screenshot`
- Add crew and `flagUrl` parameters to `SpaceCenter.LaunchVessel`
- Add support for more vessel types
- Add `LaunchSite`
- Add `CelestialBody.IsStar` and `CelestialBody.HasSolidSurface`
- Add custom axes to `Control` class
- Add `Part.FlagURL`
- Add `Part.AvailableSeats`
- Add `Part.Glow`
- Add `Part.AutoStrutMode`
- Add support for resource drain parts
- Add support for stock robotic parts
- Add `Parachute.Cut`
- Add support for docking port rotation
- Add support for `Light` blinking
- Add support for wheel steering angle limit and response time
- Fix `ParachuteState.Active` state and clean up real chute vs. stock parachute logic
- Add support for stock alarms (`AlarmManager` and `Alarm` classes)
- Use raycast to measure surface height more precisely in `CelestialBody.SurfaceHeight` (#524)
- Fix null reference exception when checking active vessel (#528, #530)
- Fix null reference exception when waiting for active vessel change (#516, #529)
- Add `RCS.ThrustLimit` and `RCS.AvailableThrust` (#519)
- Add `Control.StageLock` (#541)
- Fix launch checks and automatic vessel recovery in `SpaceCenter.LaunchVessel`
- Fix setting camera target to an unloaded vessel (#536)
- Fix `Control.Legs` and `Control.Wheels` not deploying/retracting legs/wheels correctly (#509, #261)
- Allow legs/wheels can be individually deployed/retracted using `Leg.Deployed` and `Wheel.Deployed` (#509, #261)
- Allow lights to be individually turned on and off (#261)
- Fix `Engine` and RCS fuel calculations only being done when they are enabled (#514, #254)
- Add support for parts that contain multiple experiments (#533)
  - `Part.Experiment` now throws an exception if the part has more than one experiment
  - Add `Part.Experiments` which returns a list of experiments in the part
  - Update `Parts.Experiments` to return all experiments contained in the parts
- Add `Experiment.Name` and `Experiment.Title`
- Fix `Fairing.Jettison` and `Fairing.Jettisoned` for parts from the ProceduralFairings mod (#549)
- Fix `Engine.SpecificImpulse` returning 0 for Realism Overhaul engines that are active but not throttled up (#548)
- Fix compatibility with DMagic science experiments mod (#550)
- Fix available torque calculations for RCS (#552, #354)
- Fix `RCS.MaxThrust` returning incorrect values
- Fix `SimulateAerodynamicForceAt` (#587)
- Fix `RCS.GetTorqueVectors` not considering if each direction is enabled (#591)
- Add `RCS.AvailableForce` and `Vessel.AvailableRCSForce` (#597)

## [v0.4.8]
- Allows RPCs to be accessed from game scenes other than just flight (#471)
- Merge NameTag (http://github.com/krpc/NameTag) into the main kRPC release
- Fix air-relative velocity calculation for `SimulateAeroDynamicForceAt` with FAR (#500)
- Changed `Orbit` API to accept target orbits as `Orbit` objects instead of `Vessel` objects, to allow computing closest approach etc. to planets (#479)
- Trying to enable SAS while the autopilot is engaged now throws an exception as this is not permitted (#492)
- Add color and size paramters to `UI.Message` (#499)
- Fix `Flight.TerminalVelocity` (#485)
- Fix null reference exception when deploying fairings after reverting to launch (#501)

## [v0.4.7]
- Fix `Flight.Lift` and `Flight.Drag` should return values in Newtons (#475)

## [v0.4.6]
- Added `SpaceCenter.GameMode` (#455)
- Added `SpaceCenter.Science`, `Funds` and `Reputation` (#455)
- Fix pre-flight checks not happending in `SpaceCenter.LaunchVessel` (#469)
- Add 'recover' boolean argument to `SpaceCenter.LaunchVessel` to recover existing vessels on the launch pad (#469)
- Change `CelestialBody.Biomes` to return an empty list instead of throwing an exception if the body has no biomes (#457)
- Fixed science transmission not working correctly (#456)

## [v0.4.5]
- Fixed roll input when input mode is override (#445)
- Marked `CelestialBody.Orbit` as nullable, as it returns null for the sun

## [v0.4.4]
- Add `ResourceConverter.ThermalEfficiency`, `CoreTemperature` and `OptimumCoreTemperature` (#439)

## [v0.4.1]
- Add `SpaceCenter.RaycastDistance` and `RaycastPart` (#423)
- Add `Vessel.CrewCount`, `CrewCapacity` and `Crew` (#426)
- Add `CrewMember` class (#426)

## [v0.4.0]
- `AutoPilot.Wait` and `Error` methods now throw an exception if the auto-pilot is not engaged (#409)

## [v0.3.10]
- Fix maneuver node reference frame to point in the direction of the remaining burn, rather than just the initially planned burn (#404)
- Add following to `CelestialBody` class: `AltitudeAtPosition`, `LatitudeAtPosition`, `LongitudeAtPosition` and `AtmosphericDensityAtPosition` (#413)
- Add following to `Orbit` class: `RadiusAt`, `PositionAt`, `MeanAnomalyAtUT`, `TrueAnomalyAtAN`, `TrueAnomalyAtDN`
- Add rendezvous method to `Orbit` class: `RelativeInclination`, `TimeOfClosestApproach`, `DistanceAtClosestApproach`, `ListClosestApproaches` (#407)
- Add `Control.InputMode` to configure the way that control inputs are handled (they are either additive or override the vessels control inputs) (#384)
- Add `CelestialBody.TemperatureAt`, `DensityAt` and `PressureAt` (#350, #352)
- Add `Flight.SimulateAerodynamicForceAt` (#352)

## [v0.3.9]
- Add `VesselType.Plane` and `VesselType.Relay` (#385)
- Fix `Control.WheelThrottle` and `WheelSteering` (#388)
- Fix off by one error in stage numbers for `Vessel.ResourcesInDecoupleStage` (#380)
- Fix `CelestialBody.InitialRotation` and `RotationAngle` returning values out of range 0 - 2*PI (#391)
- Add `WaypointManager.AddWaypointAtAltitude` (#399)
- Add `CelestialBody.PositionAtAltitude` (#399)
- Fix `SpaceCenter.SetTarget` not correctly setting target if input locks are enabled (#397)
- Fix possible null reference exception in `ResourceConverter.State`

## [v0.3.8]
- Add support for CommNet
- Add support for contracts (#361)
- Add support for RealChutes, add `Parachute.Arm` and `Parachute.Armed` (#382)
- Add support for Procedural Fairings (#368)
- Add `CelestialBody.InitialRotation` and `RotationAngle` (#369)
- Add `SpaceCenter.Navball` to show/hide the navball (#355)
- Add `SpaceCenter.UIVisible` to show/hide the UI (#355)
- Add `ControlSurface.AuthorityLimiter` (#352)
- Extended wheels functionality
- Rename `LandingLeg` to `Leg`
- Rename `LandingGear` to `Wheel`
- Add `Antenna.Deployable`, `Leg.Deployable` and `SolarPanel.Deployable`
- Add properties to `Control` class to set the state of all solar panels, radiators, legs, etc. on a vessel (#383)
- Fix bug with `DockingPort.Rotation` (#371)
- Fix bug with `Part.Rotation` (#371)
- Fix bug with `Part.BoundingBox` (#370)

## [v0.3.7]
- Add custom reference frames (`ReferenceFrame.CreateRelative` and `CreateHybrid`) (#252)
- Add `Vessel.BoundingBox` and `Part.BoundingBox` (#352)
- Add `LandingGear.IsGrounded` and `LandingLeg.IsGrounded` (#352)
- Add `Part.Highlighted` and `HighlightColor` (#332)
- Change `CelestialBody.Biomes` to return a set instead of a list
- Change available torque methods to return a pair of torque vectors, one in the positive control directions
  and one in the negative control direction.
- Add `Vessel.AvailableOtherTorque` which returns the available torque from other parts (i.e. torque providers
  other than the stock reaction wheels, RCS thrusters, gimballed engines and control surfaces)
- Include this 'other' torque in `Vessel.AvailableTorque`
- Change required RemoteTech version to 1.8.0
- Change maneuver node methods and properties to use double precision values (#357)
- Add support for Action Groups Extended mod (#364, #365)

## [v0.3.6]
- Add `Part.Tag` which can be used to get/set the name tag set via the NameTag mod, or kOS (#297)
- Add `Parts.WithTag` to get a list of all parts with the given tag value
- Remove `DockingPort.Name` and `Parts.DockingPortsWithName` - part tagging can now be used instead
- Add `Flight.TrueAirSpeed`
- Add FAR support for `Flight.Mach` and add `Flight.ReynoldsNumber` (#313)
- Fix value returned by `Orbit.Inclination` (#301)
- An error is now thrown when `Autopilot.ReferenceFrame` is set to a reference frame that rotates with the vessel
- Fix issues with `Part.DecoupleStage` (#307)
- Add `Part.AddForce`, `Part.InstantaneousFource` and `Force` class for exerting arbitrary forces on parts (#287)
- Fix units for `Flight.DynamicPressure` when FAR is installed - value returned is now in Pa, not kPa (#329)
- Add `Decoupler.Staged`
- Fix issue where disabled decouplers are included in stage index calculation (#323)
- Add `CelestialBody.Biomes` to get all the biomes for a planet/moon (#296)
- Add `CelestialBody.BiomeAt` to get the biome at a location on a planet/moon (#296)
- Add altitude thresholds for science to `CelestialBody`
- Add `Experiment.Biome` to get the biome an experiment part is currently in (#296)
- Add `Experiment.IsAvailable` which indicates if there is science that be gathered by the experiment part
- Add `ScienceSubject` class for getting information about a type of experiment. Accessed via `Experiment.ScienceSubject`.
- Fix landing gear state on launch (#314)
- Add `SpaceCenter.WaypointManager` and `Waypoint` class for interacting with waypoints (#335)
- Change camera distances to use meters (#339)
- Fix engine thrust calculation for air breathing engines (#327)

## [v0.3.5]
- Improved `AutoPilot` that auto-tunes itself based on the vessels available torque and moment of inertia (#289)
- Add `Resources.Enabled` to enable/disable all of the resources in a part or stage at once (#283)
- Add `Vessel.Recoverable` and `Vessel.Recover()` (#277)
- Add `SpaceCenter.LaunchableVessels` to get a list of names of launchable vessels (#278)
- Add `SpaceCenter.LaunchVessel` so that, for example, rockets built in the VAB can be launched from the runway (#278)
- Rename `Engine.Propellants` to `Engine.PropellantNames`
- Add `Propellant` class, obtained using `Engine.Propellants` property
- Add `Experiment` and `ScienceData` classes for interacting with science experiments
- Fix angular velocity for vessel reference frames
- Fix `Engine.AvailableThrust` so that it returns 0 if the vessel is not active
- Make `ReferenceFrame` type, underlying object and its transform accessible via public properties (#290)

## [v0.3.4]
- Move drawing functionality into new `Drawing` service (#253)
- Add `Light.Color`
- Add RPCs for setting `PartModule` fields: `Module.ResetField`, `SetFieldInt`, `SetFieldFloat` and `SetFieldString`

## [v0.3.3]
- Add `AvailableTorque` properties to vessels, reaction wheels, RCS, engines and control surfaces
- Rename `ReactionWheel.Torque` to `ReactionWheel.MaxTorque`
- Fix `Vessel.MomentOfInertia` - use custom inertia tensor calculations to avoid issues with KSPs `Vessel.MoI` and `Vessel.findLocalMoI`
- Move RemoteTech functionality to separate RemoteTech service

## [v0.3.2]
- Fix `Control.SpeedMode` (#258)

## [v0.3.0]
- Support for KSP 1.1
- Add camera controls (note: rotation and zooming of the IVA camera is not yet supported)
- Add saving and loading games using `SpaceCenter.Save`, `Load`, `Quicksave` and `Quickload` (#247)
- Add `ResourceTransfer` object
- Add `Resource` objects, representing an individual resource stored in a part
- Add `Vessel.MomentOfInertia`, Add `Vessel.InteriaTensor` and `Part.InertiaTensor`
- Add `Vessel.Torque` and `Vessel.ReactionWheelTorque`
- Merged `ReactionWheel.PitchTorque`, `YawTorque` and `RollTorque` into a single `ReactionWheel.Torque` method
- Add `Part.CenterOfMass` and `Part.CenterOfMassReferenceFrame`
- Add `Part.Shielded` and `Part.DynamicPressure`
- Add RCS objects.
- Add `Thruster` objects, which represent individual rocket nozzels or RCS thrusters.
- Added `ControlSurface` objects.
- `DockingPort.Undock()` now returns the newly created vessel, instead of the 'undocked vessel'
- `DockingPort.Undock()` now throws an exception (instead of returning null) if the port is not docked
- Return newly created vessels from `Decoupler.Decouple()` (#214)
- Removed `Vessel.Target`, as this functionality is provided by `SpaceCenter.TargetVessel` (#206)
- Change autopilot so that the target reference frame, direction and roll are cleared when the client disconnects and the autopilot is engaged (#248)
- Removed `LandingLegState.Repairing`
- Added `LandingGearState.Broken`
- Fix bug where `Engine.HasFuel` requires the engine to be throttled up
- Fix bug with vessel center of mass calculations (#218)

## [v0.2.3]
- Add support for engine mode switching (#219)
- `Engine.GimbalLimit` and `GimbalLocked` now return an error if the engine is not gimballed
- Add cargo bays, fairings and intakes
- Add support for fixed and 'advanced' langing gear to `LandingGear` class
- Add support for 'active' (non-deployable) radiators to `Radiator` class (#156)
- Remove `Part.ExternalTemperature` (#206,#174)
- Fix units returned by thermal mass and flux methods in `Part` class (#174)
- Fix null pointer exception in autopilot when switching scenes (#220)
- Fix bug with translation inputs in `Control` class (#223)

## [v0.2.2]
- Add `Part.IsFuelLine`
- Fix bug with `Part.FuelLinesTo` (#193)
- `Part.FuelLinesTo` and `FuelLinesFrom` now return an error if called on a fuel line part
- Fix bugs with equality testing of objects. For example, vessel and part objects now persist correctly when reverting to launch (#201,#210)
- Fix array index out of range error in `SpaceCenter.WarpTo` (#169)
- Fix bug with vessel's surface velocity reference frame (#194)

## [v0.2.1]
- Fix `Orbit.Speed` always returning 0
- Add `CelestialBody.SurfaceHeight`, `BedrockHeight`, `MSLPosition`, `SurfacePosition`, `BedrockPosition` (#186)

## [v0.2.0]
- Fix `SpaceCenter.HorizontalSpeed` calculation
- Added support for resource harvesters and converters (#166,#182)
- Fix bug with `Flight.SideslipAngle` (#189)

## [v0.1.12]
- Built for KSP 1.0.5

## [v0.1.11]
- Rename `maxRate` parameter in `SpaceCenter.WarpTo` to `maxRailsRate`
- Add more thermal properties to `Part` class (for new thermal model in KSP 1.0.3) (#155)
- Add `Comms.HasLocalControl`
- Add `Control.SASMode` (identical to `AutoPilot.SASMode`)
- Change control behaviour: now shared amongst clients, and cleared when all
  clients that set a control have disconnected
- Fix setting the throttle with RemoteTech enabled and no connection
- Rewrite `AutoPilot` to use a tunable PID controller (#75)
- Add setting of `SpaceCenter.ActiveVessel` to allow switching vessels (#95)
- Add `SpaceCenter.LaunchVesselFromVAB` and `SpaceCenter.LaunchVesselFromSPH` (#95)
- Update aero methods to use FAR 0.15 if available

## [v0.1.10]
- KSP 1.0.4 support (#151)
- Add more time warp functionality: `SpaceCenter.WarpMode`, `WarpRate`, `WarpFactor`,
  `RailsWarpFactor`, `PhysicsWarpFactor`, `CanRailsWarpAt` and `MaximumRailsWarpFactor` (#134)
- Implement physical time warp for `SpaceCenter.WarpTo` (#137)
- Add support for radiator parts (#154)
- Fix `Vessel.Thrust`, `Engine.Thrust` and `Engine.AvailableThrust` when engines have no fuel (#146)
- Add `Resoures.FlowMode`
- Add ability to control vessels other than the active one, when they are within physics range (#61)
- Add `SpaceCenter.DrawLine` to draw arbitrary lines (#150)
- Rename `SpaceCenter.ClearDirections` to `SpaceCenter.ClearDrawing`

## [v0.1.9]
- Fix bug with combined specific impulse calculations (#117)
- Add `Engine.PropellantRatios` (#118)
- Combined `VesselResources` and `PartResources` classes into a single `Resources` class
- Add `Resources.Density`

## [v0.1.8]
- Add `Engine.MaxVacuumThrust`
- Add `Engine.Throttle`
- Add `Engine.GimbalLimit`
- Add `Vessel.MaxVacuumThrust`
- Clean up float/double types (#113)
- Remove `Vessel.CrossSectionalArea`
- Remove `CelestialBody.AtmospherePressure`, `AtmosphereDensity`,
  `AtmosphereScaleHeight`, `AtmosphereMaxAltitude`, `AtmospherePressureAt`, `AtmosphereDensityAt`
- Add `CelestialBody.AtmosphereDepth` and `HasAtmosphericOxygen`

## [v0.1.7]
- Add `AutoPilot.SAS`, `SASMode` and `SpeedMode` (#94)
- Update `AutoPilot.Error` to also return SAS error (#94)
- Change return types of `Vessel.Mass`, `DryMass`, `CrossSectionalArea`, `SpecificImpulse` to float
- Changes to thrust functions `Vessel.Thrust`, `AvailableThrust` and `MaxThrust` (#103)
- Changes to thrust functions `Engine.Thrust`, `Engine.AvailableThrust` and `MaxThrust` (#103)
- Fix orientation of docking port reference frames
- Rename `Engine.Activated` to `Engine.Active` to match other parts

## [v0.1.6]
- `Parts`
- Targeting of vessels, bodies and docking ports
- Remove `Orbit.ReferenceFrame`
- Fix `AutoPilot.Error` and add `AutoPilot.RollError` (#98)

## [v0.1.5]
- Add `SpaceCenter.DrawDirection` and `ClearDirections` for visual debugging
- Add `Vessel.Comms` to interact with RemoteTech
- Add `Control.WheelThrottle` and `WheelSteering`
- Change `Control.ActivateNextStage` to return a list of jettisoned vessels
- Add `Flight.Longitude` and `Latitude`
- Rename `Flight.NormalNeg` to `AntiNormal` and `Flight.RadialNeg` to `AntiRadial` (#90)
- Add FAR functionality to `Vessel.Flight`
- Fix `Body.AtmospherePressureAt` and `AtmosphereDensityAt` when there is no atmosphere
- Add `Vessel.SurfaceVelocityReferenceFrame`
- Add `Node.OrbitalReferenceFrame`
- Add `Node.RemainingBurnVector`
- Fix lots of reference frame bugs
- Remove `Vessel.NonRotatingReferenceFrame`
- Remove `Body.SurfaceReferenceFrame`
- Change default reference frame to `Vessel.SurfaceReferenceFrame` for `Vessel.Flight` and
  `AutoPilot` methods
- Add wait parameter to `AutoPilot` methods
- `AutoPilot` and `Control` inputs reset to 0 when the client that requested them disconnects
- Change `AutoPilot` to set SAS to false when it's disengaged

## [v0.1.4]
- Major refactoring of reference frames (#66)
- Add `Control.Nodes` (#53)
- Add `Body.AtmosphereDensity` (#52)
- Add `CelestialBody.AtmospherePressureAt` and `AtmosphereDensityAt`
- Add `Vessel.Mass` and `DryMass` (#51)
- Add `Vessel.CrossSectionalArea`, `DragCoefficient` and `Drag`
- Add `Vessel.Thrust` and `SpecificImpulse`
- Add `Orbit.Period`, `MeanAnomalyAtEpoch` and `Epoch`
- Change altitude properties to have sensible names (#62)
- Improvements to `Control.Node` class (#48)
- Fix argument type in `Conrol.AddNode` (#59)
- Fix bug with `Vessel.HorizontalSpeed` (#67)

## [v0.1.3]
- Add `SpaceCenter.Vessels`
- Add `SpaceCenter.Bodies`
- Remove `SpaceCenter.Body`
- Add `SpaceCenter.G`
- Add `CelestialBody.Satellites`
- Add `Orbit.Radius`
- Add `Orbit.Speed`
- Add `Orbit.TimeToSOIChange`
- Add `Orbit.NextOrbit`
- Add `Node.Orbit`
- Add `Control.CurrentStage`
- `Resources.Names` now returns a list of resource names
- Add support for abort and 0 action groups
- Rename `Flight.Altitude` to `Flight.TrueAltitude`
- Rename `Flight.TrueAltitude` to `Flight.AbsoluteAltitude`
- Add `Flight.SurfaceAltitude`
- Add `Flight.Elevation`
- Fix bug with `Node.Vector` returning a vector in the wrong vector space
- Improvements to `Resources` class

## [v0.1.2]
- Include reference frame velocity in orbital direction vectors
- Remove `ReferenceFrame.SurfaceVelocity` and `ReferenceFrame.TargetVelocity`
- Change default reference frames for `Vessel.Flight()` and `AutoPilot` functions
  to `ReferenceFrame.Orbital`
- Use Pa instead of kPa for atmospheric pressure

## [v0.1.1]
- Add new functionality to `SpaceCenter` service

## [v0.1.0]
- Initial pre-release
