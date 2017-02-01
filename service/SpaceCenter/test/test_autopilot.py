import unittest
import krpctest
from krpctest.geometry import normalize
import krpc


class TestAutoPilot(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab('Basic')
        cls.set_orbit('Eve', 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.ap.sas = False
        cls.sas_mode = cls.connect().space_center.SASMode

    def setUp(self):
        self.connect().testing_tools.clear_rotation()

    def test_equality(self):
        self.assertEqual(self.ap, self.vessel.auto_pilot)

    def wait_for_autopilot(self):
        self.ap.engage()
        self.ap.wait()
        self.ap.disengage()

    def set_rotation(self, pitch, heading, roll=float('nan')):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_pitch = pitch
        self.ap.target_heading = heading
        self.ap.target_roll = roll

    def check_rotation(self, pitch, heading, roll=None):
        flight = self.vessel.flight()
        ph = (pitch, heading)
        actual_ph = (flight.pitch, flight.heading)
        self.assertAlmostEqual(ph, actual_ph, delta=1)
        if roll:
            self.assertAlmostEqual(roll, flight.roll, delta=1)

    def set_direction(self, direction, roll=float('nan')):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def check_direction(self, direction, roll=None):
        flight = self.vessel.flight()
        self.assertAlmostEqual(direction, flight.direction, delta=0.1)
        if roll is not None:
            self.assertAlmostEqual(roll, flight.roll, delta=1)

    def test_sas(self):
        self.ap.sas = True
        self.assertTrue(self.ap.sas)
        self.ap.sas = False
        self.assertFalse(self.ap.sas)

    def test_set_pitch(self):
        for pitch in (-60, -30, 0, 30, 60):
            self.set_rotation(pitch, 90)
            self.wait_for_autopilot()
            self.check_rotation(pitch, 90)

    def test_set_heading(self):
        for heading in (20, 80, 147, 340):
            self.set_rotation(0, heading)
            self.wait_for_autopilot()
            self.check_rotation(0, heading)

    def test_set_roll(self):
        for roll in (-170, -50, 0, 50, 170):
            self.set_rotation(0, 90, roll)
            self.wait_for_autopilot()
            self.check_rotation(0, 90, roll)

    def test_set_rotation(self):
        cases = [
            (50, 90, 90),
            (-50, 90, 90),
            (0, 20, 90),
            (0, 300, 90),
            (0, 90, 160),
            (0, 90, -160)
        ]

        for phr in cases:
            pitch, heading, roll = phr
            self.set_rotation(pitch, heading, roll)
            self.wait_for_autopilot()
            self.check_rotation(pitch, heading, roll)

    def test_set_direction(self):
        cases = [
            (1, 0, 0),
            (0, 1, 0),
            (0, 0, 1),
            (-1, 0, 0),
            (0, -1, 0),
            (0, 0, -1),
            (1, 2, 3)
        ]

        for direction in cases:
            direction = normalize(direction)
            self.set_direction(direction)
            self.wait_for_autopilot()
            self.check_direction(direction)

    def test_set_direction_and_roll(self):
        cases = [
            ((1, 1, 0), 23),
            ((0, 1, 1), -75),
            ((1, 0, 1), 14),
            ((-1, 0, 1), -83),
            ((0, -1, 1), -11),
            ((1, 0, -1), 2),
            ((1, 2, 3), 42)
        ]

        for direction, roll in cases:
            direction = normalize(direction)
            self.set_direction(direction, roll)
            self.wait_for_autopilot()
            self.check_direction(direction, roll)

    def test_orbital_directions(self):
        flight = self.vessel.flight()
        directions = [
            flight.prograde,
            flight.retrograde,
            flight.normal,
            flight.anti_normal,
            flight.radial,
            flight.anti_radial
        ]
        for direction in directions:
            self.set_direction(direction)
            self.wait_for_autopilot()
            self.check_direction(direction)

    def test_error(self):
        flight = self.vessel.flight()

        self.ap.disengage()
        self.assertDegreesAlmostEqual(0, self.ap.error)

        self.set_direction(flight.prograde, roll=0)
        self.wait_for_autopilot()
        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.target_roll = float('nan')
        self.ap.engage()

        self.ap.target_direction = flight.prograde
        self.assertDegreesAlmostEqual(0, self.ap.error, delta=1)

        self.ap.target_direction = flight.retrograde
        self.assertDegreesAlmostEqual(180, self.ap.error, delta=1)

        self.ap.target_direction = flight.normal
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=1)

        self.ap.target_direction = flight.anti_normal
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=1)

        self.ap.target_direction = flight.radial
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=1)

        self.ap.target_direction = flight.anti_radial
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=1)

        self.ap.target_direction = flight.anti_radial
        self.assertDegreesAlmostEqual(90, self.ap.error, delta=1)

        self.ap.target_direction = flight.prograde
        self.ap.target_roll = 30.0
        self.assertDegreesAlmostEqual(30, self.ap.error, delta=1)

        self.ap.disengage()

        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_roll_error(self):
        self.ap.disengage()
        self.assertAlmostEqual(0, self.ap.roll_error)

        set_roll = -57
        direction = self.vessel.direction(self.vessel.surface_reference_frame)
        self.set_direction(direction, roll=set_roll)
        self.wait_for_autopilot()
        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.engage()
        for roll in (0, -54, -90, 27, 45, 90):
            self.ap.target_roll = roll
            self.assertAlmostEqual(
                abs(set_roll - roll), self.ap.roll_error, delta=1)
        self.ap.disengage()

        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_invalid_reference_frame(self):
        frames = [
            self.vessel.reference_frame,
            self.vessel.parts.all[0].reference_frame,
            self.vessel.parts.all[0].center_of_mass_reference_frame,
            self.vessel.parts.docking_ports[0].reference_frame,
            self.vessel.parts.engines[0].thrusters[0].thrust_reference_frame
        ]
        for frame in frames:
            self.assertRaises(krpc.client.RPCError, setattr,
                              self.ap, 'reference_frame', frame)

    def test_reset_on_disconnect(self):
        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        ap.reference_frame = vessel.orbital_reference_frame
        ap.target_pitch_and_heading(10, 20)
        ap.target_roll = 30
        ap.engage()
        conn.close()

        self.wait()

        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        self.assertEqual(vessel.surface_reference_frame, ap.reference_frame)
        # FIXME: tuples returned from server cannot be null
        # self.assertEqual(None, ap.target_direction)
        self.assertIsNaN(ap.target_roll)
        conn.close()

    def test_dont_reset_on_clean_disconnect(self):
        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        ap.reference_frame = vessel.orbital_reference_frame
        ap.target_pitch_and_heading(10, 20)
        ap.target_roll = 30
        ap.engage()
        self.wait()
        ap.disengage()
        conn.close()

        self.wait()

        conn = self.connect(use_cached=False)
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        self.assertEqual(vessel.orbital_reference_frame, ap.reference_frame)
        self.assertIsNotNone(ap.target_direction)
        self.assertEqual(30, ap.target_roll)
        conn.close()


class TestAutoPilotSAS(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab('Basic')
        cls.set_orbit('Eve', 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.sas_mode = cls.connect().space_center.SASMode
        cls.speed_mode = cls.connect().space_center.SpeedMode

    def setUp(self):
        self.connect().testing_tools.clear_rotation()

    def wait_for_autopilot(self):
        self.ap.engage()
        self.ap.wait()
        self.ap.disengage()

    def set_direction(self, direction, roll=float('nan')):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def check_direction(self, direction, roll=None):
        flight = self.vessel.flight()
        self.assertAlmostEqual(direction, flight.direction, delta=0.1)
        if roll is not None:
            self.assertAlmostEqual(roll, flight.roll, delta=1)

    def test_sas_error(self):
        flight = self.vessel.flight()
        self.set_direction(flight.prograde, roll=27)
        self.wait_for_autopilot()
        self.check_direction(flight.prograde, roll=27)

        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.speed_mode = self.speed_mode.orbit

        self.ap.sas_mode = self.sas_mode.prograde
        self.assertAlmostEqual(0, self.ap.error, delta=1)

        self.ap.sas_mode = self.sas_mode.retrograde
        self.assertAlmostEqual(180, self.ap.error, delta=1)

        self.ap.sas_mode = self.sas_mode.normal
        self.assertAlmostEqual(90, self.ap.error, delta=1)

        self.ap.sas_mode = self.sas_mode.anti_normal
        self.assertAlmostEqual(90, self.ap.error, delta=1)

        self.ap.sas_mode = self.sas_mode.radial
        self.assertAlmostEqual(90, self.ap.error, delta=1)

        self.ap.sas_mode = self.sas_mode.anti_radial
        self.assertAlmostEqual(90, self.ap.error, delta=1)

        self.ap.sas = False
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True


class TestAutoPilotOtherVessel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Multi')
        cls.remove_other_vessels()
        cls.set_orbit('Eve', 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        space_center = cls.connect().space_center
        next(iter(space_center.active_vessel.parts.docking_ports)).undock()
        cls.vessel = space_center.active_vessel
        cls.other_vessel = next(
            v for v in space_center.vessels if v != cls.vessel)

    def test_autopilot(self):
        ap = self.other_vessel.auto_pilot
        ap.target_pitch_and_heading(0, 0)
        ap.target_roll = 0
        ap.engage()
        ap.wait()
        ap.disengage()


if __name__ == '__main__':
    unittest.main()
