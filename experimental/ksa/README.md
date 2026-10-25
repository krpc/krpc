# kRPC for KSA

Prototype kRPC server for Kitten Space Agency (KSA). It is a [StarMap](https://github.com/StarMapLoader/StarMap)
code mod that serves the `KRPC` service.

Built against kRPC 0.7.0, KSA 2026.10.19.5638 and StarMap 0.4.8, set in `versions.sh`.

## Layout

 * `server/` - the StarMap entry point (`Addon.cs`), settings and `mod.toml`. All StarMap and
   game lifecycle code is in `Addon.cs`.
 * `build.sh` - builds the mod folder.
 * `install-starmap.sh`, `run-ksa.sh` - install StarMap, and launch the game through it on Linux.

## Build

Build the mod into `bin/kRPC.KSA`:

```bash
experimental/ksa/build.sh
```

The build uses a kRPC release, set by `krpc_version` in `versions.sh`, and needs Python 3 and the
.NET 10 SDK:

 * `krpctools` from PyPI, installed into `bin/venv`, provides the .NET `KRPC.Core.dll`.
 * The mod compiles against that `KRPC.Core.dll` and the KSA assemblies in `KSA_DIR`, which
   defaults to `../KSA` beside the repo.

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

The server starts when the game has loaded its mods. Server settings are in
`kRPC.KSA/settings.toml`. By default it listens on `127.0.0.1`, ports 50000 and 50001, and
accepts every connection.

## Server

 * kRPC runs once per frame, in a Harmony prefix on `Universe.ExecuteNextVehicleSolvers`. That
   point is on the main thread, after the frame's solver results and player input are applied,
   and before the next solver step is queued. The vehicle solver runs on a worker thread from
   there until the next frame.
 * A frame that does not reach that point updates the server from StarMap's after-frame hook.
 * The `Flight` scene is a loaded system with a controlled vehicle. Otherwise the scene is `None`.
 * `KRPC.Paused` sets the simulation speed to zero, and restores it.
 * Log output goes to the KSA log, under the `kRPC` category.
