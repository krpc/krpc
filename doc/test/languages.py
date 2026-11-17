"""How to start an example program in each language.

Python runs from its source. The other languages run the binary Bazel builds for each example,
which build() builds and locates before the tests run."""

from __future__ import annotations

import os
import pathlib
import shutil
import subprocess
import sys
from collections.abc import Callable, Iterable
from dataclasses import dataclass

REPO = pathlib.Path(__file__).resolve().parent.parent.parent
SCRIPTS = REPO / "doc" / "src" / "scripts"


@dataclass(frozen=True)
class Command:
    argv: list[str]
    env: dict[str, str]


@dataclass(frozen=True)
class Language:
    # The Bazel target that builds an example, from its path under doc/src/scripts, or None
    # for a language that runs from source
    target: Callable[[pathlib.PurePath], str] | None
    command: Callable[[pathlib.Path], Command]


def _python(path: pathlib.Path) -> Command:
    # The test run's interpreter, with its path, which holds the client built from this tree
    env = dict(os.environ, PYTHONPATH=os.pathsep.join(sys.path))
    return Command([sys.executable, "-u", str(path)], env)


def _binary(path: pathlib.Path) -> Command:
    # C's standard output to a file is flushed a block at a time, so a looping example stopped
    # after a few seconds can appear to print nothing. stdbuf flushes it a line at a time.
    stdbuf = shutil.which("stdbuf")
    prefix = [stdbuf, "-oL"] if stdbuf else []
    return Command([*prefix, str(path)], dict(os.environ))


def _doc_target(prefix: str) -> Callable[[pathlib.PurePath], str]:
    return lambda path: "//doc:%s/src/scripts/%s" % (prefix, path.as_posix())


# The languages whose examples run, by name
LANGUAGES = {
    "python": Language(None, _python),
    "cpp": Language(_doc_target("compile-scripts-cpp"), _binary),
    "java": Language(_doc_target("compile-scripts-java"), _binary),
    "cnano": Language(_doc_target("run-scripts-cnano"), _binary),
    "lua": Language(_doc_target("run-scripts-lua"), _binary),
}

# The built example binaries, by target
_executables: dict[str, pathlib.Path] = {}


def build(examples: Iterable[tuple[str, pathlib.PurePath]]) -> None:
    """Builds the binaries of the given examples, as language and path, and records where they
    are."""
    targets = sorted(
        {
            target(path)
            for language, path in examples
            if (target := LANGUAGES[language].target) is not None
        }
    )
    if not targets:
        return
    subprocess.check_call(["bazel", "build", *targets], cwd=REPO)
    output = subprocess.check_output(
        [
            "bazel",
            "cquery",
            "--output=starlark",
            "--starlark:expr="
            'str(target.label) + "\\t" + target.files_to_run.executable.path',
            "set(%s)" % " ".join(targets),
        ],
        cwd=REPO,
        text=True,
    )
    # The paths are relative to the execution root
    root = pathlib.Path(
        subprocess.check_output(
            ["bazel", "info", "execution_root"], cwd=REPO, text=True
        ).strip()
    )
    for line in output.splitlines():
        label, _, path = line.partition("\t")
        # The label as cquery prints it, //doc:name or @@//doc:name
        _executables["//" + label.partition("//")[2]] = root / path


def command(language: str, path: pathlib.PurePath) -> Command:
    spec = LANGUAGES[language]
    if spec.target is None:
        return spec.command(SCRIPTS / path)
    return spec.command(_executables[spec.target(path)])
