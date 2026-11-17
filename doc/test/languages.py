"""How to start an example program in each language."""

from __future__ import annotations

import os
import pathlib
import sys
from dataclasses import dataclass

SCRIPTS = pathlib.Path(__file__).resolve().parent.parent / "src" / "scripts"


@dataclass(frozen=True)
class Command:
    argv: list[str]
    env: dict[str, str]


def python(path: pathlib.Path) -> Command:
    # The test run's interpreter, with its path, which holds the client built from this tree
    env = dict(os.environ, PYTHONPATH=os.pathsep.join(sys.path))
    return Command([sys.executable, "-u", str(path)], env)


# The languages whose examples run, by name, with how to start one
LANGUAGES = {
    "python": python,
}


def command(language: str, path: pathlib.PurePath) -> Command:
    return LANGUAGES[language](SCRIPTS / path)
