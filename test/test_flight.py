#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc
import time
import numpy as np
import math
from mathtools import v3, rad2deg

class TestFlight(testingtools.TestCase):

    def setUp(self):
        load_save('flight')
        self.ksp = krpc.connect()
        self.vessel = self.ksp.SpaceCenter.ActiveVessel
        self.flight = self.vessel.Flight
        self.control = self.vessel.Control

    def test_flight(self):

        # Check basic flight telemetry
        self.assertBetween(99900, 100100, self.flight.Altitude)
        self.assertBetween(99900, 100100, self.flight.TrueAltitude)
        #self.assertBetween(2245, 2247, self.flight.OrbitalSpeed)
        #self.assertBetween(2041, 2043, self.flight.SurfaceSpeed)
        #self.assertBetween(-1, 1, self.flight.VerticalSurfaceSpeed)
        #v3(self.flight.CenterOfMass)

        # Check vessel direction vectors
        direction      = v3(self.flight.Direction)
        upDirection    = v3(self.flight.UpDirection)
        northDirection = v3(self.flight.NorthDirection)
        self.assertClose(1, np.linalg.norm(direction))
        self.assertClose(1, np.linalg.norm(upDirection))
        self.assertClose(1, np.linalg.norm(northDirection))
        self.assertClose(0, np.dot(upDirection, northDirection))

        # Check vessel direction vector agrees with pitch angle
        pitch = self.flight.Pitch
        heading = self.flight.Heading
        self.assertClose(pitch, 90 - rad2deg(math.acos(np.dot(direction, upDirection))))

        # Check vessel direction vector agrees with heading angle
        upComponent = np.dot(direction, upDirection) * np.array(upDirection)
        northComponent = np.array(direction) - upComponent
        northComponent = northComponent / np.linalg.norm(northComponent)
        self.assertClose(heading, 360 - rad2deg(math.acos(np.dot(northComponent, northDirection))))

        # Check orbital direction vectors
        prograde   = v3(self.flight.Prograde)
        retrograde = v3(self.flight.Retrograde)
        normal     = v3(self.flight.Normal)
        normalNeg  = v3(self.flight.NormalNeg)
        radial     = v3(self.flight.Radial)
        radialNeg  = v3(self.flight.RadialNeg)
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
        vessel = self.ksp.SpaceCenter.ActiveVessel
        self.assertBetween(57, 58, self.flight.Pitch)
        self.assertBetween(224, 227, self.flight.Heading)
        self.assertBetween(132, 134, self.flight.Roll)

    def test_pitch_control(self):
        vessel = self.ksp.SpaceCenter.ActiveVessel

        self.control.SAS = False
        self.control.Pitch = 1
        time.sleep(3)
        self.control.Pitch = 0

        # Check flight is pitching in correct direction
        pitch = self.flight.Pitch
        time.sleep(0.1)
        diff = pitch - self.flight.Pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        vessel = self.ksp.SpaceCenter.ActiveVessel

        self.control.SAS = False
        self.control.Yaw = 1
        time.sleep(3)
        self.control.Yaw = 0

        # Check flight is yawing in correct direction
        heading = self.flight.Heading
        time.sleep(0.1)
        diff = heading - self.flight.Heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        vessel = self.ksp.SpaceCenter.ActiveVessel

        self.control.SAS = False
        self.control.Roll = 0.1
        time.sleep(3)
        self.control.Roll = 0

        self.assertBetween(57, 58, self.flight.Pitch)
        self.assertBetween(224, 227, self.flight.Heading)
        self.assertBetween(80, 110, self.flight.Roll)

        # Check flight is rolling in correct direction
        roll = self.flight.Roll
        time.sleep(0.1)
        diff = roll - self.flight.Roll
        self.assertGreater(diff, 0)

if __name__ == "__main__":
    unittest.main()
