#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
from mathtools import v3 as vec3
from mathtools import norm, normalize, to_vector
import krpc

class TestNode(testingtools.TestCase):

    def check(self, node, v):
        self.assertEqual(v[0], node.prograde)
        self.assertEqual(v[1], node.normal)
        self.assertEqual(v[2], node.radial)
        self.assertEqual(v, vec3(node.vector))
        self.assertEqual(norm(v), node.delta_v)
        self.assertEqual(normalize(v), vec3(node.direction))

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
        self.check(node, v0)

        # Test setters
        v2 = [-50,500,-150]
        ut2 = ut + 500
        node.ut = ut2
        node.prograde = v2[0]
        node.normal = v2[1]
        node.radial = v2[2]
        self.assertClose(ut2, node.ut, error=1)
        self.assertClose(ut2 - start_ut, node.time_to, error=1)
        self.check(node, v2)

        # Test set magnitude
        magnitude = 128
        v3 = [x*magnitude for x in vec3(node.direction)]
        node.delta_v = magnitude
        self.check(node, v3)

        # Test set direction
        magnitude = node.delta_v
        direction = normalize([2,1,-0.5])
        v4 = [x*magnitude for x in direction]
        node.direction = to_vector(direction)
        self.check(node, v4)

        # Remove node
        node.remove()
        with self.assertRaises (krpc.client.RPCError):
            node.prograde = 0

        # Remove nodes
        node = control.add_node(ut, *v0)
        control.remove_nodes()
        # TODO: don't skip the following
        #with self.assertRaises (krpc.client.RPCError):
        #    node.prograde = 0

if __name__ == "__main__":
    unittest.main()
