# kRPC for KSA

A kRPC server for Kitten Space Agency (KSA). It is a [StarMap](https://github.com/StarMapLoader/StarMap)
code mod that serves the `KRPC` service and a `KSA` service written for KSA's own model. The tests
run in game on Linux and Windows.

Built against kRPC 0.7.0, KSA 2026.10.25.5707 and StarMap 0.4.8, set in `versions.sh`.

## Layout

 * `server/` - the StarMap entry point (`Addon.cs`), settings and `mod.toml`. All StarMap and
   game lifecycle code is in `Addon.cs`. `src/UI` holds the server windows and the kRPC menu.
 * `service/` - the `KSA` service.
 * `testing/` - the `TestingTools` service, installed only for the tests, test vehicles, and the
   server settings the tests install.
 * `test/` - integration tests, and their harness `ksatest.py`.
 * `examples/` - client scripts.
 * `doc/` - the documentation sources, and the template for the generated API pages.
 * `stubs/` - the readme for the client stubs archive.
 * `release/` - the licenses and install readme for the mod archive.
 * `build.sh` - builds the mod folder and the client stubs.
 * `docs.sh` - builds the documentation.
 * `release.sh` - builds the release archive.
 * `publish-docs.sh` - commits the docs to the `gh-pages` branch.
 * `install-starmap.sh`, `run-ksa.sh` - install StarMap, and launch the game through it on Linux.
 * `test.sh` - runs the tests.
 * `build.bat`, `install-starmap.bat`, `run-ksa.bat`, `test.bat` - the Windows versions of those
   scripts, with the same arguments.

## Build

Build the mod into `bin/kRPC`:

```bash
experimental/ksa/build.sh
```

The build uses a kRPC release, set by `krpc_version` in `versions.sh`, and needs Python 3, the .NET
10 SDK and `zip`:

 * `krpctools` from PyPI, installed into `bin/venv`, provides the .NET `KRPC.Core.dll`, the
   `ServiceDefinitions` tool and `krpc-clientgen`.
 * Both projects compile against that `KRPC.Core.dll` and the KSA assemblies in `KSA_DIR`, which
   defaults to `../KSA` beside the repo. Set `KSA_DIR` to build and test against another install.
 * `ServiceDefinitions` writes `bin/KRPC.KSA.json`, which runs the server's service scan and
   checks the documentation.
 * `krpc-clientgen` builds client stubs from the definitions into `bin/stubs`, which also holds
   the definitions.

Documentation comments use the markup of that release, so units are words rather than `<unit>`.

## Documentation

Build the HTML documentation into `bin/docs`, after `build.sh`:

```bash
experimental/ksa/docs.sh
experimental/ksa/docs.sh --serve   # also serve it on http://127.0.0.1:8000
```

 * `krpc-docgen` generates the API pages for each client language from the templates in
   `doc/api` and `bin/KRPC.KSA.json`, one page per template. `doc/api/ksa.tmpl` lists the pages,
   and each page in `doc/api/ksa` covers a group of classes. `doc/order.txt` orders the members,
   and lists every one of them.
 * Sphinx and its extensions, at the versions of the kRPC docs build, are installed into
   `bin/doc-venv` from `doc/requirements.txt`.
 * The pages follow the layout of the kRPC documentation, with sphinx-design cards on the home
   page. `doc/scripts` holds the code the pages include, and `examples/` is copied beside it as
   `scripts/` for the tutorials. The index example comes in each client language.
 * `pyrightconfig.json` in `examples/` and `doc/scripts` turns off `reportAttributeAccessIssue`,
   which reports the `KSA` service as the client builds it at run time.
 * The pages are static, so `bin/docs/index.html` also opens straight from disk.

`publish-docs.sh` commits the docs into the `gh-pages` branch under `experimental/ksa/`, in a
worktree at `bin/gh-pages`, and `--push` pushes the commit. They are served at
<https://krpc.github.io/krpc/experimental/ksa/>. The docs workflow replaces only its own `dev/`
and version folders, and the version switcher lists only those.

## Release

Build the release archive, `bin/release/kRPC-KSA-0.1.0.zip`, and its `SHA256SUMS`:

```bash
experimental/ksa/release.sh
```

The archive holds the readme and licenses, and three folders:

 * `kRPC/` - the mod folder, without `TestingTools`, which players copy into the KSA mods folder.
   It sits at the top of the archive, with its own copy of the licenses, as the Borea mod manager
   installs only the folder holding `mod.toml`.
 * `stubs/` - the client stubs and service definitions.
 * `examples/` - the example scripts.

The documentation is published online rather than in the archive, see `publish-docs.sh`.

The mod is under the LGPL v3, as `KRPC.Core` is. The archive includes the licenses of the bundled
`Google.Protobuf` and `System.IO.Ports` binaries.

## Install and Run

Install StarMap into `../StarMap` beside the repo, configured for `KSA_DIR`:

```bash
experimental/ksa/install-starmap.sh
```

Install the mod into the KSA user mods folder and enable it in the game's `manifest.toml`:

```bash
experimental/ksa/build.sh --install
```

The mods folder is `<Documents>/My Games/Kitten Space Agency/mods`, or `KSA_MODS_DIR`. Launch the
game through StarMap:

```bash
experimental/ksa/run-ksa.sh
```

The StarMap release targets Windows. `run-ksa.sh` runs it with `dotnet`, with the KSA folder on
`LD_LIBRARY_PATH` for the game's native libraries. `run-ksa.bat` runs `StarMap.exe`.

The game opens a configuration window before loading. Untick "Always Show" in it for the tests
to run unattended.

The server starts when the game has loaded its mods. By default it listens on `127.0.0.1`, ports
50000 and 50001, and accepts every connection. The kRPC menu in the game's menu bar opens the
server window, which edits the settings and saves them to `kRPC/settings.toml`.

## Clients

Clients build the `KSA` service from the definitions the server sends. The Python client does this
on connect, as it has no pre-generated stubs for the service:

```python
import krpc
conn = krpc.connect()
vehicle = conn.ksa.active_vehicle
print(vehicle.name, vehicle.flight.mean_altitude)
```

`examples/telemetry.py` prints telemetry for the active vehicle. `examples/launch_into_orbit.py`
launches the Rocket from Cape Canaveral into orbit. `examples/raise_orbit.py` raises an orbit
with two burns that the script flies, and `examples/circularize.py` circularizes it with a burn
that the flight computer executes. Lua also builds services at run time.

The C#, C++, C-nano and Java clients use the stubs in `bin/stubs`, which the release archive
holds as `stubs`, together with the kRPC 0.7.0 client libraries. Its readme shows each
language.

## Server

 * kRPC runs once per frame, in a Harmony prefix on `Universe.ExecuteNextVehicleSolvers`. That
   point is on the main thread, after the frame's solver results and player input are applied,
   and before the next solver step is queued. The vehicle solver runs on a worker thread from
   there until the next frame.
 * A frame that does not reach that point updates the server from StarMap's after-frame hook.
 * The `Flight` scene is a loaded system with a controlled vehicle. Otherwise the scene is `None`.
 * `KRPC.Paused` sets the simulation speed to zero, and restores it.
 * Log output goes to the KSA log, under the `kRPC` category.
 * On .NET, closing a `TcpClient` sets its socket to null, so `KRPC.Core`'s connected check
   throws for a client that disconnects during a call that yields. A Harmony finalizer treats
   it as disconnected, as the check does for a disposed socket on Mono.

## Service

| Class | Members |
|---|---|
| `KSA` | `ActiveVehicle`, `Vehicles`, `Bodies`, `UT`, `SimulationSpeed`, `WarpTo()`, `LaunchVehicle()`, `TransformPosition()`, `TransformDirection()`, `TransformRotation()`, `TransformVelocity()` |
| `Vehicle` | `Name`, `Situation`, `HasLaunched`, `LaunchTime`, `Mass`, `PropellantMass`, `Thrust`, `AvailableThrust`, `SpecificImpulse`, `Parts`, `RootPart`, `Resources`, `DeltaV`, `Sequences`, `TargetVehicle`, `TargetBody`, `Burns`, `AddBurn()`, `Orbit`, `Flight`, `Control`, `AutoPilot`, `ReferenceFrame`, `SurfaceReferenceFrame`, `OrbitalReferenceFrame`, `Position()`, `Velocity()`, `Direction()`, `Rotation()`, `AngularVelocity()` |
| `Sequence` | `Number`, `Activated`, `DeltaV`, `Thrust`, `SpecificImpulse`, `BurnTime`, `StartMass`, `EndMass`, `PropellantMass` |
| `Orbit` | `Body`, `Apoapsis`, `Periapsis`, `ApoapsisAltitude`, `PeriapsisAltitude`, `SemiMajorAxis`, `Speed`, `Radius`, `Period`, `TimeToApoapsis`, `TimeToPeriapsis`, `Eccentricity`, `Inclination`, `LongitudeOfAscendingNode`, `ArgumentOfPeriapsis`, `TrueAnomaly`, `MeanAnomaly`, `TrueAnomalyAtUT()`, `UTAtTrueAnomaly()`, `RadiusAt()`, `PositionAt()`, `VelocityAt()`, `StartUT`, `EndUT`, `EndTransition`, `TimeToSOIChange`, `NextOrbit` |
| `OrbitTransition` | `None`, `Encounter`, `Escape`, `Impact`, `Burn` |
| `Body` | `Name`, `Mass`, `GravitationalParameter`, `Radius`, `AtmosphereRadius`, `SurfaceGravity`, `RotationalSpeed`, `RotationalPeriod`, `SphereOfInfluence`, `Parent`, `Satellites`, `Orbit`, `SurfaceHeight()`, `BedrockHeight()`, `LaunchSites`, `ReferenceFrame`, `NonRotatingReferenceFrame`, `Position()`, `Velocity()`, `Rotation()`, `AngularVelocity()`, `SurfacePosition()` |
| `ReferenceFrame` | Obtained from vehicles and bodies |
| `Flight` | `MeanAltitude`, `SurfaceAltitude`, `BedrockAltitude`, `Elevation`, `TimeToImpact`, `Latitude`, `Longitude`, `Speed`, `HorizontalSpeed`, `VerticalSpeed`, `Pitch`, `Heading`, `Roll`, `StaticPressure`, `AtmosphereDensity`, `DynamicPressure`, `GForce`, `AngleOfAttack`, `SideslipAngle` |
| `Control` | `Throttle`, `EngineOn`, `Stabilization`, `RCS`, `ActiveSequence`, `ActivateNextSequence()` |
| `AutoPilot` | `Engage()`, `Disengage()`, `Engaged`, `Mode`, `TargetPitch`, `TargetHeading`, `TargetRoll`, `TargetPitchAndHeading()`, `TargetDirection()`, `Error`, `RollError`, `Wait()`, `AutoBurn` |
| `Situation` | KSA's eight situations, from `Maneuvering` to `Bottomed` |
| `AutoPilotMode` | `Attitude`, `Hold`, orbital and surface prograde, retrograde, normal and radial |
| `Burn` | `UT`, `Prograde`, `Normal`, `Radial`, `DeltaV`, `RemainingDeltaV`, `IgnitionTime`, `BurnTime`, `Orbit`, `Direction()`, `Remove()` |
| `Part` | `Name`, `Title`, `Vehicle`, `Parent`, `Children`, `Sequence`, `Mass`, `DryMass`, `Resources`, `Position()`, `Rotation()`, `Engine`, `RCS`, `Decoupler`, `Parachute`, `DockingPort` |
| `Resource` | `Name`, `Amount`, `Max` |
| `Engine`, `RCS` | `Part`, `Active`; engines also `HasPropellant`, `Thrust`, `AvailableThrust`, `SpecificImpulse` |
| `Decoupler` | `Part`, `Enabled`, `Decoupled`, `Decouple()` |
| `Parachute` | `Part`, `State`, `Fill`, `DeployAltitude`, `CanCut`, `Deploy()`, `Cut()` |
| `DockingPort` | `Part`, `Docked`, `DockedPart`, `Undock()` |

 * A `Vehicle` holds the game's vehicle object, as a KSA vehicle id is its name. It throws once the
   vehicle leaves the system, including after docking and undocking.
 * Bodies are spheres. Altitudes are above the mean radius, and inclination is against the
   body's equator.
 * `Throttle` and `EngineOn` write the vehicle's manual inputs through reflection, as KSA has no
   setter for them.
 * `ActivateNextSequence` returns the vehicles that appeared within three updates of activation.
 * `AutoPilot` is the game's flight computer. `Attitude` mode is a custom pitch, heading and roll
   target, and the other modes are the flight computer's track targets. A NaN
   `TargetRoll` leaves roll free. `Wait` returns once the errors are within twice the angle
   deadband.
 * The solver step copies the flight computer settings in when it is queued, and back out when
   its results are applied. Writes from the server land between the two.
 * Pitch is positive above the horizon, and heading is clockwise from north. Roll is zero with the
   vehicle's +z axis toward the ground. `test_autopilot.py` checks pitch and heading against
   the game's prograde, normal and radial targets.
 * `GForce` is the measured acceleration, which excludes gravity. It is zero in free fall.
 * `Thrust` and `SpecificImpulse` sum the active engines' performance at their commanded and at
   full throttle, at the current pressure.
 * `WarpTo` uses the game's auto-warp, and returns when the warp ends.
 * `LaunchVehicle` places a default or saved vehicle on a launch pad, as the game's launch menu
   does.
 * A vehicle's `Orbit` follows the vehicle. `NextOrbit` and `Burn.Orbit` are snapshots of the
   game's patched-conic prediction, which it updates on a worker thread.
 * Part modules change through the game's input events, which apply at the start of the next
   frame. The setters and actions wait for the change, up to ten updates. KSA has no landing
   leg or wheel modules.
 * Resources are the substances in tanks, in kilograms. Sea level is a single height per body,
   so `SurfaceHeight` is zero over inland depressions such as the Dead Sea.
 * A `Sequence` is KSA's stage. Its performance is the game's prediction, which the game
   recomputes in flight only while its sequence or engine windows are open. The service
   recomputes it at most once per game time.
 * Reference frames go through the game's ecliptic frame, which every object can convert to.
   Body frames have z along the rotation axis. The vehicle frame is x forward, y right and
   z down, the surface frame east, north and up, and the orbital frame prograde, normal and
   radial.
 * A `Burn` is KSA's maneuver node, with its change in velocity in the orbital frame at the
   burn. The flight computer times the next burn in its solver step, so `AddBurn` waits for
   that. `AutoBurn` is the flight computer's auto-burn mode.

## Tests

Python tests that drive a live game, run with pytest. `test.sh` builds and installs the mod with
the `TestingTools` service, then runs the tests. Arguments go to pytest:

```bash
experimental/ksa/test.sh                        # launch KSA, run every test, quit
experimental/ksa/test.sh test/test_orbit.py -v  # one file
experimental/ksa/test.sh -k test_rename         # by name
experimental/ksa/test.sh --no-launch            # against a game started with run-ksa.sh
```

 * With no server running, the harness launches KSA through `run-ksa.sh`, or `run-ksa.bat` on
   Windows, logging to `bin/ksa.log`. It quits the game when the session ends.
 * A launched game is saved as `krpctest` once it has loaded the system. Each test class loads
   that save, so starts from the game's default start state. `--no-launch` needs the save from
   an earlier launched run.
 * The client is `krpc` 0.7.0 from PyPI, in `bin/test-venv`.
 * The harness saves and loads with `KSA.Save` and `KSA.Load`. `Load` loads at the end of the
   frame, where the game's menus load saves, and returns once the load is done. The game is
   paused after a load, and the harness unpauses it.
 * `TestingTools.Quit` quits the game.
 * `TestingTools.RegisterVehicle` adds a vehicle in `testing/vehicles` to the game's default
   vehicles. `test_parachute.py` launches single parachute parts from there.
 * `build.sh --install` without `--testing` removes `TestingTools` from the installed mod.
 * `test_examples.py` flies `examples/launch_into_orbit.py` to orbit, which takes about eight
   minutes. It is marked `slow`, so `-m "not slow"` skips it.

All the tests pass on Linux with KSA 2026.10.25.5707. With KSA 2026.10.19.5638 they also passed
on Windows, and the release archive built on Linux installed and ran there.

## Open Questions

 * `Flight` attitude uses the helm frame, and the flight computer steers the thrust frame while
   the engines burn. The two agree on the Rocket, and may differ on other vehicles.
 * Manual rotation input. KSA's pitch, yaw and roll inputs are on/off flags, which the game clears
   before the solver reads them unless the player is flying the vehicle. The auto-pilot steers
   instead.
