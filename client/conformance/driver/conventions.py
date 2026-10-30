"""A client's conventions file: the native error type the client raises for each mapped server
exception, and how a dynamically typed client names what it exposes. The file is kept apart from
the client, as the listings are, so a change to the client's own mapping fails the suite.

  {
    "native_types": {"System.ArgumentException": "ValueError", ...},
    "naming": {"case": "snake", "accessors": false, "exceptions": true}
  }

A key of native_types is the mapped type of a KRPC exception, the .NET exception it stands for.
A null value is an exception the client raises only as a type of its own.
A client with the error-types profile needs native_types, and one with the dynamic profile
needs naming.
"""

from __future__ import annotations

import json
from collections.abc import Callable
from dataclasses import dataclass

from krpc.utils import snake_case

# The profile each section of the file is for
SECTIONS = {"native_types": "error-types", "naming": "dynamic"}

# The function that converts a name to each case
CASES: dict[str, Callable[[str], str]] = {"snake": snake_case}


@dataclass(frozen=True)
class Naming:
    """The case of member names, whether a property is a pair of get_ and set_ accessor
    functions, and whether the client exposes exception types."""

    case: str
    accessors: bool
    exceptions: bool

    def convert(self, name: str) -> str:
        """A name in the client's case."""
        return CASES[self.case](name)


@dataclass(frozen=True)
class Conventions:
    native_types: dict[str, str | None] | None = None
    naming: Naming | None = None


def read(path: str) -> Conventions:
    """The conventions in a file, or none for an empty path."""
    if not path:
        return Conventions()
    with open(path, encoding="utf-8") as conventions_file:
        content = json.load(conventions_file)
    unknown = set(content) - set(SECTIONS)
    if unknown:
        raise ValueError("%s has unknown keys: %s" % (path, ", ".join(sorted(unknown))))
    naming = content.get("naming")
    return Conventions(
        native_types=content.get("native_types"),
        naming=None if naming is None else _naming(path, naming),
    )


def _naming(path: str, naming: dict) -> Naming:
    """The naming section of a file, checked against the cases and flags it can hold."""
    fields = {"case", "accessors", "exceptions"}
    if set(naming) != fields:
        raise ValueError(
            "%s: naming needs exactly the keys %s" % (path, ", ".join(sorted(fields)))
        )
    if naming["case"] not in CASES:
        raise ValueError(
            "%s: naming case is %r, not one of %s"
            % (path, naming["case"], ", ".join(CASES))
        )
    for flag in ("accessors", "exceptions"):
        if not isinstance(naming[flag], bool):
            raise ValueError(
                "%s: naming %s is %r, not true or false" % (path, flag, naming[flag])
            )
    return Naming(**naming)


def missing(conventions: Conventions, profiles: list[str]) -> list[str]:
    """The sections the profiles need that the conventions lack."""
    return [
        name
        for name, profile in SECTIONS.items()
        if profile in profiles and getattr(conventions, name) is None
    ]
