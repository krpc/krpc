# krpctest

Utilities for running kRPC's service integration tests — the main entry point for testing
RPCs against a running copy of Kerbal Space Program.

Installing the package provides:

- `krpc-install` — builds the `//:krpc` release archive and the `TestingTools` add-on, and
  installs them into a KSP `GameData` directory (the install given by `KSP_DIR` / `--ksp-dir`).
- `krpc-run-ksp` — installs the mod, launches KSP, and streams its kRPC log output to the
  terminal. Its `--load-*` options drive the auto-load behaviour documented below.
- a pytest plugin — so `pytest` discovers and runs the `krpctest.TestCase` tests natively,
  launching and managing KSP automatically.

From a checkout of the kRPC repository there is no need to install the package: `bazel run
//:test-ingame` runs the tests and `bazel run //:run-ksp` runs the game, taking this package and
the client from the build. See the [Development Guide](../../Development-Guide.md) for both ways
of running the test suite.

## TestingTools

`TestingTools` is a development-only kRPC add-on installed by `krpc-install`. It is used by the
service tests and by local KSP runs; it is not included in the release zip.

### Auto-load CLI arguments

The add-on auto-loads a save when KSP reaches the main menu, but only when at least one
`--krpctest-load-*` argument is supplied. With no such arguments it does nothing and KSP stays at
the main menu:

```sh
# No auto-load arguments: KSP stays at the main menu.
krpc-run-ksp
```

To load `saves/default/persistent.sfs` into the Space Center, request it explicitly:

```sh
krpc-run-ksp --load-game=default
```

Loading a save does not focus a vessel on its own: add `--load-vessel` to switch to an existing
vessel, or `--load-craft` to launch a craft. When a load is requested, any argument left out uses
its default from the table below.

The auto-load behavior can be configured through `krpc-run-ksp`:

```sh
krpc-run-ksp \
  --load-game=krpctest \
  --load-save=persistent \
  --load-craft=Parts \
  --load-craft-directory=VAB \
  --load-craft-fixture-dir="$PWD/service/SpaceCenter/test/craft"
```

The runner exposes each auto-load argument as an explicit `--load-*` option (run
`krpc-run-ksp --help` to list them) and forwards it to KSP as the matching `--krpctest-load-*`
argument below.

Supported arguments (as parsed by the add-on; `krpc-run-ksp` exposes each as the matching
`--load-*` option):

| Argument | Default | Description |
| --- | --- | --- |
| `--krpctest-load-game=<folder>` | `default` | Save folder under `saves/`. |
| `--krpctest-load-save=<name>` | `persistent` | Save file name without `.sfs`. |
| `--krpctest-load-vessel=<index>` | none | Vessel index to focus after loading the save. When omitted, no vessel is focused and the save loads into the Space Center. An out-of-range index is a fatal error (see [Error handling](#error-handling)). Ignored when craft launch is requested. |
| `--krpctest-load-craft=<name>` | none | Craft to launch after loading the save. Accepts `Parts` or `Parts.craft`. |
| `--krpctest-load-craft-directory=VAB\|SPH` | inferred | KSP craft directory and launch facility. `VAB` uses `Ships/VAB`; `SPH` uses `Ships/SPH`. |
| `--krpctest-load-craft-fixture-dir=<path>` | none | Source directory of fixture `.craft` files to stage into the save. When omitted, the craft is loaded from the save's own `Ships` directory. |
| `--krpctest-load-launch-site=<site>` | by craft directory | Launch site passed to KSP, for example `LaunchPad` or `Runway`. |

Craft launch takes precedence over vessel switching. If `--krpctest-load-craft` is provided,
TestingTools launches that craft from Space Center, staging it from a fixture directory first when
`--krpctest-load-craft-fixture-dir` is given.

### Craft launch

Craft launch resolves the craft in one of two ways:

- **Staged from a fixture directory.** When `--krpctest-load-craft-fixture-dir` is given and
  contains `<craft>.craft`, TestingTools copies it (and `<craft>.loadmeta`, when present) into
  `saves/<game>/Ships/<craft-directory>/` before launching. This mirrors
  `krpctest.TestCase.launch_vessel_from_vab`.
- **Loaded from the save.** Otherwise, the craft is launched directly from
  `saves/<game>/Ships/<craft-directory>/` and must already exist there, having been built in the
  editor or staged by a previous run. This is also the fallback when the craft is not found in the
  fixture directory.

Launch uses kRPC's internal SpaceCenter launch helper, after the same pre-flight checks as the
`LaunchVessel` RPC.

If the craft cannot be staged or found, or if the launch itself fails, this is a fatal error:
TestingTools quits KSP rather than loading into the Space Center without the requested craft (see
[Error handling](#error-handling)).

`--krpctest-load-craft-fixture-dir` is the source directory for fixture craft files. It is not the
KSP craft directory. Use `--krpctest-load-craft-directory=VAB` or `SPH` to choose where the craft
is staged and which editor facility KSP uses for launch checks. Relative fixture paths are resolved
by the KSP process; with `krpc-run-ksp`, that means relative to `KSP_DIR`, not the repository root.

Default inference:

| Inputs | Resolved craft directory | Resolved launch site |
| --- | --- | --- |
| no craft directory, no launch site | `VAB` | `LaunchPad` |
| `--krpctest-load-launch-site=Runway` | `SPH` | `Runway` |
| `--krpctest-load-craft-directory=VAB` | `VAB` | `LaunchPad` |
| `--krpctest-load-craft-directory=SPH` | `SPH` | `Runway` |

Examples:

```sh
# Stage the Parts fixture from the repo into the save, then launch it.
krpc-run-ksp \
  --load-game=krpctest \
  --load-save=persistent \
  --load-craft=Parts \
  --load-craft-fixture-dir="$PWD/service/SpaceCenter/test/craft"
```

```sh
# Launch a craft already saved under saves/krpctest/Ships/VAB.
krpc-run-ksp \
  --load-game=krpctest \
  --load-save=persistent \
  --load-craft=Parts
```

```sh
# Stage a spaceplane fixture and launch it from the SPH/Runway.
krpc-run-ksp \
  --load-game=krpctest \
  --load-save=persistent \
  --load-craft=Spaceplane \
  --load-craft-directory=SPH \
  --load-craft-fixture-dir="$PWD/service/SpaceCenter/test/craft"
```

### Vessel switching

To load a save and switch to a specific existing vessel:

```sh
krpc-run-ksp \
  --load-game=krpctest \
  --load-save=persistent \
  --load-vessel=0
```

If the requested vessel index is out of range for the save, this is a fatal error (see
[Error handling](#error-handling)).

### Error handling

TestingTools fails loudly rather than silently doing something other than what was asked. If any
`--krpctest-load-*` option cannot be honoured, it logs a prominent `FATAL` error (tagged
`[kRPC testing tools]`, so it also shows in the `krpc-run-ksp` terminal output) and quits KSP, so
the launch fails fast instead of leaving you at a stuck load screen with no idea why. `krpc-run-ksp`
returns when KSP exits, and the test framework reports `KSP exited before the kRPC server became
available`.

The failures treated as fatal are:

- An unrecognized or empty `--krpctest-*` argument (for example a typo like
  `--krpctest-load-vesel=0`, or a `--krpctest-*` flag this version does not handle), which would
  otherwise be silently ignored.
- A malformed value: a non-integer or negative `--krpctest-load-vessel`, or a
  `--krpctest-load-craft-directory` other than `VAB`/`SPH`.
- The requested save `.sfs` (or its game configuration) fails to load, or the save upgrade pipeline
  fails.
- A requested `--krpctest-load-craft` cannot be found or staged, or fails to launch.
- A requested `--krpctest-load-vessel` index is out of range for the save.
