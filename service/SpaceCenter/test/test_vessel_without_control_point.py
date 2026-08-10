import unittest

import krpctest


class TestVesselWithoutControlPoint(krpctest.TestCase):
    """A vessel separated from another one gets a control point only if one of its parts
    is a command module with crew aboard. The lower stage of PartsDecoupler has neither,
    so the vessel the decoupler leaves behind has no control point at all: KSP orients it
    by its root part, and gives it no presence in the communication network."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsDecoupler")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 250000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        decoupler = cls.vessel.parts.with_name("Decoupler.1")[0].decoupler
        cls.debris = decoupler.decouple()
        cls.wait(1)

    def test_controlling_is_none(self):
        self.assertIsNone(self.debris.parts.controlling)
        # The vessel that kept the crewed pod still has one.
        self.assertEqual(self.vessel.parts.root, self.vessel.parts.controlling)

    def test_oriented_by_root_part(self):
        ref = self.debris.orbit.body.non_rotating_reference_frame
        self.assertAlmostEqual(
            self.debris.parts.root.direction(ref), self.debris.direction(ref), places=3
        )

    def test_setting_a_control_point(self):
        root = self.debris.parts.root
        self.debris.parts.controlling = root
        self.assertEqual(root, self.debris.parts.controlling)

    def test_setting_a_control_point_to_none_fails(self):
        self.assertRaises(ValueError, setattr, self.debris.parts, "controlling", None)

    def test_control_state(self):
        self.assertEqual(self.space_center.ControlState.none, self.debris.control.state)
        self.assertEqual(
            self.space_center.ControlSource.none, self.debris.control.source
        )

    def test_comms(self):
        comms = self.debris.comms
        self.assertFalse(comms.can_communicate)
        self.assertFalse(comms.can_transmit_science)
        self.assertEqual(0, comms.signal_strength)
        self.assertEqual(0, comms.signal_delay)
        self.assertEqual(0, comms.power)
        self.assertEqual([], comms.control_path)


if __name__ == "__main__":
    unittest.main()
