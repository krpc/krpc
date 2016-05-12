import unittest
import time
import krpctest
from krpctest.geometry import normalize

class TestAutoPilot(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.remove_other_vessels()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.set_orbit('Eve', 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.ap.sas = False
        cls.SASMode = cls.conn.space_center.SASMode
        cls.ap.rotation_speed_multiplier = 2
        cls.ap.roll_speed_multiplier = 2
        cls.ap.set_pid_parameters(3, 0, 0)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def setUp(self):
        self.conn.testing_tools.clear_rotation()

    def test_equality(self):
        self.assertEqual(self.ap, self.vessel.auto_pilot)

    def wait_for_autopilot(self):
        self.ap.engage()
        self.ap.wait()
        self.ap.disengage()

    def set_rotation(self, pitch, heading, roll=float('nan')):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_pitch_and_heading(pitch, heading)
        self.ap.target_roll = roll

    def check_rotation(self, pitch, heading, roll=None):
        flight = self.vessel.flight()
        ph = (pitch, heading)
        actual_ph = (flight.pitch, flight.heading)
        self.assertClose(ph, actual_ph, error=1)
        if roll:
            self.assertClose(roll, flight.roll, error=1)

    def set_direction(self, direction, roll=float('nan')):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def check_direction(self, direction, roll=None):
        flight = self.vessel.flight()
        self.assertClose(direction, flight.direction, error=0.1)
        if roll is not None:
            self.assertClose(roll, flight.roll, error=1)

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
        self.assertClose(0, self.ap.error)

        self.set_direction(flight.prograde, roll=27)
        self.wait_for_autopilot()
        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.engage()

        self.ap.target_direction = flight.prograde
        self.assertClose(0, self.ap.error, 1)

        self.ap.target_direction = flight.retrograde
        self.assertClose(180, self.ap.error, 1)

        self.ap.target_direction = flight.normal
        self.assertClose(90, self.ap.error, 1)

        self.ap.target_direction = flight.anti_normal
        self.assertClose(90, self.ap.error, 1)

        self.ap.target_direction = flight.radial
        self.assertClose(90, self.ap.error, 1)

        self.ap.target_direction = flight.anti_radial
        self.assertClose(90, self.ap.error, 1)

        self.ap.disengage()

        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_roll_error(self):
        self.ap.disengage()
        self.assertClose(0, self.ap.roll_error)

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
            self.assertClose(abs(set_roll - roll), self.ap.roll_error, 1)
        self.ap.disengage()

        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

    def test_reset_on_disconnect(self):
        conn = krpctest.connect(self, 'test_reset_on_disconnect')
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        ap.reference_frame = vessel.orbital_reference_frame
        ap.target_pitch_and_heading(10, 20)
        ap.target_roll = 30
        ap.engage()
        conn.close()

        time.sleep(0.1)

        conn = krpctest.connect(self, 'test_reset_on_disconnect')
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        self.assertEqual(vessel.surface_reference_frame, ap.reference_frame)
        #FIXME: tuples returned from server cannot be null
        #self.assertEqual(None, ap.target_direction)
        self.assertIsNaN(ap.target_roll)
        conn.close()

    def test_dont_reset_on_clean_disconnect(self):
        conn = krpctest.connect(self, 'test_dont_reset_on_clean_disconnect')
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        ap.reference_frame = vessel.orbital_reference_frame
        ap.target_pitch_and_heading(10, 20)
        ap.target_roll = 30
        ap.engage()
        time.sleep(0.1)
        ap.disengage()
        conn.close()

        time.sleep(0.1)

        conn = krpctest.connect(self, 'test_dont_reset_on_clean_disconnect')
        vessel = conn.space_center.active_vessel
        ap = vessel.auto_pilot
        self.assertEqual(vessel.orbital_reference_frame, ap.reference_frame)
        self.assertNotNone(ap.target_direction)
        self.assertEqual(30, ap.target_roll)
        conn.close()

class TestAutoPilotSAS(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.remove_other_vessels()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.set_orbit('Eve', 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.conn = krpctest.connect(cls)
        cls.vessel = cls.conn.space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.sas_mode = cls.conn.space_center.SASMode
        cls.speed_mode = cls.conn.space_center.SpeedMode
        cls.ap.rotation_speed_multiplier = 2
        cls.ap.roll_speed_multiplier = 2
        cls.ap.set_pid_parameters(3, 0, 0)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def setUp(self):
        self.conn.testing_tools.clear_rotation()

    def wait_for_autopilot(self):
        self.ap.engage()
        self.ap.wait()
        self.ap.disengage()

    def set_direction(self, direction, roll=float('nan')):
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_direction = direction
        self.ap.target_roll = roll

    def test_sas_error(self):
        flight = self.vessel.flight()
        self.set_direction(flight.prograde, roll=27)
        self.wait_for_autopilot()

        self.ap.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.speed_mode = self.speed_mode.orbit

        self.ap.sas_mode = self.SASMode.prograde
        self.assertClose(0, self.ap.error, 1)

        self.ap.sas_mode = self.SASMode.retrograde
        self.assertClose(180, self.ap.error, 1)

        self.ap.sas_mode = self.SASMode.normal
        self.assertClose(90, self.ap.error, 1)

        self.ap.sas_mode = self.SASMode.anti_normal
        self.assertClose(90, self.ap.error, 1)

        self.ap.sas_mode = self.SASMode.radial
        self.assertClose(90, self.ap.error, 1)

        self.ap.sas_mode = self.SASMode.anti_radial
        self.assertClose(90, self.ap.error, 1)

        self.ap.sas = False
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = True

class TestAutoPilotOtherVessel(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.remove_other_vessels()
        krpctest.launch_vessel_from_vab('Multi')
        krpctest.set_orbit('Eve', 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.conn = krpctest.connect(cls)
        next(iter(cls.conn.space_center.active_vessel.parts.docking_ports)).undock()
        cls.vessel = cls.conn.space_center.active_vessel
        cls.other_vessel = next(v for v in cls.conn.space_center.vessels if v != cls.vessel)

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_autopilot(self):
        ap = self.other_vessel.auto_pilot
        ap.target_pitch_and_heading(90, 0)
        ap.engage()
        ap.wait()
        ap.disengage()

if __name__ == '__main__':
    unittest.main()
