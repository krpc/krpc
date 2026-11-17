"""The types the manifest declares the examples with.

Nothing here imports krpc, so the coverage test reads the manifest without a game."""

from __future__ import annotations

import pathlib
from collections.abc import Callable
from dataclasses import dataclass, field, replace
from typing import Any

# The languages, by the extension of an example's file
LANGUAGES = {
    ".py": "python",
    ".cs": "csharp",
    ".cpp": "cpp",
    ".c": "cnano",
    ".java": "java",
    ".lua": "lua",
    ".js": "js",
}

# The client guide folders, whose examples share the "client/" keys
CLIENT_FOLDERS = set(LANGUAGES.values())

# The server-side C# sources, which are not client programs
SERVER_SUFFIX = ".lib.cs"


def example_key(path: pathlib.PurePath) -> tuple[str, str] | None:
    """The language and key of an example, from its path under doc/src/scripts.

    None for a file that is not an example program."""
    if path.name.endswith(SERVER_SUFFIX) or path.suffix not in LANGUAGES:
        return None
    parts = path.with_suffix("").parts
    if parts[0] in CLIENT_FOLDERS:
        parts = ("client", *parts[1:])
    return LANGUAGES[path.suffix], "/".join(parts)


def example_path(key: str, language: str) -> pathlib.PurePosixPath:
    """The path of an example's file for one language, under doc/src/scripts."""
    extension = next(ext for ext, name in LANGUAGES.items() if name == language)
    folder, _, rest = key.partition("/")
    if folder == "client":
        key = language + "/" + rest
    return pathlib.PurePosixPath(key + extension)


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
    # Relaunch before each example, for examples that stage, crash or change scene
    fresh: bool = False
    mods: tuple[str, ...] = ()
    # Further craft the examples launch by name, as name to the craft staged under it
    ships: tuple[tuple[str, str], ...] = ()


# How an example runs


@dataclass(frozen=True)
class Exit:
    """Exits with code 0 within the timeout, in seconds."""

    timeout: float = 60


@dataclass(frozen=True)
class Loop:
    """Still running after the given seconds, having printed. The harness then stops it."""

    seconds: float = 5
    prints: bool = True


@dataclass(frozen=True)
class Interactive:
    """A hook acts on the game once the example has connected, or once its output matches
    ready, then `then` applies."""

    during: Callable[[Any], None]
    then: Exit | Loop = Exit()
    ready: str | None = None


Run = Exit | Loop | Interactive


@dataclass(frozen=True)
class Example:
    key: str
    setup: str
    run: Run = Exit()
    # Run against the game before the example starts
    prepare: Callable[[Any], None] | None = None
    # Regular expressions the output must match
    expect_output: tuple[str, ...] = ()
    # Run against the game once the example has finished
    check: Callable[[Any], None] | None = None
    # A long flight, deselected with -m "not slow"
    slow: bool = False
    # Field overrides for one language, such as a language that polls where others wait
    per_language: dict[str, dict[str, Any]] = field(default_factory=dict)

    def for_language(self, language: str) -> Example:
        overrides = self.per_language.get(language)
        if not overrides:
            return self
        return replace(self, **overrides)
