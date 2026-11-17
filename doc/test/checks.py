"""The hooks that act on the game while an example runs, and the checks of the game state once
it has finished. Each takes the running test case."""

from __future__ import annotations

import math
from collections.abc import Callable
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from doc.test.harness import ExampleTestCase as TestCase


# Hooks


def target_first_vessel(test: TestCase) -> None:
    """Targets the first vessel, which the examples name as some vessel, where it is not the
    active vessel."""
    space_center = test.connect().space_center
    space_center.target_vessel = space_center.vessels[0]


def climb_past(altitude: int) -> Callable[[TestCase], None]:
    """Checks that the example is still waiting for the vessel to climb, then places the
    vessel in flight above the altitude."""

    def during(test: TestCase) -> None:
        conn = test.connect()
        clients = len(conn.krpc.clients)
        test.wait(2)
        test.assertEqual(
            clients,
            len(conn.krpc.clients),
            "The example stopped waiting before the vessel climbed",
        )
        test.set_flight(altitude=altitude + 2000, speed=200)

    return during


def pitches_up(test: TestCase) -> None:
    """Waits for the example's control loop to pitch the level vessel up toward 45
    degrees."""
    control = test.connect().space_center.active_vessel.control
    test.wait_until(
        lambda: control.pitch > 0.5, timeout=10, message="the example to pitch up"
    )


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


def toggles_action_group(test: TestCase) -> None:
    """Waits to see action group 1 both on and off, as the example toggles it."""
    control = test.connect().space_center.active_vessel.control
    seen = set()

    def both() -> bool:
        seen.add(control.get_action_group(1))
        return len(seen) == 2

    test.wait_until(both, timeout=10, message="action group 1 to toggle")


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


def engines_at_half_thrust(test: TestCase) -> None:
    engines = test.connect().space_center.active_vessel.parts.engines
    test.assertTrue(engines)
    for engine in engines:
        test.assertAlmostEqual(0.5, engine.thrust_limit, places=3)


def solar_panels_extended(test: TestCase) -> None:
    space_center = test.connect().space_center
    panels = space_center.active_vessel.parts.solar_panels
    test.assertTrue(panels)
    states = (
        space_center.DeployableState.deploying,
        space_center.DeployableState.deployed,
    )
    for panel in panels:
        test.assertIn(panel.state, states)


def all_of(*checks: Callable[[TestCase], None]) -> Callable[[TestCase], None]:
    def check(test: TestCase) -> None:
        for each in checks:
            each(test)

    return check


def rails_warp_factor(factor: int) -> Callable[[TestCase], None]:
    """The factor, or the highest the game allows where the vessel is."""

    def check(test: TestCase) -> None:
        space_center = test.connect().space_center
        expected = min(factor, space_center.maximum_rails_warp_factor)
        test.wait_until(
            lambda: space_center.rails_warp_factor == expected,
            timeout=10,
            message="rails warp factor %d" % expected,
        )

    return check


def pointing(
    pitch: float, heading: float | None = None, tolerance: float = 5
) -> Callable[[TestCase], None]:
    """The vessel's pitch and heading in its surface reference frame, in degrees."""

    def check(test: TestCase) -> None:
        flight = test.connect().space_center.active_vessel.flight()
        test.assertAlmostEqual(pitch, flight.pitch, delta=tolerance)
        if heading is not None:
            error = (flight.heading - heading + 180) % 360 - 180
            test.assertAlmostEqual(0, error, delta=tolerance)

    return check


def autopilot(**settings) -> Callable[[TestCase], None]:
    """The auto-pilot's settings, by property name. A tuple compares element by element."""

    def check(test: TestCase) -> None:
        conn = test.connect()
        ap = conn.space_center.active_vessel.auto_pilot
        for name, expected in settings.items():
            if callable(expected):
                expected = expected(conn)
            value = getattr(ap, name)
            if isinstance(expected, tuple):
                for e, v in zip(expected, value, strict=True):
                    test.assertAlmostEqual(e, v, places=3, msg=name)
            elif isinstance(expected, float):
                test.assertAlmostEqual(expected, value, places=3, msg=name)
            else:
                test.assertEqual(expected, value, name)

    return check


class Quickload:
    """Records the game's loads before an example, and checks it loaded a save."""

    def __init__(self) -> None:
        self.loads = 0

    def prepare(self, test: TestCase) -> None:
        self.loads = test.connect().krpc.game_state_loads

    def check(self, test: TestCase) -> None:
        krpc = test.connect().krpc
        test.wait_until(
            lambda: (
                krpc.game_state_loads > self.loads
                and krpc.game_scene == krpc.GameScene.flight
            ),
            timeout=120,
            message="the quicksave to load",
        )


class WarpToApoapsis:
    """Puts the vessel on an elliptic orbit with its apoapsis ahead, and checks the example
    warped to just before it."""

    AHEAD = 300

    def __init__(self) -> None:
        self.start = 0.0

    def prepare(self, test: TestCase) -> None:
        space_center = test.connect().space_center
        kerbin = space_center.bodies["Kerbin"]
        # Periapsis at 100 km and apoapsis at 300 km
        radius = kerbin.equatorial_radius
        a = radius + 200_000
        period = 2 * math.pi * math.sqrt(a**3 / kerbin.gravitational_parameter)
        mean_anomaly = math.pi - 2 * math.pi * self.AHEAD / period
        self.start = space_center.ut
        test.set_orbit("Kerbin", a, 100_000 / a, 0, 0, 0, mean_anomaly, self.start)

    def check(self, test: TestCase) -> None:
        space_center = test.connect().space_center
        test.assertGreater(space_center.ut - self.start, self.AHEAD - 60)
        test.assertLess(space_center.active_vessel.orbit.time_to_apoapsis, 20)


class CallCount:
    """Checks that the example made remote procedure calls, for an example that makes its
    calls and then waits."""

    # The calls the harness makes while the example runs, with the second read of the count
    HARNESS = 2

    def __init__(self, minimum: int) -> None:
        self.minimum = minimum
        self.start = 0

    def prepare(self, test: TestCase) -> None:
        self.start = _rpcs_executed(test)

    def check(self, test: TestCase) -> None:
        calls = _rpcs_executed(test) - self.start - self.HARNESS
        test.assertGreaterEqual(calls, self.minimum, "The example made too few calls")


# The values and lines the examples print


def specific_impulse(test: TestCase) -> list[float]:
    return [test.connect().space_center.active_vessel.specific_impulse]


def maximum_rails_warp_factor(test: TestCase) -> list[float]:
    return [test.connect().space_center.maximum_rails_warp_factor]


def thrust(test: TestCase) -> list[float]:
    return [test.connect().space_center.active_vessel.thrust]


def engine_thrusts(test: TestCase) -> list[float]:
    engines = test.connect().space_center.active_vessel.parts.engines
    return [engine.thrust for engine in engines]


def burn_times(test: TestCase) -> list[float]:
    vessel = test.connect().space_center.active_vessel
    exhaust_velocity = vessel.specific_impulse * 9.82
    mass = vessel.mass
    thrust = vessel.available_thrust
    return [
        (mass - mass / math.exp(delta_v / exhaust_velocity)) * exhaust_velocity / thrust
        for delta_v in (100, 250, 400)
    ]


def angle_to_prograde(test: TestCase) -> list[float]:
    vessel = test.connect().space_center.active_vessel
    frame = vessel.orbit.body.non_rotating_reference_frame
    direction = vessel.direction(frame)
    prograde = vessel.flight(frame).prograde
    dot = sum(a * b for a, b in zip(direction, prograde, strict=True))
    return [math.degrees(math.acos(max(-1, min(1, dot))))]


def rotation(test: TestCase) -> list[float]:
    return list(test.connect().space_center.active_vessel.flight().rotation)


def prograde(test: TestCase) -> list[float]:
    return list(test.connect().space_center.active_vessel.flight().prograde)


def part_titles(test: TestCase) -> list[str]:
    parts = test.connect().space_center.active_vessel.parts.all
    return [part.title for part in parts]


def attachment_modes(test: TestCase) -> list[str]:
    parts = test.connect().space_center.active_vessel.parts.all
    return [
        part.title + " - " + ("axial" if part.axially_attached else "radial")
        for part in parts
    ]


def scene(name: str) -> Callable[[TestCase], None]:
    def check(test: TestCase) -> None:
        conn = test.connect()
        test.wait_until(
            lambda: conn.krpc.game_scene == getattr(conn.krpc.GameScene, name),
            message="the " + name + " scene",
        )

    return check


def vessel_named(name: str) -> Callable[[TestCase], None]:
    def check(test: TestCase) -> None:
        test.assertEqual(name, test.connect().space_center.active_vessel.name)

    return check
