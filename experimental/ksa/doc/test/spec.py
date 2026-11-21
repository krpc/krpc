"""The game states the kRPC for KSA examples run in.

Nothing here imports krpc, so the coverage test reads the manifest without a game."""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class Orbit:
    """An equatorial orbit of Earth, by its altitudes in meters."""

    periapsis: float
    apoapsis: float
    # Where the vehicle is placed, in radians from the periapsis
    mean_anomaly: float = 0


@dataclass(frozen=True)
class Setup:
    """The game state a group of examples runs in. Each loads the krpctest save first."""

    # Launch the Rocket from the launch site, as the active vehicle
    launch: bool = False
    # Then place the active vehicle in this orbit
    orbit: Orbit | None = None
    # Set up again before each example, for examples that fly, stage or launch
    fresh: bool = False
