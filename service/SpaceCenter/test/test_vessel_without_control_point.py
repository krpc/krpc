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


class TestVesselWithCrewlessCommandPod(krpctest.TestCase):
    """CrewlessCommandPod is a command pod, a stack separator and a second command pod
    mounted upside down. Launched with a single crew member, who is seated in the first
    pod, separating leaves a vessel whose only command module has no crew aboard. That
    vessel is in the communication network, unlike one with no command module at all, but
    it has no control point and no control."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center
        if cls.space_center.get_kerbal("Solo Kerman") is None:
            cls.space_center.create_kerbal("Solo Kerman", "Pilot", True)
        cls._stage_craft("CrewlessCommandPod", "VAB", None)
        cls.space_center.launch_vessel(
            "VAB", "CrewlessCommandPod", "LaunchPad", ["Solo Kerman"]
        )
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 250000)
        cls.vessel = cls.space_center.active_vessel
        # The TS-12 is an omni-decoupler, detaching at both ends, so staging it leaves the
        # separator on its own as well as the stack below it. The stack below is the one
        # with the crewless pod on it.
        new_vessels = cls.vessel.control.activate_next_stage()
        cls.wait(1)
        cls.lower = next(v for v in new_vessels if v.crew_capacity > 0)

    def test_crew_is_in_the_upper_vessel(self):
        self.assertEqual(1, self.vessel.crew_count)
        self.assertEqual(0, self.lower.crew_count)
        self.assertEqual(3, self.lower.crew_capacity)

    def test_no_control_point(self):
        self.assertIsNone(self.lower.parts.controlling)
        self.assertEqual("trussPiece1x", self.lower.parts.root.name)

    def test_control_state(self):
        # The empty pod makes this vessel part of the network, so the control state comes
        # from CommNet rather than from the absence of a connection.
        self.assertEqual(self.space_center.ControlState.none, self.lower.control.state)
        self.assertEqual(
            self.space_center.ControlSource.none, self.lower.control.source
        )
        # The vessel that kept the crew is unaffected.
        self.assertEqual(self.space_center.ControlState.full, self.vessel.control.state)
        self.assertEqual(
            self.space_center.ControlSource.kerbal, self.vessel.control.source
        )


if __name__ == "__main__":
    unittest.main()
