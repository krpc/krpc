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

    def test_flight(self):
        vessel = self.ksp.Flight.ActiveVessel

        # Check basic flight telemetry
        self.assertEqual("Kerbin", vessel.Body)
        self.assertBetween(99900, 100100, vessel.Altitude)
        self.assertBetween(99900, 100100, vessel.TrueAltitude)
        self.assertBetween(2245, 2247, vessel.OrbitalSpeed)
        self.assertBetween(2041, 2043, vessel.SurfaceSpeed)
        self.assertBetween(-1, 1, vessel.VerticalSurfaceSpeed)
        #v3(vessel.CenterOfMass)

        # Check vessel direction vectors
        direction      = v3(vessel.Direction)
        upDirection    = v3(vessel.UpDirection)
        northDirection = v3(vessel.NorthDirection)
        self.assertClose(1, np.linalg.norm(direction))
        self.assertClose(1, np.linalg.norm(upDirection))
        self.assertClose(1, np.linalg.norm(northDirection))
        self.assertClose(0, np.dot(upDirection, northDirection))

        # Check vessel direction vector agrees with pitch angle
        pitch = vessel.Pitch
        heading = vessel.Heading
        self.assertClose(pitch, 90 - rad2deg(math.acos(np.dot(direction, upDirection))))

        # Check vessel direction vector agrees with heading angle
        upComponent = np.dot(direction, upDirection) * np.array(upDirection)
        northComponent = np.array(direction) - upComponent
        northComponent = northComponent / np.linalg.norm(northComponent)
        self.assertClose(heading, 360 - rad2deg(math.acos(np.dot(northComponent, northDirection))))

        # Check orbital direction vectors
        prograde   = v3(vessel.Prograde)
        retrograde = v3(vessel.Retrograde)
        normal     = v3(vessel.Normal)
        normalNeg  = v3(vessel.NormalNeg)
        radial     = v3(vessel.Radial)
        radialNeg  = v3(vessel.RadialNeg)
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
        vessel = self.ksp.Flight.ActiveVessel
        self.assertBetween(57, 58, vessel.Pitch)
        self.assertBetween(224, 227, vessel.Heading)
        self.assertBetween(132, 134, vessel.Roll)

    def test_pitch_control(self):
        vessel = self.ksp.Flight.ActiveVessel

        self.ksp.Control.SAS = False
        self.ksp.Control.Pitch = 1
        time.sleep(3)
        self.ksp.Control.Pitch = 0

        # Check vessel is pitching in correct direction
        pitch = vessel.Pitch
        time.sleep(0.1)
        diff = pitch - vessel.Pitch
        self.assertGreater(diff, 0)

    def test_yaw_control(self):
        vessel = self.ksp.Flight.ActiveVessel

        self.ksp.Control.SAS = False
        self.ksp.Control.Yaw = 1
        time.sleep(3)
        self.ksp.Control.Yaw = 0

        # Check vessel is yawing in correct direction
        heading = vessel.Heading
        time.sleep(0.1)
        diff = heading - vessel.Heading
        self.assertGreater(diff, 0)

    def test_roll_control(self):
        vessel = self.ksp.Flight.ActiveVessel

        self.ksp.Control.SAS = False
        self.ksp.Control.Roll = 0.1
        time.sleep(3)
        self.ksp.Control.Roll = 0

        self.assertBetween(57, 58, vessel.Pitch)
        self.assertBetween(224, 227, vessel.Heading)
        self.assertBetween(80, 110, vessel.Roll)

        # Check vessel is rolling in correct direction
        roll = vessel.Roll
        time.sleep(0.1)
        diff = roll - vessel.Roll
        self.assertGreater(diff, 0)

if __name__ == "__main__":
    unittest.main()
