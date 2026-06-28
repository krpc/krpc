# TestingTools

TestingTools is a development-only kRPC add-on installed by `tools/install.sh`.
It is used by service tests and local KSP runs; it is not included in the release
zip.

## Auto-load CLI arguments

The add-on auto-loads a save when KSP reaches the main menu. With no command-line
arguments, it preserves the historical behavior:

```sh
tools/run-ksp.sh
```

This loads `saves/default/persistent.sfs` and switches to the first vessel that is
not a `SpaceObject`.

The standard runners forward all arguments to KSP, so the auto-load behavior can
be configured through `tools/run-ksp.sh` or `tools/run-ksp-remote.sh`:

```sh
tools/run-ksp.sh \
  --krpc-auto-load-game=krpctest \
  --krpc-auto-load-save=persistent \
  --krpc-auto-load-craft=Parts \
  --krpc-auto-load-craft-directory=VAB \
  --krpc-auto-load-craft-fixture-dir="$PWD/service/SpaceCenter/test/craft"
```

Supported arguments:

| Argument | Default | Description |
| --- | --- | --- |
| `--krpc-auto-load-game=<folder>` | `default` | Save folder under `saves/`. |
| `--krpc-auto-load-save=<name>` | `persistent` | Save file name without `.sfs`. |
| `--krpc-auto-load-vessel=<index>` | first non-`SpaceObject` | Vessel index to focus after loading the save. Ignored when craft launch is requested. |
| `--krpc-auto-load-craft=<name>` | none | Craft to launch after loading the save. Accepts `Parts` or `Parts.craft`. |
| `--krpc-auto-load-craft-directory=VAB\|SPH` | inferred | KSP craft directory and launch facility. `VAB` uses `Ships/VAB`; `SPH` uses `Ships/SPH`. |
| `--krpc-auto-load-craft-fixture-dir=<path>` | none | Source directory of fixture `.craft` files to stage into the save. When omitted, the craft is loaded from the save's own `Ships` directory. |
| `--krpc-auto-load-launch-site=<site>` | by craft directory | Launch site passed to KSP, for example `LaunchPad` or `Runway`. |

Craft launch takes precedence over vessel switching. If `--krpc-auto-load-craft`
is provided, TestingTools launches that craft from Space Center, staging it from a
fixture directory first when `--krpc-auto-load-craft-fixture-dir` is given.

## Craft launch

Craft launch resolves the craft in one of two ways:

- **Staged from a fixture directory.** When `--krpc-auto-load-craft-fixture-dir`
  is given and contains `<craft>.craft`, TestingTools copies it (and
  `<craft>.loadmeta`, when present) into `saves/<game>/Ships/<craft-directory>/`
  before launching. This mirrors `krpctest.TestCase.launch_vessel_from_vab`.
- **Loaded from the save.** Otherwise, the craft is launched directly from
  `saves/<game>/Ships/<craft-directory>/` and must already exist there, having
  been built in the editor or staged by a previous run. This is also the fallback
  when the craft is not found in the fixture directory.

Launch uses kRPC's internal SpaceCenter launch helper, after the same pre-flight
checks as the `LaunchVessel` RPC.

If the craft cannot be staged or found, TestingTools logs a warning and still
loads the save into the Space Center without launching a craft. This keeps KSP
out of the main menu so a test client can connect and fail with a real assertion
rather than a connection timeout.

`--krpc-auto-load-craft-fixture-dir` is the source directory for fixture craft
files. It is not the KSP craft directory. Use
`--krpc-auto-load-craft-directory=VAB` or `SPH` to choose where the craft is
staged and which editor facility KSP uses for launch checks. Relative fixture
paths are resolved by the KSP process; with `tools/run-ksp.sh`, that means
relative to `KSP_DIR`, not the repository root.

Default inference:

| Inputs | Resolved craft directory | Resolved launch site |
| --- | --- | --- |
| no craft directory, no launch site | `VAB` | `LaunchPad` |
| `--krpc-auto-load-launch-site=Runway` | `SPH` | `Runway` |
| `--krpc-auto-load-craft-directory=VAB` | `VAB` | `LaunchPad` |
| `--krpc-auto-load-craft-directory=SPH` | `SPH` | `Runway` |

Examples:

```sh
# Stage the Parts fixture from the repo into the save, then launch it.
tools/run-ksp.sh \
  --krpc-auto-load-game=krpctest \
  --krpc-auto-load-save=persistent \
  --krpc-auto-load-craft=Parts \
  --krpc-auto-load-craft-fixture-dir="$PWD/service/SpaceCenter/test/craft"
```

```sh
# Launch a craft already saved under saves/krpctest/Ships/VAB.
tools/run-ksp.sh \
  --krpc-auto-load-game=krpctest \
  --krpc-auto-load-save=persistent \
  --krpc-auto-load-craft=Parts
```

```sh
# Stage a spaceplane fixture and launch it from the SPH/Runway.
tools/run-ksp.sh \
  --krpc-auto-load-game=krpctest \
  --krpc-auto-load-save=persistent \
  --krpc-auto-load-craft=Spaceplane \
  --krpc-auto-load-craft-directory=SPH \
  --krpc-auto-load-craft-fixture-dir="$PWD/service/SpaceCenter/test/craft"
```

## Vessel switching

To load a save and switch to a specific existing vessel:

```sh
tools/run-ksp.sh \
  --krpc-auto-load-game=krpctest \
  --krpc-auto-load-save=persistent \
  --krpc-auto-load-vessel=0
```

If the requested vessel index is invalid, TestingTools logs a warning and falls
back to the first non-`SpaceObject` vessel.
