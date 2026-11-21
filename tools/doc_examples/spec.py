"""The types a game's manifest declares its documentation examples with, and how an example's
key maps to its files.

Nothing here imports krpc, so the coverage test reads a manifest without a game."""

from __future__ import annotations

import pathlib
from collections.abc import Callable, Sequence
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

# A number as the clients print one
NUMBER = r"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?"


@dataclass(frozen=True)
class Values:
    """The numbers the output must print, as the game gives them once the example has
    finished. Each match of the pattern's group is one number, in order."""

    expected: Callable[[Any], Sequence[float]]
    pattern: str = "(" + NUMBER + ")"
    relative: float = 0.01
    absolute: float = 0


@dataclass(frozen=True)
class Example:
    key: str
    # The name of the setup in the game's manifest
    setup: str
    run: Run = Exit()
    # Run against the game before the example starts
    prepare: Callable[[Any], None] | None = None
    # Regular expressions the output must match
    expect_output: tuple[str, ...] = ()
    # Regular expressions the error output must match, for an example that shows a failure.
    # With none, the error output must be empty.
    expect_errors: tuple[str, ...] = ()
    # Run against the game once the example has finished
    check: Callable[[Any], None] | None = None
    # The numbers the output must print
    values: Values | None = None
    # The lines the output must print, in any order, as the game gives them once the example
    # has finished. The whitespace around and within each line is not compared.
    lines: Callable[[Any], Sequence[str]] | None = None
    # Records the game while the example runs, and checks the recording once it has
    # finished: a tools.doc_examples.monitor.Monitor, made from the test case
    monitor: Callable[[Any], Any] | None = None
    # A long flight, deselected with -m "not slow"
    slow: bool = False
    # Field overrides for one language, such as a language that polls where others wait
    per_language: dict[str, dict[str, Any]] = field(default_factory=dict)

    def for_language(self, language: str) -> Example:
        overrides = self.per_language.get(language)
        if not overrides:
            return self
        return replace(self, **overrides)
