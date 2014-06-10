import unittest
import testingtools
from testingtools import load_save
from mathtools import vector, norm, normalize, to_vector
import krpc

class TestNode(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        load_save('orbit-kerbin')
        cls.conn = krpc.connect()
        cls.vessel = cls.conn.space_center.active_vessel
        cls.control = cls.vessel.control

    def check(self, node, v):
        self.assertEqual(v[0], node.prograde)
        self.assertEqual(v[1], node.normal)
        self.assertEqual(v[2], node.radial)
        self.assertEqual([0,0,norm(v)], vector(node.vector))
        self.assertEqual(norm(v), node.delta_v)

    def test_add_node(self):
        start_ut = self.conn.space_center.ut
        ut = start_ut + 60
        v0 = [100,200,-350]
        node = self.control.add_node(ut, *v0)
        self.assertClose(ut, node.ut, error=1)
        self.assertClose(ut - start_ut, node.time_to, error=1)
        self.check(node, v0)
        node.remove()

    def test_remove_node(self):
        node = self.control.add_node(self.conn.space_center.ut, 0, 0, 0)
        node.remove()
        with self.assertRaises (krpc.client.RPCError):
            node.prograde = 0

    def test_remove_nodes(self):
        node0 = self.control.add_node(self.conn.space_center.ut+15, 4, -2, 1)
        node1 = self.control.add_node(self.conn.space_center.ut+40, 1, 3, 2)
        node2 = self.control.add_node(self.conn.space_center.ut+60, 0, 4, 0)
        self.control.remove_nodes()
        # TODO: don't skip the following
        #with self.assertRaises (krpc.client.RPCError):
        #    node.prograde = 0

    def test_get_nodes(self):
        self.assertEquals([], self.control.nodes)
        node0 = self.control.add_node(self.conn.space_center.ut+35, 4, -2, 1)
        self.assertEquals([node0], self.control.nodes)
        node1 = self.control.add_node(self.conn.space_center.ut+15, 1, 3, 2)
        self.assertEquals([node1, node0], self.control.nodes)
        node2 = self.control.add_node(self.conn.space_center.ut+60, 0, 4, 0)
        self.assertEquals([node1, node0, node2], self.control.nodes)
        self.control.remove_nodes()
        self.assertEquals([], self.control.nodes)

    def test_setters(self):
        start_ut = self.conn.space_center.ut
        ut = start_ut + 60
        node = self.control.add_node(ut, 0, 0, 0)
        v = [-50,500,-150]
        ut2 = ut + 500
        node.ut = ut2
        node.prograde = v[0]
        node.normal = v[1]
        node.radial = v[2]
        self.assertClose(ut2, node.ut, error=1)
        self.assertClose(ut2 - start_ut, node.time_to, error=1)
        self.check(node, v)
        node.remove()

    def test_set_magnitude(self):
        node = self.control.add_node(self.conn.space_center.ut, 1, -2, 3)
        magnitude = 128
        node.delta_v = magnitude
        v = normalize([1,-2,3]) * magnitude
        print magnitude * normalize([1,-2,3])
        self.check(node, v)
        node.remove()

    def test_orbit(self):
        start_ut = self.conn.space_center.ut
        ut = start_ut + 60
        v = [100,0,0]
        node = self.control.add_node(ut, *v)
        self.check(node, v)

        orbit0 = self.vessel.orbit
        orbit1 = node.orbit

        # Check semi-major axis using vis-viva equation
        GM = self.conn.space_center.bodies['Kerbin'].gravitational_parameter
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
