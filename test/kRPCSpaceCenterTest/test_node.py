import unittest
import testingtools
from testingtools import load_save
from mathtools import vector, norm, normalize, to_vector
import krpc

class TestNode(testingtools.TestCase):

    def check(self, node, v):
        self.assertEqual(v[0], node.prograde)
        self.assertEqual(v[1], node.normal)
        self.assertEqual(v[2], node.radial)
        self.assertEqual(v, vector(node.vector))
        self.assertEqual(norm(v), node.delta_v)
        self.assertEqual(normalize(v), vector(node.direction))

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
        v3 = [x*magnitude for x in vector(node.direction)]
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

    def test_orbit(self):
        load_save('orbit-kerbin')
        ksp = krpc.connect()
        vessel = ksp.space_center.active_vessel
        control = vessel.control

        start_ut = ksp.space_center.ut
        ut = start_ut + 60
        v = [100,0,0]
        node = control.add_node(ut, *v)
        self.check(node, v)

        orbit0 = vessel.orbit
        orbit1 = node.orbit

        # Check semi-major axis using vis-viva equation
        GM = ksp.space_center.bodies['Kerbin'].gravitational_parameter
        vsq = (orbit0.speed + v[0])**2
        r = orbit0.radius
        self.assertClose (GM / ((2*GM/r) - vsq), orbit1.semi_major_axis, error=0.1)

        # Check there is no inclination change
        self.assertClose(orbit0.inclination, orbit1.inclination)

        # Check the eccentricity
        rp = orbit1.periapsis
        ra = orbit1.apoapsis
        e = (ra - rp) / (ra + rp)
        self.assertGreater(orbit1.eccentricity, orbit0.eccentricity)
        self.assertClose(e, orbit1.eccentricity)

if __name__ == "__main__":
    unittest.main()
