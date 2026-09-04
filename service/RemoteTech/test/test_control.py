import unittest

import krpctest


class TestControlRemoteTechConnected(krpctest.TestCase):
    """Control inputs are applied to a vessel that is controllable via RemoteTech."""

    mods = ["RemoteTech"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Probe")
        cls.remove_other_vessels()
        cls.rt = cls.connect().remote_tech
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.engine = next(iter(cls.vessel.parts.engines))
        cls.engine.active = True

    def test_throttle_applied(self):
        comms = self.rt.comms(self.vessel)
        self.assertFalse(comms.has_local_control)
        self.assertTrue(comms.has_connection)
        self.control.throttle = 1
        self.wait(1.5)
        self.assertAlmostEqual(1, self.engine.throttle, places=2)
        self.control.throttle = 0
        self.wait(1)
        self.assertAlmostEqual(0, self.engine.throttle, places=2)

    def test_pitch_applied_once(self):
        """RemoteTech runs kRPC's fly-by-wire callback as a sanctioned pilot, so the
        callback must not also be on the vessel's own callback list."""
        self.assertTrue(self.rt.comms(self.vessel).has_connection)
        self.control.sas = False
        self.control.pitch = 0.5
        self.wait(1)
        self.assertAlmostEqual(0.5, self.control.pitch, places=2)
        self.control.pitch = 0


class TestControlRemoteTechNoConnection(krpctest.TestCase):
    """Throttle is applied to a vessel that RemoteTech cannot reach.

    The uncrewed probe is placed far from Kerbin so its antenna cannot reach any
    ground station. kRPC flies the vessel as a RemoteTech sanctioned pilot, which
    the flight computer's signal delay and connection rules do not apply to, so
    the throttle follows the other control axes and is applied.
    """

    mods = ["RemoteTech"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Probe")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Jool", 20000000)
        cls.rt = cls.connect().remote_tech
        cls.vessel = cls.connect().space_center.active_vessel
        cls.control = cls.vessel.control
        cls.engine = next(iter(cls.vessel.parts.engines))
        cls.engine.active = True

    def test_throttle_applied(self):
        comms = self.rt.comms(self.vessel)
        self.assertFalse(comms.has_local_control)
        self.assertFalse(comms.has_connection)
        self.control.throttle = 1
        self.wait(1.5)
        self.assertAlmostEqual(1, self.engine.throttle, places=2)
        self.control.throttle = 0
        self.wait(1)
        self.assertAlmostEqual(0, self.engine.throttle, places=2)


if __name__ == "__main__":
    unittest.main()
