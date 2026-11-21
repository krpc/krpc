"""The hooks that act on the game before an example runs, and the checks of the game state once
it has finished. Each takes the running test case."""

from __future__ import annotations

import math
from collections.abc import Callable
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from experimental.ksa.doc.test.harness import ExampleTestCase as TestCase


# Hooks


def circular_orbit(altitude: float) -> Callable[[TestCase], None]:
    """Places the active vehicle in a circular orbit, at an altitude in meters."""

    def hook(test: TestCase) -> None:
        test.place(altitude, altitude)

    return hook


def out_of_propellant(test: TestCase) -> None:
    """Ignites the active vehicle's engines and empties its tanks."""
    vehicle = test.ksa.active_vehicle
    vehicle.control.activate_next_sequence()
    vehicle.control.throttle = 1
    vehicle.control.engine_on = True
    test.ksa.Debug.empty_resources(vehicle)


# Checks


def altitude_above(meters: float) -> Callable[[TestCase], None]:
    """A check that the active vehicle is above an altitude."""

    def check(test: TestCase) -> None:
        altitude = test.ksa.active_vehicle.flight.mean_altitude
        test.assertGreater(altitude, meters)

    return check


def in_orbit(test: TestCase) -> None:
    """A check that the active vehicle's periapsis is above the atmosphere."""
    orbit = test.ksa.active_vehicle.orbit
    body = orbit.body
    test.assertGreater(orbit.periapsis_altitude, body.atmosphere_radius - body.radius)


def circular(altitude: float, tolerance: float) -> Callable[[TestCase], None]:
    """A check that the active vehicle's orbit is circular, at an altitude in meters, within a
    fraction of it."""

    def check(test: TestCase) -> None:
        orbit = test.ksa.active_vehicle.orbit
        for value in (orbit.periapsis_altitude, orbit.apoapsis_altitude):
            test.assertTrue(
                math.isclose(value, altitude, rel_tol=tolerance),
                "periapsis %.0f m, apoapsis %.0f m"
                % (orbit.periapsis_altitude, orbit.apoapsis_altitude),
            )

    return check


def all_of(*checks: Callable[[TestCase], None]) -> Callable[[TestCase], None]:
    """A check that runs each of the checks."""

    def check(test: TestCase) -> None:
        for each in checks:
            each(test)

    return check
