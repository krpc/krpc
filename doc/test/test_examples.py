"""Runs each documentation example in game, in each language whose examples run.

A test class holds the examples that share a setup, so the game is set up once for them, and
carries the setup's mods, so the examples that need the same mods run together. Each test is
named for its language and example, as test_<language>__<example>, and marked example, with
its language, and slow for a long flight."""

from __future__ import annotations

import re
import sys
from collections.abc import Callable

import pytest

# The runner reads the output and stops the process group with POSIX calls
if sys.platform == "win32":
    pytest.skip("The examples run on Linux only", allow_module_level=True)

from doc.test import languages, listing  # noqa: E402
from doc.test.examples import EXAMPLES, SETUPS  # noqa: E402
from doc.test.harness import ExampleTestCase  # noqa: E402
from doc.test.languages import SCRIPTS  # noqa: E402
from doc.test.spec import Example, example_path  # noqa: E402

_UNSUPPORTED = listing.read()


def _test(example: Example, language: str) -> Callable[[ExampleTestCase], None]:
    def test(self: ExampleTestCase) -> None:
        self.run_example(example, language)

    marks = [pytest.mark.example, getattr(pytest.mark, language)]
    if example.slow:
        marks.append(pytest.mark.slow)
    for mark in marks:
        test = mark(test)
    return test


def _name(text: str) -> str:
    return re.sub(r"\W", "_", text)


def _classes() -> dict[str, type[ExampleTestCase]]:
    classes: dict[str, type[ExampleTestCase]] = {}
    for setup_name, setup in SETUPS.items():
        tests = {}
        for example in EXAMPLES:
            if example.setup != setup_name:
                continue
            for language in languages.LANGUAGES:
                path = example_path(example.key, language)
                if not (SCRIPTS / path).exists():
                    continue
                if listing.find(_UNSUPPORTED, language, example.key):
                    continue
                name = "test_%s__%s" % (language, _name(example.key))
                tests[name] = _test(example, language)
        if tests:
            class_name = "Test" + "".join(
                part.capitalize() for part in setup_name.split("-")
            )
            attributes = {"setup": setup, "mods": list(setup.mods), **tests}
            classes[class_name] = type(class_name, (ExampleTestCase,), attributes)
    return classes


globals().update(_classes())
