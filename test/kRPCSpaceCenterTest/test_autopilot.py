import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import math
from mathtools import vector, rad2deg, normalize

class TestAutoPilot(testingtools.TestCase):

    def setUp(self):
        load_save('autopilot')
        self.conn = krpc.connect()
        self.vessel = self.conn.space_center.active_vessel
        self.ref = self.conn.space_center.ReferenceFrame
        self.ap = self.vessel.auto_pilot
        self.ap.sas = False
        self.sas_mode = self.conn.space_center.SASMode

    def tearDown(self):
        self.conn.close()

    def test_equality(self):
        self.assertEqual(self.vessel.auto_pilot, self.ap)

    def wait_for_autopilot(self):
        while self.ap.error > 0.25 or self.ap.roll_error > 0.25:
            time.sleep(0.25)

    def set_rotation(self, pitch, heading, roll):
        self.ap.set_rotation(pitch, heading, roll)
        self.wait_for_autopilot()
        self.ap.disengage()

    def check_rotation(self, pitch, heading, roll):
        phr = (pitch,heading,roll)
        flight = self.vessel.flight()
        actual_phr = (flight.pitch, flight.heading, flight.roll)
        self.assertClose(phr, actual_phr, error=1)

    def set_direction(self, direction, roll=float('nan')):
        self.ap.set_direction(direction, roll=roll)
        self.wait_for_autopilot()
        self.ap.disengage()

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
        for pitch in range(-80, 80, 20):
            self.set_rotation(pitch,90,0)
            self.check_rotation(pitch,90,0)

    def test_set_heading(self):
        for heading in range(20, 340, 40):
            self.set_rotation(0,heading,0)
            self.check_rotation(0,heading,0)

    def test_set_roll(self):
        for roll in range(-160, 160, 20):
            self.set_rotation(0,90,roll)
            self.check_rotation(0,90,roll)

    def test_set_rotation(self):
        cases = [
            (50,90,90),
            (-50,90,90),
            (0,20,90),
            (0,300,90),
            (0,90,160),
            (0,90,-160)
        ]

        for phr in cases:
            pitch,heading,roll = phr
            self.set_rotation(pitch,heading,roll)
            self.check_rotation(pitch,heading,roll)

    def test_set_direction(self):
        cases = [
            (1,0,0),
            (0,1,0),
            (0,0,1),
            (-1,0,0),
            (0,-1,0),
            (0,0,-1),
            (1,2,3)
        ]

        for direction in cases:
            direction = normalize(direction)
            self.set_direction(direction)
            self.check_direction(direction)

    def test_set_direction_and_roll(self):
        cases = [
            ((1,1,0), 23),
            ((0,1,1), -75),
            ((1,0,1), 14),
            ((-1,0,1), -83),
            ((0,-1,1), -11),
            ((1,0,-1), 2),
            ((1,2,3), 42)
        ]

        for direction,roll in cases:
            direction = normalize(direction)
            self.set_direction(direction,roll)
            self.check_direction(direction,roll)

    def test_orbital_directions(self):
        flight = self.vessel.flight()
        self.set_direction(flight.prograde)
        self.check_direction(flight.prograde)
        self.set_direction(flight.retrograde)
        self.check_direction(flight.retrograde)
        self.set_direction(flight.normal)
        self.check_direction(flight.normal)
        self.set_direction(flight.anti_normal)
        self.check_direction(flight.anti_normal)
        self.set_direction(flight.radial)
        self.check_direction(flight.radial)
        self.set_direction(flight.anti_radial)
        self.check_direction(flight.anti_radial)

    def test_error(self):
        flight = self.vessel.flight()

        self.ap.disengage()
        self.assertClose(0, self.ap.error)

        self.set_direction(flight.prograde, roll=27)
        self.vessel.control.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        self.ap.set_direction(flight.prograde)
        self.assertClose(0, self.ap.error, 1)

        self.ap.set_direction(flight.retrograde)
        self.assertClose(180, self.ap.error, 1)

        self.ap.set_direction(flight.normal)
        self.assertClose(90, self.ap.error, 1)

        self.ap.set_direction(flight.radial)
        self.assertClose(90, self.ap.error, 1)

        self.ap.set_direction(flight.anti_radial)
        self.assertClose(90, self.ap.error, 1)

    def test_roll_error(self):
        self.ap.disengage()
        self.assertClose(0, self.ap.roll_error)

        set_roll = -57
        direction = self.vessel.direction(self.vessel.surface_reference_frame)
        self.set_direction(direction, roll=set_roll)
        self.vessel.control.sas = True
        for wheel in self.vessel.parts.reaction_wheels:
            wheel.active = False

        for roll in [0,-54,-90,27,45,90]:
            self.ap.set_direction(direction, roll=roll)
            self.assertClose(abs(set_roll - roll), self.ap.roll_error, 1)

    def test_disengage_on_disconnect(self):
        self.ap.set_rotation(90,0)
        self.assertGreater(self.ap.error, 0)
        self.conn.close()
        conn = krpc.connect()
        ap = conn.space_center.active_vessel.auto_pilot
        self.assertTrue(math.isnan(ap.error))

class TestAutoPilotSAS(testingtools.TestCase):

    def setUp(self):
        load_save('autopilot')
        self.conn = krpc.connect()
        self.vessel = self.conn.space_center.active_vessel
        self.ap = self.vessel.auto_pilot
        self.sas_mode = self.conn.space_center.SASMode
        self.speed_mode = self.conn.space_center.SpeedMode

    def wait_for_autopilot(self):
        while self.ap.error > 0.25:
            time.sleep(0.25)

            self.ap.sas = False

    def test_sas_mode(self):
        self.ap.sas = True
        self.ap.sas_mode = self.sas_mode.stability_assist
        self.vessel.control.add_node(self.conn.space_center.ut + 60, 100, 0, 0)
        self.assertEqual(self.ap.sas_mode, self.sas_mode.stability_assist)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.maneuver
        self.assertEqual(self.ap.sas_mode, self.sas_mode.maneuver)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.prograde
        self.assertEqual(self.ap.sas_mode, self.sas_mode.prograde)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.retrograde
        self.assertEqual(self.ap.sas_mode, self.sas_mode.retrograde)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.normal
        self.assertEqual(self.ap.sas_mode, self.sas_mode.normal)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.anti_normal
        self.assertEqual(self.ap.sas_mode, self.sas_mode.anti_normal)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.radial
        self.assertEqual(self.ap.sas_mode, self.sas_mode.radial)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.anti_radial
        self.assertEqual(self.ap.sas_mode, self.sas_mode.anti_radial)
        time.sleep(0.25)
        # No target set, should not change
        # TODO: test with a target set
        self.ap.sas_mode = self.sas_mode.target
        self.assertEqual(self.ap.sas_mode, self.sas_mode.anti_radial)
        time.sleep(0.25)
        self.ap.sas_mode = self.sas_mode.anti_target
        self.assertEqual(self.ap.sas_mode, self.sas_mode.anti_radial)

    def test_speed_mode(self):
        self.ap.speed_mode = self.speed_mode.orbit
        self.assertEqual(self.ap.speed_mode, self.speed_mode.orbit)
        time.sleep(0.25)
        self.ap.speed_mode = self.speed_mode.surface
        self.assertEqual(self.ap.speed_mode, self.speed_mode.surface)
        time.sleep(0.25)
        # No target set, should not change
        # TODO: test with a target set
        self.ap.speed_mode = self.speed_mode.target
        self.assertEqual(self.ap.speed_mode, self.speed_mode.surface)
        time.sleep(0.25)
        self.ap.speed_mode = self.speed_mode.orbit
        self.assertEqual(self.ap.speed_mode, self.speed_mode.orbit)

if __name__ == "__main__":
    unittest.main()
