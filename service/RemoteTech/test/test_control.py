import unittest

import krpctest


class TestControlThrottleRemoteTechConnected(krpctest.TestCase):
    """Throttle is applied to a vessel that is controllable via RemoteTech.

    Regression test for the throttle gate in PilotAddon.HandleThrottle: throttle
    must follow RemoteTech's controllability rule rather than either being
    blocked when the vessel is controllable, or leaking through when it is not.
    """

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
        # On the launch pad the uncrewed probe's antenna reaches the ground
        # station, so it is controllable and throttle should be applied.
        comms = self.rt.comms(self.vessel)
        self.assertFalse(comms.has_local_control)
        self.assertTrue(comms.has_connection)
        self.control.throttle = 1
        self.wait(1.5)
        self.assertAlmostEqual(1, self.engine.throttle, places=2)
        self.control.throttle = 0
        self.wait(1)
        self.assertAlmostEqual(0, self.engine.throttle, places=2)


class TestControlThrottleRemoteTechNoConnection(krpctest.TestCase):
    """Throttle is dropped for a vessel that is not controllable via RemoteTech.

    The uncrewed probe is placed far from Kerbin so its antenna cannot reach any
    ground station: with no local control and no connection it cannot be
    controlled, and throttle set over kRPC must not be applied.
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

    def test_throttle_blocked(self):
        comms = self.rt.comms(self.vessel)
        self.assertFalse(comms.has_local_control)
        self.assertFalse(comms.has_connection)
        self.control.throttle = 1
        self.wait(1.5)
        self.assertAlmostEqual(0, self.engine.throttle, places=2)


if __name__ == "__main__":
    unittest.main()
