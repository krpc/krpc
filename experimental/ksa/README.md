# kRPC for KSA

Prototype kRPC server for Kitten Space Agency (KSA). It is a [StarMap](https://github.com/StarMapLoader/StarMap)
code mod that serves the `KRPC` service and a `KSA` service written for KSA's own model. The tests
pass in game on Linux. Windows is untested.

Built against kRPC 0.7.0, KSA 2026.10.19.5638 and StarMap 0.4.8, set in `versions.sh`.

## Layout

 * `server/` - the StarMap entry point (`Addon.cs`), settings and `mod.toml`. All StarMap and
   game lifecycle code is in `Addon.cs`.
 * `service/` - the `KSA` service.
 * `testing/` - the `TestingTools` service, installed only for the tests.
 * `test/` - integration tests, and their harness `ksatest.py`.
 * `examples/` - client scripts.
 * `doc/` - the documentation sources, and the template for the generated API pages.
 * `stubs/` - the readme for the client stubs archive.
 * `release/` - the licenses and install readme for the mod archive.
 * `build.sh` - builds the mod folder and the client stubs.
 * `docs.sh` - builds the documentation.
 * `release.sh` - builds the release archives.
 * `publish-docs.sh` - commits the docs to the `gh-pages` branch.
 * `install-starmap.sh`, `run-ksa.sh` - install StarMap, and launch the game through it on Linux.
 * `test.sh` - runs the tests.

## Build

Build the mod into `bin/kRPC.KSA`:

```bash
experimental/ksa/build.sh
```

The build uses a kRPC release, set by `krpc_version` in `versions.sh`, and needs Python 3, the .NET
10 SDK and `zip`:

 * `krpctools` from PyPI, installed into `bin/venv`, provides the .NET `KRPC.Core.dll`, the
   `ServiceDefinitions` tool and `krpc-clientgen`.
 * Both projects compile against that `KRPC.Core.dll` and the KSA assemblies in `KSA_DIR`, which
   defaults to `../KSA` beside the repo.
 * `ServiceDefinitions` writes `bin/KRPC.KSA.json`, which runs the server's service scan and
   checks the documentation.
 * `krpc-clientgen` builds client stubs from the definitions into `bin/krpc-ksa-stubs-0.1.0.zip`,
   which also holds the definitions.

Documentation comments use the markup of that release, so units are words rather than `<unit>`.

## Documentation

Build the HTML documentation into `bin/docs`, after `build.sh`:

```bash
experimental/ksa/docs.sh
experimental/ksa/docs.sh --serve   # also serve it on http://127.0.0.1:8000
```

 * `krpc-docgen` generates the API pages for each client language from `doc/api/api.tmpl` and
   `bin/KRPC.KSA.json`. `doc/order.txt` orders the members, and lists every one of them.
 * Sphinx and its extensions, at the versions of the kRPC 0.7.0 docs build, are installed into
   `bin/doc-venv` from `doc/requirements.txt`.
 * `examples/` is copied into the sources as `scripts/`, for the tutorial to include.
 * The pages are static, so `bin/docs/index.html` also opens straight from disk.

`publish-docs.sh` commits the docs into the `gh-pages` branch under `experimental/ksa/`, in a
worktree at `bin/gh-pages`, and `--push` pushes the commit. They are served at
<https://krpc.github.io/krpc/experimental/ksa/>. The docs workflow replaces only its own `dev/`
and version folders, and the version switcher lists only those.

## Release

Build the release archives into `bin/release`:

```bash
experimental/ksa/release.sh
```

 * `kRPC.KSA-0.1.0.zip` - the mod folder, without `TestingTools`, with the licenses and an install
   readme.
 * `krpc-ksa-stubs-0.1.0.zip` - the client stubs and service definitions.
 * `krpc-ksa-docs-0.1.0.zip` - the HTML documentation.
 * `SHA256SUMS`.

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
`LD_LIBRARY_PATH` for the game's native libraries.

The game opens a configuration window before loading. Untick "Always Show" in it for the tests
to run unattended.

The server starts when the game has loaded its mods. Server settings are in
`kRPC.KSA/settings.toml`. By default it listens on `127.0.0.1`, ports 50000 and 50001, and
accepts every connection.

## Clients

Clients build the `KSA` service from the definitions the server sends. The Python client does this
on connect, as it has no pre-generated stubs for the service:

```python
import krpc
conn = krpc.connect()
vehicle = conn.ksa.active_vehicle
print(vehicle.name, vehicle.flight.mean_altitude)
```

`examples/telemetry.py` prints telemetry for the active vehicle, and `examples/raise_orbit.py`
raises the orbit of the active vehicle with two burns. Lua also builds services at run
time.

The C#, C++, C-nano and Java clients use the stubs in `bin/krpc-ksa-stubs-0.1.0.zip`, together
with the kRPC 0.7.0 client libraries. Its readme shows each language.

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
| `KSA` | `ActiveVehicle`, `Vehicles`, `Bodies`, `UT`, `SimulationSpeed`, `WarpTo()`, `TransformPosition()`, `TransformDirection()`, `TransformRotation()`, `TransformVelocity()` |
| `Vehicle` | `Name`, `Situation`, `HasLaunched`, `LaunchTime`, `Mass`, `PropellantMass`, `Thrust`, `AvailableThrust`, `SpecificImpulse`, `DeltaV`, `Sequences`, `TargetVehicle`, `TargetBody`, `Orbit`, `Flight`, `Control`, `AutoPilot`, `ReferenceFrame`, `SurfaceReferenceFrame`, `OrbitalReferenceFrame`, `Position()`, `Velocity()`, `Direction()`, `Rotation()`, `AngularVelocity()` |
| `Sequence` | `Number`, `Activated`, `DeltaV`, `Thrust`, `SpecificImpulse`, `BurnTime`, `StartMass`, `EndMass`, `PropellantMass` |
| `Orbit` | `Body`, `Apoapsis`, `Periapsis`, `ApoapsisAltitude`, `PeriapsisAltitude`, `SemiMajorAxis`, `Speed`, `Period`, `TimeToApoapsis`, `TimeToPeriapsis`, `Eccentricity`, `Inclination`, `LongitudeOfAscendingNode`, `ArgumentOfPeriapsis` |
| `Body` | `Name`, `Mass`, `GravitationalParameter`, `Radius`, `AtmosphereRadius`, `SurfaceGravity`, `RotationalSpeed`, `RotationalPeriod`, `SphereOfInfluence`, `Parent`, `Satellites`, `ReferenceFrame`, `NonRotatingReferenceFrame`, `Position()`, `Velocity()`, `Rotation()`, `AngularVelocity()`, `SurfacePosition()` |
| `ReferenceFrame` | Obtained from vehicles and bodies |
| `Flight` | `MeanAltitude`, `SurfaceAltitude`, `Latitude`, `Longitude`, `Speed`, `HorizontalSpeed`, `VerticalSpeed`, `Pitch`, `Heading`, `Roll`, `StaticPressure`, `AtmosphereDensity`, `DynamicPressure`, `GForce`, `AngleOfAttack`, `SideslipAngle` |
| `Control` | `Throttle`, `EngineOn`, `Stabilization`, `RCS`, `ActiveSequence`, `ActivateNextSequence()` |
| `AutoPilot` | `Engage()`, `Disengage()`, `Engaged`, `Mode`, `TargetPitch`, `TargetHeading`, `TargetRoll`, `TargetPitchAndHeading()`, `TargetDirection()`, `Error`, `RollError`, `Wait()` |
| `Situation` | KSA's eight situations, from `Maneuvering` to `Bottomed` |
| `AutoPilotMode` | `Attitude`, `Hold`, orbital and surface prograde, retrograde, normal and radial |

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
 * A `Sequence` is KSA's stage. Its performance is the game's prediction, which the game
   recomputes in flight only while its sequence or engine windows are open. The service
   recomputes it at most once per game time.
 * Reference frames go through the game's ecliptic frame, which every object can convert to.
   Body frames have z along the rotation axis. The vehicle frame is x forward, y right and
   z down, the surface frame east, north and up, and the orbital frame prograde, normal and
   radial.

## Tests

Python tests that drive a live game, run with pytest. `test.sh` builds and installs the mod with
the `TestingTools` service, then runs the tests. Arguments go to pytest:

```bash
experimental/ksa/test.sh                        # launch KSA, run every test, quit
experimental/ksa/test.sh test/test_orbit.py -v  # one file
experimental/ksa/test.sh -k test_rename         # by name
experimental/ksa/test.sh --no-launch            # against a game started with run-ksa.sh
```

 * With no server running, the harness launches KSA through `run-ksa.sh`, logging to
   `bin/ksa.log`. It quits the game when the session ends.
 * A launched game is saved as `krpctest` once it has loaded the system. Each test class loads
   that save, so starts from the game's default start state. `--no-launch` needs the save from
   an earlier launched run.
 * The client is `krpc` 0.7.0 from PyPI, in `bin/test-venv`.
 * `TestingTools` saves, loads and quits the game. `LoadSave` loads at the end of the frame,
   where the game's menus load saves, and returns once the load is done. The game is paused
   after a load, and the harness unpauses it.
 * `build.sh --install` without `--testing` removes `TestingTools` from the installed mod.

All the tests pass on Linux, with KSA 2026.10.19.5638, and `examples/raise_orbit.py` raises the
Rocket's orbit from 207 km to 400 km.

## Open Questions

 * `Flight` attitude uses the helm frame, and the flight computer steers the thrust frame while
   the engines burn. The two agree on the Rocket, and may differ on other vehicles.
 * Manual rotation input. KSA's pitch, yaw and roll inputs are on/off flags, which the game clears
   before the solver reads them unless the player is flying the vehicle. The auto-pilot steers
   instead.
