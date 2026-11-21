"""The manifest: how to run each kRPC for KSA documentation example, and what it must do.

Each Example covers the files of every language that share its key. coverage_test.py checks
that every example file is either declared here or listed in unsupported.txt."""

from __future__ import annotations

from experimental.ksa.doc.test import checks, flights
from experimental.ksa.doc.test.spec import Orbit, Setup
from tools.doc_examples.spec import Example, Exit, Loop

# In the order the test classes run: the shared setup first, then those set up for every example
SETUPS = {
    # The save as loaded, with its active vehicle in orbit
    "save": Setup(),
    "rocket": Setup(launch=True, fresh=True),
    # The examples launch the Rocket themselves
    "launch": Setup(fresh=True),
    # The Rocket coasting to the apoapsis the launch tutorial leaves it at, above the
    # atmosphere at 173 km
    "suborbit": Setup(
        launch=True, orbit=Orbit(100_000, 500_000, mean_anomaly=2.8), fresh=True
    ),
    # The orbit the circularization leaves it in
    "orbit": Setup(launch=True, orbit=Orbit(500_000, 500_000), fresh=True),
}

# The time a tutorial has to fly, in seconds
TUTORIAL = Exit(timeout=1800)

_LAUNCH = {
    "expect_output": (r"has launched", r"reached 10 km"),
    "check": checks.altitude_above(10_000),
}

EXAMPLES = [
    # The save
    Example("getting-started/telemetry", "save", run=Loop()),
    Example("grpc/ActiveVehicle", "save", expect_output=(r"UT \d+ s",)),
    Example("user-guide/autopilot/Point", "save", expect_output=(r"Pointing",)),
    Example("user-guide/reference-frames/Direction", "save"),
    Example("user-guide/reference-frames/Transform", "save"),
    Example("user-guide/reference-frames/Velocities", "save"),
    Example("user-guide/server-side-functions/ComputedStream", "save"),
    Example("user-guide/server-side-functions/EngineThrusts", "save"),
    Example("user-guide/server-side-functions/PropellantFraction", "save"),
    # Shows a window until its Done button is clicked
    Example("user-guide/ui/Throttle", "save", run=Loop(prints=False)),
    # The Rocket on the launch pad
    Example(
        "user-guide/object-lifetime/Decouple",
        "rocket",
        expect_output=(r"The LR91 Sea is now part of",),
    ),
    Example(
        "user-guide/server-side-functions/CapturedValue",
        "rocket",
        prepare=checks.circular_orbit(200_000),
    ),
    Example(
        "user-guide/server-side-functions/FirstEvent",
        "rocket",
        prepare=checks.circular_orbit(200_000),
        expect_output=(r"Apoapsis passed 100 km",),
    ),
    Example(
        "user-guide/server-side-functions/OutOfPropellant",
        "rocket",
        prepare=checks.out_of_propellant,
        expect_output=(r"An engine has run out of propellant",),
    ),
    Example("user-guide/server-side-functions/RunOnce", "rocket"),
    Example(
        "user-guide/timing/Throttle",
        "rocket",
        expect_output=(r"Thrust a frame later",),
    ),
    # Launches
    Example("index/Launch", "launch", **_LAUNCH),
    Example("index/LaunchGrpc", "launch", **_LAUNCH),
    Example("index/LaunchHTTP", "launch", **_LAUNCH),
    Example(
        "tutorials/launch-into-orbit/launch_into_orbit",
        "launch",
        check=checks.all_of(checks.in_orbit, checks.circular(500_000, 0.05)),
        run=TUTORIAL,
        monitor=flights.fast_ascent(500_000),
        slow=True,
    ),
    Example(
        "tutorials/launch-with-kfs/launch_with_kfs",
        "launch",
        expect_output=(r"Staging", r"staged 1"),
        run=TUTORIAL,
        monitor=flights.fast_ascent(400_000),
        slow=True,
    ),
    # Orbits
    Example(
        "tutorials/circularize/circularize",
        "suborbit",
        check=checks.circular(500_000, 0.05),
        run=TUTORIAL,
        slow=True,
    ),
    Example(
        "tutorials/raise-orbit/raise_orbit",
        "orbit",
        check=checks.circular(1_000_000, 0.05),
        run=TUTORIAL,
        slow=True,
    ),
]
