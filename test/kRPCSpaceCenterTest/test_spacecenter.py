import unittest
import testingtools
from mathtools import norm, normalize
import krpc
import time
import itertools

class TestBody(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Basic')
        testingtools.set_circular_orbit('Kerbin', 260000)
        cls.conn = krpc.connect()
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.ref_vessel = cls.vessel.reference_frame
        cls.ref_nr_vessel = cls.vessel.non_rotating_reference_frame
        bodies = cls.sc.bodies
        cls.sun = bodies['Sun']
        cls.kerbin = bodies['Kerbin']
        cls.mun = bodies['Mun']
        cls.ref_sun = cls.sun.reference_frame
        cls.ref_kerbin = cls.kerbin.reference_frame
        cls.ref_mun = cls.mun.reference_frame
        cls.ref_nr_sun = cls.sun.non_rotating_reference_frame
        cls.ref_nr_kerbin = cls.kerbin.non_rotating_reference_frame
        cls.ref_nr_mun = cls.mun.non_rotating_reference_frame

    def test_active_vessel(self):
        active = self.sc.active_vessel
        self.assertEqual(active.name, 'Basic')
        self.assertEqual(self.sc.active_vessel, active)

    def test_vessels(self):
        vessels = self.sc.vessels
        self.assertEqual(set(['Basic']), set(v.name for v in vessels))
        self.assertEqual(self.sc.vessels, vessels)

    def test_bodies(self):
        self.assertEqual(set([
            'Sun', 'Moho', 'Eve', 'Gilly', 'Kerbin', 'Mun', 'Minmus',
            'Duna', 'Ike', 'Dres', 'Jool', 'Laythe', 'Vall', 'Tylo',
            'Bop', 'Pol', 'Eeloo']), set(self.sc.bodies.keys()))

    def test_ut(self):
        ut = self.sc.ut
        time.sleep(1)
        self.assertClose(ut + 1, self.sc.ut, error=0.25)

    def test_g(self):
        self.assertEqual(6.673e-11, self.sc.g)

    def test_warp_to(self):
        t = self.sc.ut + (5*60)
        self.sc.warp_to(t)
        self.assertClose(t, self.sc.ut, error=2)

    def test_transform_position_same_reference_frame(self):
        self.assertClose((1,2,3), self.sc.transform_position((1,2,3), self.ref_vessel, self.ref_vessel))

    def test_transform_position_between_celestial_bodies(self):
        p = self.sc.transform_position((0,0,0), self.ref_kerbin, self.ref_mun)
        self.assertClose(self.mun.orbit.radius, norm(p))

        p = self.sc.transform_position((0,0,0), self.ref_sun, self.ref_kerbin)
        self.assertClose(self.kerbin.orbit.radius, norm(p))

    def test_transform_position_between_vessel_and_celestial_body(self):
        p = self.sc.transform_position((0,0,0), self.ref_vessel, self.ref_kerbin)
        self.assertClose(self.vessel.orbit.radius, norm(p), error=0.01)

    def test_transform_position_between_vessel_and_celestial_bodies(self):
        p0 = self.sc.transform_position((0,0,0), self.ref_vessel, self.ref_kerbin)
        p1 = self.sc.transform_position((0,0,0), self.ref_vessel, self.ref_sun)
        p2 = self.sc.transform_position((0,0,0), self.ref_kerbin, self.ref_sun)

        p3 = tuple(x-y for (x,y) in itertools.izip(p1,p2))
        #TODO: sometimes there is a large difference?!?! but only sometimes...
        self.assertClose(norm(p0), norm(p3), error=100)

    #TODO: improve transform direction tests

    def test_transform_direction_same_reference_frame(self):
        d = normalize((1,2,3))
        self.assertClose(d, self.sc.transform_direction(d, self.ref_vessel, self.ref_vessel))

    def test_transform_direction_between_celestial_bodies(self):
        up = (0,1,0)
        forward = (0,0,1)
        self.assertClose(up, self.sc.transform_direction(up, self.ref_kerbin, self.ref_mun))
        self.assertNotClose(forward, self.sc.transform_direction(forward, self.ref_kerbin, self.ref_mun))
        self.assertClose(up, self.sc.transform_direction(up, self.ref_sun, self.ref_kerbin))
        self.assertNotClose(forward, self.sc.transform_direction(forward, self.ref_sun, self.ref_kerbin))

    def test_transform_direction_between_vessel_and_celestial_body(self):
        up = (0,1,0)
        self.assertNotClose(up, self.sc.transform_direction(up, self.ref_vessel, self.ref_kerbin))

    #TODO: improve transform rotation tests

    def test_transform_rotation_same_reference_frame(self):
        r = (1,0,0,0)
        self.assertClose(r, self.sc.transform_rotation(r, self.ref_vessel, self.ref_vessel))

    #TODO: improve transform velcoity tests - check it includes rotational velocities

    def test_transform_velocity_same_reference_frame(self):
        p = (0,0,0)
        v = (1,2,3)
        r = self.ref_vessel
        self.assertClose(v, self.sc.transform_velocity(p, v, r, r))
        self.assertClose(v, self.sc.transform_velocity(p + (10,20,30), v, r, r))

    def test_transform_velocity_between_vessel_and_celestial_body(self):
        v = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_vessel, self.ref_nr_kerbin)
        self.assertClose(self.vessel.orbit.speed, norm(v))

    def test_transform_velocity_between_vessel_and_celestial_bodies(self):
        v0 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_vessel, self.ref_nr_kerbin)
        v1 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_vessel, self.ref_nr_sun)
        v2 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_kerbin, self.ref_nr_sun)
        v3 = tuple(x-y for (x,y) in itertools.izip(v1,v2))
        self.assertClose(norm(v0), norm(v3))

    def test_transform_velocity_between_celestial_bodies(self):
        v1 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_mun, self.ref_nr_kerbin)
        v2 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_kerbin, self.ref_nr_mun)
        self.assertClose(self.mun.orbit.speed, norm(v1))
        self.assertClose(self.mun.orbit.speed, norm(v2))
        self.assertClose(v1, tuple(-x for x in v2))

        v1 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_kerbin, self.ref_nr_sun)
        v2 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_sun, self.ref_nr_kerbin)
        self.assertClose(self.kerbin.orbit.speed, norm(v1))
        self.assertClose(self.kerbin.orbit.speed, norm(v2))
        self.assertClose(v1, tuple(-x for x in v2))

    def test_transform_velocity_with_rotational_velocity(self):
        d = 100000 + 600000
        v = self.sc.transform_velocity((d,0,0), (0,0,0), self.ref_kerbin, self.ref_nr_kerbin)
        self.assertClose(d * self.kerbin.rotational_speed, norm(v))

if __name__ == "__main__":
    unittest.main()
