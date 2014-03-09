#!/usr/bin/env python2

import testingtools
from testingtools import load_save
import krpc
import time
import numpy as np
import math
from mathtools import v3, deg2rad

class TestFlight(testingtools.TestCase):

    def setUp(self):
        load_save('flight')
        self.ksp = krpc.connect()

    def test_flight(self):
        # Check basic flight telemtry
        self.assertEqual("Kerbin", self.ksp.Flight.Body)
        self.assertBetween(99900, 100100, self.ksp.Flight.Altitude)
        self.assertBetween(99900, 100100, self.ksp.Flight.TrueAltitude)
        self.assertBetween(2245, 2247, self.ksp.Flight.OrbitalSpeed)
        self.assertBetween(2041, 2043, self.ksp.Flight.SurfaceSpeed)
        self.assertBetween(-1, 1, self.ksp.Flight.VerticalSurfaceSpeed)
        #v3(self.ksp.Flight.CenterOfMass)

        # Check vessel direction vectors
        direction      = v3(self.ksp.Flight.Direction)
        upDirection    = v3(self.ksp.Flight.UpDirection)
        northDirection = v3(self.ksp.Flight.NorthDirection)
        self.assertClose(1, np.linalg.norm(direction))
        self.assertClose(1, np.linalg.norm(upDirection))
        self.assertClose(1, np.linalg.norm(northDirection))
        self.assertClose(0, np.dot(upDirection, northDirection))

        # Check vessel direction vector agrees with pitch angle
        pitch = self.ksp.Flight.Pitch
        heading = self.ksp.Flight.Heading
        self.assertClose(pitch, 90 - rad2deg(math.acos(np.dot(direction, upDirection))))

        # Check vessel direction vector agrees with heading angle
        upComponent = np.dot(direction, upDirection) * np.array(upDirection)
        northComponent = np.array(direction) - upComponent
        northComponent = northComponent / np.linalg.norm(northComponent)
        self.assertClose(heading, 360 - rad2deg(math.acos(np.dot(northComponent, northDirection))))

        # Check orbital direction vectors
        prograde   = v3(self.ksp.Flight.Prograde)
        retrograde = v3(self.ksp.Flight.Retrograde)
        normal     = v3(self.ksp.Flight.Normal)
        normalNeg  = v3(self.ksp.Flight.NormalNeg)
        radial     = v3(self.ksp.Flight.Radial)
        radialNeg  = v3(self.ksp.Flight.RadialNeg)
        self.assertClose(1, np.linalg.norm(prograde))
        self.assertClose(1, np.linalg.norm(retrograde))
        self.assertClose(1, np.linalg.norm(normal))
        self.assertClose(1, np.linalg.norm(normalNeg))
        self.assertClose(1, np.linalg.norm(radial))
        self.assertClose(1, np.linalg.norm(radialNeg))
        self.assertEqual(prograde, [-x for x in retrograde])
        self.assertEqual(radial, [-x for x in radialNeg])
        self.assertEqual(normal, [-x for x in normalNeg])
        self.assertClose(0, np.dot(prograde, radial))
        self.assertClose(0, np.dot(prograde, normal))
        self.assertClose(0, np.dot(radial, normal))

        # Check vessel directions agree with orbital directions
        # (we are in a 0 degree inclined orbit, so they should do)
        self.assertClose(1, np.dot(upDirection, radial))
        self.assertClose(1, np.dot(northDirection, normal))

    def test_roll_pitch_yaw(self):
        self.assertBetween(57, 58, self.ksp.Flight.Pitch)
        self.assertBetween(224, 227, self.ksp.Flight.Heading)
        self.assertBetween(132, 134, self.ksp.Flight.Roll)

    def test_pitch_control(self):
        self.ksp.Control.SAS = False
        self.ksp.Control.Pitch = 1
        time.sleep(3)
        self.ksp.Control.Pitch = 0

        # Check vessel is pitching in correct direction
        pitch = self.ksp.Flight.Pitch
        time.sleep(0.1)
        diff = pitch - self.ksp.Flight.Pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        self.ksp.Control.SAS = False
        self.ksp.Control.Yaw = 1
        time.sleep(3)
        self.ksp.Control.Yaw = 0

        # Check vessel is yawing in correct direction
        heading = self.ksp.Flight.Heading
        time.sleep(0.1)
        diff = heading - self.ksp.Flight.Heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        self.ksp.Control.SAS = False
        self.ksp.Control.Roll = 0.1
        time.sleep(3)
        self.ksp.Control.Roll = 0

        self.assertBetween(57, 58, self.ksp.Flight.Pitch)
        self.assertBetween(224, 227, self.ksp.Flight.Heading)
        self.assertBetween(80, 110, self.ksp.Flight.Roll)

        # Check vessel is rolling in correct direction
        roll = self.ksp.Flight.Roll
        time.sleep(0.1)
        diff = roll - self.ksp.Flight.Roll
        self.assertGreater(diff, 0)

if __name__ == "__main__":
    unittest.main()
