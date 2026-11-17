"""The hooks that act on the game while an example runs, and the checks of the game state once
it has finished. Each takes the running test case."""

from __future__ import annotations

from collections.abc import Callable
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from doc.test.harness import ExampleTestCase as TestCase


# Hooks


def _rpcs_executed(test: TestCase) -> int:
    return test.connect().krpc.get_status().statistics.rpcs_executed


def keeps_calling(test: TestCase) -> None:
    """Checks that the example's loop makes remote procedure calls."""
    test.wait(1)
    start = _rpcs_executed(test)
    test.wait(2)
    # One of the calls is the second read of the count
    test.assertGreater(
        _rpcs_executed(test) - start, 1, "The example's loop made no calls"
    )


def screen_menu(test: TestCase) -> None:
    """Waits for the menu on the first screen, then presses its enter button, which toggles
    the lights."""
    conn = test.connect()
    vessel = conn.space_center.active_vessel
    assert vessel is not None
    monitor = conn.raster_prop_monitor.monitors(vessel)[0]
    test.wait_until(lambda: "kRPC MENU" in monitor.text, timeout=10, message="the menu")
    lights = vessel.control.lights
    monitor.press_button(monitor.buttons.index("button_ENTER"))
    test.wait_until(
        lambda: vessel.control.lights != lights,
        timeout=10,
        message="the menu to toggle the lights",
    )


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
