import unittest
import krpctest
from krpctest.geometry import vector, norm, normalize
import krpc


class TestNode(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit('Kerbin', 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.control = cls.vessel.control
        cls.control.remove_nodes()

    def check(self, node, delta_v):
        self.assertAlmostEqual(delta_v[0], node.prograde, places=3)
        self.assertAlmostEqual(delta_v[1], node.normal, places=3)
        self.assertAlmostEqual(delta_v[2], node.radial, places=3)
        self.assertAlmostEqual(norm(delta_v), node.delta_v, places=3)
        self.assertAlmostEqual(norm(delta_v),
                               node.remaining_delta_v, delta=0.2)

        bv = node.burn_vector(node.reference_frame)
        self.assertAlmostEqual(norm(delta_v), norm(bv), places=3)
        self.assertAlmostEqual((0, 1, 0), normalize(bv), places=3)

        orbital_bv = node.burn_vector(node.orbital_reference_frame)
        self.assertAlmostEqual(norm(delta_v), norm(orbital_bv), places=3)
        self.assertAlmostEqual(
            (-delta_v[2], delta_v[0], delta_v[1]), orbital_bv, places=3)

        direction = node.direction(node.reference_frame)
        self.assertAlmostEqual((0, 1, 0), direction, places=3)
        orbital_direction = node.direction(node.orbital_reference_frame)
        self.assertAlmostEqual(
            normalize((-delta_v[2], delta_v[0], delta_v[1])),
            orbital_direction, places=3)

    def test_add_node(self):
        start_ut = self.space_center.ut
        ut = start_ut + 60
        delta_v = (100, 200, -350)
        node = self.control.add_node(ut, *delta_v)
        self.assertAlmostEqual(ut, node.ut, delta=1)
        self.assertAlmostEqual(ut - start_ut, node.time_to, delta=1)
        self.check(node, delta_v)
        node.remove()

    def test_remove_node(self):
        node = self.control.add_node(self.space_center.ut, 0, 0, 0)
        node.remove()
        with self.assertRaises(krpc.client.RPCError):
            node.prograde = 0

    def test_remove_nodes(self):
        self.control.add_node(self.space_center.ut+15, 4, -2, 1)
        self.control.add_node(self.space_center.ut+40, 1, 3, 2)
        self.control.add_node(self.space_center.ut+60, 0, 4, 0)
        self.control.remove_nodes()
        # TODO: don't skip the following
        # with self.assertRaises (krpc.client.RPCError):
        #     node.prograde = 0

    def test_get_nodes(self):
        self.assertEqual([], self.control.nodes)
        node0 = self.control.add_node(self.space_center.ut+35, 4, -2, 1)
        self.assertEqual([node0], self.control.nodes)
        node1 = self.control.add_node(self.space_center.ut+15, 1, 3, 2)
        self.assertEqual([node1, node0], self.control.nodes)
        node2 = self.control.add_node(self.space_center.ut+60, 0, 4, 0)
        self.assertEqual([node1, node0, node2], self.control.nodes)
        self.control.remove_nodes()
        self.assertEqual([], self.control.nodes)

    def test_setters(self):
        start_ut = self.space_center.ut
        ut = start_ut + 60
        node = self.control.add_node(ut, 0, 0, 0)
        v = (-50, 500, -150)
        ut2 = ut + 500
        node.ut = ut2
        node.prograde = v[0]
        node.normal = v[1]
        node.radial = v[2]
        self.assertAlmostEqual(ut2, node.ut, delta=1)
        self.assertAlmostEqual(ut2 - start_ut, node.time_to, delta=1)
        self.check(node, v)
        node.remove()

    def test_set_magnitude(self):
        node = self.control.add_node(self.space_center.ut, 1, -2, 3)
        magnitude = 128
        node.delta_v = magnitude
        delta_v = vector(normalize((1, -2, 3))) * magnitude
        self.check(node, delta_v)
        node.remove()

    def test_orbit(self):
        start_ut = self.space_center.ut
        ut = start_ut + 60
        delta_v = (100, 0, 0)
        node = self.control.add_node(ut, *delta_v)
        self.check(node, delta_v)

        orbit0 = self.vessel.orbit
        orbit1 = node.orbit

        # Check semi-major axis using vis-viva equation
        gm = self.space_center.bodies['Kerbin'].gravitational_parameter
        vsq = (orbit0.speed + delta_v[0])**2
        radius = orbit0.radius
        self.assertAlmostEqual(
            gm / ((2*gm/radius) - vsq), orbit1.semi_major_axis, delta=1)

        # Check there is no inclination change
        self.assertAlmostEqual(orbit0.inclination, orbit1.inclination)

        # Check the eccentricity
        radius_pe = orbit1.periapsis
        radius_ap = orbit1.apoapsis
        eccentricit = (radius_ap - radius_pe) / (radius_ap + radius_pe)
        self.assertGreater(orbit1.eccentricity, orbit0.eccentricity)
        self.assertAlmostEqual(eccentricit, orbit1.eccentricity)

    def test_complex(self):
        ut = self.space_center.ut
        node = self.control.add_node(ut + 60, 100, 200, 300)
        self.wait(0.5)
        node.remove()
        node = self.control.add_node(ut+35, 4, -2, 1)
        self.wait(0.5)
        self.control.remove_nodes()


if __name__ == '__main__':
    unittest.main()
