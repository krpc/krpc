#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
from mathtools import v3 as vec3
from mathtools import length, normalize, to_vector
import krpc

class TestNode(testingtools.TestCase):

    def test_basic(self):
        load_save('orbit-kerbin')
        ksp = krpc.connect()
        vessel = ksp.space_center.active_vessel
        control = vessel.control

        # Test creation
        start_ut = ksp.space_center.ut
        ut = start_ut + 60
        v0 = [100,200,-350]
        node = control.add_node(ut, *v0)
        self.assertClose(ut, node.ut, error=1)
        self.assertClose(ut - start_ut, node.time_to, error=1)
        self.assertEqual(v0[0], node.prograde)
        self.assertEqual(v0[1], node.normal)
        self.assertEqual(v0[2], node.radial)
        self.assertEqual(v0, vec3(node.vector))
        self.assertEqual(length(v0), node.delta_v)
        self.assertEqual(normalize(v0), vec3(node.direction))

        # Test setters
        v2 = [-50,500,-150]
        ut2 = ut + 500
        node.ut = ut2
        node.prograde = v2[0]
        node.normal = v2[1]
        node.radial = v2[2]
        self.assertClose(ut2, node.ut, error=1)
        self.assertClose(ut2 - start_ut, node.time_to, error=1)
        self.assertEqual(v2[0], node.prograde)
        self.assertEqual(v2[1], node.normal)
        self.assertEqual(v2[2], node.radial)
        self.assertEqual(v2, vec3(node.vector))
        self.assertEqual(length(v2), node.delta_v)
        self.assertEqual(normalize(v2), vec3(node.direction))

        # Test set magnitude
        magnitude = 128
        v3 = [x*magnitude for x in vec3(node.direction)]
        node.delta_v = magnitude
        self.assertEqual(v3[0], node.prograde)
        self.assertEqual(v3[1], node.normal)
        self.assertEqual(v3[2], node.radial)
        self.assertEqual(v3, vec3(node.vector))
        self.assertEqual(length(v3), node.delta_v)
        self.assertEqual(normalize(v3), vec3(node.direction))

        # Test set direction
        magnitude = node.delta_v
        direction = normalize([2,1,-0.5])
        v4 = [x*magnitude for x in direction]
        node.direction = to_vector(direction)
        self.assertEqual(v4[0], node.prograde)
        self.assertEqual(v4[1], node.normal)
        self.assertEqual(v4[2], node.radial)
        self.assertEqual(v4, vec3(node.vector))
        self.assertEqual(length(v4), node.delta_v)
        self.assertEqual(normalize(v4), vec3(node.direction))

        # Remove node
        node.remove()

if __name__ == "__main__":
    unittest.main()
