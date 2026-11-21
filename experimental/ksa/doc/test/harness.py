"""The test case the kRPC for KSA examples run in: it brings the game to an example's setup, and
runs the example with tools.doc_examples."""

from __future__ import annotations

from typing import Any, ClassVar

import ksatest

from experimental.ksa.doc.test.spec import Setup
from tools.doc_examples.harness import ExampleRunner
from tools.doc_examples.languages import Languages

# The launch site of the examples that launch the Rocket
SITE = "CCSFS LC-39A"

# The languages of the examples in experimental/ksa/doc/src/scripts
LANGUAGES = Languages(
    "//experimental/ksa/doc", "//experimental/ksa/doc/test:run-example-js"
)


class ExampleTestCase(ksatest.TestCase, ExampleRunner):
    languages = LANGUAGES
    setup: ClassVar[Setup]
    # The active vehicle the setup leaves, to tell whether an example changed it
    _vehicle: ClassVar[Any] = None

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.establish()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def setUp(self):
        if self.setup.fresh or self.changed():
            self.establish()
        else:
            self.restore()

    def tearDown(self):
        # An example that turns on the flight software leaves it on through loads
        self.conn.kfs.available = False

    @classmethod
    def establish(cls):
        cls.load_save()
        setup = cls.setup
        if setup.launch:
            cls.ksa.launch_vehicle("Rocket", SITE)
        if setup.orbit is not None:
            orbit = setup.orbit
            cls.place(orbit.periapsis, orbit.apoapsis, orbit.mean_anomaly)
        cls._vehicle = cls.ksa.active_vehicle

    @classmethod
    def place(cls, periapsis: float, apoapsis: float, mean_anomaly: float = 0):
        """Places the active vehicle in an equatorial orbit of Earth, by its altitudes in
        meters, at a mean anomaly in radians."""
        earth = cls.ksa.bodies["Earth"]
        rp = earth.radius + periapsis
        ra = earth.radius + apoapsis
        cls.ksa.Debug.set_orbit(
            cls.ksa.active_vehicle,
            earth,
            (rp + ra) / 2,
            (ra - rp) / (ra + rp),
            0,
            0,
            0,
            mean_anomaly,
            cls.ksa.ut,
        )

    def changed(self) -> bool:
        """Whether an example left the game in a state the restore cannot undo."""
        return self.ksa.active_vehicle != self._vehicle

    def restore(self):
        """Undoes the changes an example can make without changing the vehicle."""
        vehicle = self.ksa.active_vehicle
        vehicle.auto_pilot.disengage()
        vehicle.control.throttle = 0
        vehicle.control.engine_on = False
        self.ksa.UI.clear()
