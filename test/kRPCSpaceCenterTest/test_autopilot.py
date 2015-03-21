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
        self.vessel.control.sas = False

    def tearDown(self):
        del self.conn

    def test_equality(self):
        self.assertEqual(self.vessel.auto_pilot, self.ap)

    def wait_for_autopilot(self):
        while self.ap.error > 0.25:
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
        self.assertTrue(math.isnan(self.ap.error))
        self.set_direction(flight.prograde)
        self.ap.set_direction(flight.retrograde)
        self.assertClose(180, self.ap.error, 1)
        self.ap.set_direction(flight.normal)
        self.assertClose(90, self.ap.error, 1)
        self.ap.set_direction(flight.anti_normal)
        self.assertClose(90, self.ap.error, 1)

if __name__ == "__main__":
    unittest.main()
