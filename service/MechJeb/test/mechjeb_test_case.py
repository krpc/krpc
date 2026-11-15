"""The base classes of the MechJeb tests.

This file is not named test_* so pytest does not collect it. The test modules import it by
its fully-qualified name, as service.SpaceCenter.test.resources_equivalence is imported.
"""

import os

from krpctools.game.ksp import TestCase

CRAFT = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..",
    "..",
    "SpaceCenter",
    "test",
    "craft",
)


class MechJebTestCase(TestCase):
    """A vessel with MechJeb on the launch pad."""

    mods = ["MechJeb"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("Basic", directory=CRAFT)
        cls.conn = cls.connect()
        cls.mj = cls.conn.mech_jeb
        cls.sc = cls.conn.space_center
        cls.wait_until(lambda: cls.mj.ready)

    def assert_settings(self, settings):
        """Set and restore each setting, given as (object, attribute, value to set)."""
        for obj, name, value in settings:
            with self.subTest(cls=type(obj).__name__, member=name):
                old = getattr(obj, name)
                self.assertNotEqual(value, old)
                setattr(obj, name, value)
                if isinstance(value, float):
                    self.assertAlmostEqual(value, getattr(obj, name), places=4)
                else:
                    self.assertEqual(value, getattr(obj, name))
                setattr(obj, name, old)
                self.assertEqual(old, getattr(obj, name))


class MechJebInOrbitTestCase(MechJebTestCase):
    """A vessel with MechJeb in a 100 km circular orbit of Kerbin, with its engine active."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.wait_until(lambda: cls.mj.ready)
        cls.vessel.control.activate_next_stage()
        cls.planner = cls.mj.maneuver_planner

    def tearDown(self):
        self.vessel.control.remove_nodes()
