"""The game states the KSP examples run in.

Nothing here imports krpc, so the coverage test reads the manifest without a game."""

from __future__ import annotations

from dataclasses import dataclass

# Placements of the vessel


@dataclass(frozen=True)
class Pad:
    """On the launch pad, as launched."""


@dataclass(frozen=True)
class Orbit:
    """A circular orbit."""

    body: str = "Kerbin"
    altitude: float = 100_000


@dataclass(frozen=True)
class Flight:
    """In flight above the space center."""

    altitude: int = 5000
    speed: int = 200


@dataclass(frozen=True)
class Splashed:
    """In the sea east of the space center."""


Placement = Pad | Orbit | Flight | Splashed


@dataclass(frozen=True)
class Setup:
    """The game state a group of examples runs in."""

    # The craft to launch, from directory, relative to the repository root
    craft: str = "Basic"
    directory: str = "service/SpaceCenter/test/craft"
    editor: str = "VAB"
    placement: Placement = Orbit()
    # The scene the examples start in: flight, space_center or editor
    scene: str = "flight"
    # A quicksave made once the vessel is placed
    quicksave: bool = False
    # A quickload after the quicksave, which brings the save's record of its vessels up to
    # date. Until then, the record holds the vessels the launch removed.
    reload: bool = False
    # Relaunch before each example, for examples that stage, crash or change scene
    fresh: bool = False
    # The vessels in the game. Each after the first is the craft launched again from the
    # runway, and the last is the active vessel.
    vessels: int = 1
    mods: tuple[str, ...] = ()
    # Further craft the examples launch by name, as name to the craft staged under it
    ships: tuple[tuple[str, str], ...] = ()
