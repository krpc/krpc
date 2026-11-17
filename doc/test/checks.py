"""The hooks that act on the game while an example runs, and the checks of the game state once
it has finished. Each takes the running test case."""

from __future__ import annotations

from collections.abc import Callable
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from doc.test.harness import ExampleTestCase as TestCase


# Checks


def controlling(title: str) -> Callable[[TestCase], None]:
    def check(test: TestCase) -> None:
        parts = test.connect().space_center.active_vessel.parts
        test.assertEqual(title, parts.controlling.title)

    return check


def parachutes_deployed(test: TestCase) -> None:
    parachutes = test.connect().space_center.active_vessel.parts.parachutes
    test.assertTrue(parachutes)
    for parachute in parachutes:
        test.assertTrue(parachute.deployed)


# The values and lines the examples print


def specific_impulse(test: TestCase) -> list[float]:
    return [test.connect().space_center.active_vessel.specific_impulse]
