import unittest
import testingtools
from mathtools import norm, normalize
import krpc
import time
import itertools

class TestSpaceCenter(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('Basic')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 1000000)
        testingtools.launch_vessel_from_vab('Basic')
        testingtools.set_circular_orbit('Kerbin', 1010000)
        cls.conn = krpc.connect(name='TestSpaceCenter')
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.other_vessel = next(iter(filter(lambda v: v != cls.vessel, cls.sc.vessels)))
        cls.ref_vessel = cls.vessel.reference_frame
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

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_active_vessel(self):
        active = self.sc.active_vessel
        active.name = 'Active'
        self.assertEqual(active.name, 'Active')
        self.assertEqual(self.sc.active_vessel, active)

    def test_vessels(self):
        active = self.sc.active_vessel
        active.name = 'Active'
        vessels = self.sc.vessels
        self.assertEqual(['Active', 'Basic'], sorted(v.name for v in vessels))
        self.assertEqual(self.sc.vessels, vessels)

    def test_bodies(self):
        self.assertEqual(set([
            'Sun', 'Moho', 'Eve', 'Gilly', 'Kerbin', 'Mun', 'Minmus',
            'Duna', 'Ike', 'Dres', 'Jool', 'Laythe', 'Vall', 'Tylo',
            'Bop', 'Pol', 'Eeloo']), set(self.sc.bodies.keys()))

    def test_target_body(self):
        self.assertEqual(None, self.sc.target_body)
        self.sc.target_body = self.mun
        time.sleep(1)
        self.assertEqual(self.mun, self.sc.target_body)
        self.assertEqual(None, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)
        self.sc.target_body = None
        time.sleep(1)
        self.assertEqual(None, self.sc.target_body)
        self.assertEqual(None, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)

    def test_target_vessel(self):
        self.assertEqual(None, self.sc.target_vessel)
        self.sc.target_vessel = self.other_vessel
        time.sleep(1)
        self.assertEqual(None, self.sc.target_body)
        self.assertEqual(self.other_vessel, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)
        self.sc.target_vessel = None
        time.sleep(1)
        self.assertEqual(None, self.sc.target_body)
        self.assertEqual(None, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)

    def test_clear_target(self):
        self.assertEqual(None, self.sc.target_body)
        self.assertEqual(None, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)

        self.sc.target_body = self.mun
        self.assertEqual(self.mun, self.sc.target_body)
        self.sc.clear_target()

        self.assertEqual(None, self.sc.target_body)
        self.assertEqual(None, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)

        self.sc.target_vessel = self.other_vessel
        self.assertEqual(self.other_vessel, self.sc.target_vessel)
        self.sc.clear_target()

        self.assertEqual(None, self.sc.target_body)
        self.assertEqual(None, self.sc.target_vessel)
        self.assertEqual(None, self.sc.target_docking_port)

    def test_ut(self):
        ut = self.sc.ut
        time.sleep(1)
        self.assertClose(ut + 1, self.sc.ut, error=0.25)

    def test_g(self):
        self.assertClose(6.673e-11, self.sc.g, error=0.0005e-11)

    def test_warp_when_throttled_up(self):
        self.sc.active_vessel.control.throttle = 1
        self.sc.active_vessel.control.activate_next_stage()
        time.sleep(0.1)
        self.assertEquals(0, self.sc.maximum_rails_warp_factor)
        self.sc.active_vessel.control.throttle = 0
        time.sleep(1)
        self.assertEquals(7, self.sc.maximum_rails_warp_factor)

    def test_no_warp(self):
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_rails_warp(self):
        rates = [5.0, 10, 50, 100, 1000, 10000, 100000]
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(1, self.sc.warp_rate)
        for factor in range(1,8):
            self.sc.rails_warp_factor = factor
            time.sleep(2)
            self.assertEqual(self.sc.WarpMode.rails, self.sc.warp_mode)
            self.assertEqual(self.sc.rails_warp_factor, factor)
            self.assertEqual(self.sc.physics_warp_factor, 0)
            self.assertEqual(rates[factor-1], self.sc.warp_rate)

        self.sc.rails_warp_factor = 8
        time.sleep(0.5)
        self.assertEqual(7, self.sc.rails_warp_factor)
        self.assertEqual(rates[6], self.sc.warp_rate)
        self.sc.rails_warp_factor = 42
        time.sleep(0.5)
        self.assertEqual(7, self.sc.rails_warp_factor)
        self.assertEqual(rates[6], self.sc.warp_rate)

        self.sc.rails_warp_factor = 0
        time.sleep(1)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

        self.sc.rails_warp_factor = -1
        time.sleep(0.5)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_physics_warp(self):
        rates = [2,3,4]
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        for factor in range(1,4):
            self.sc.physics_warp_factor = factor
            time.sleep(2)
            self.assertEqual(self.sc.WarpMode.physics, self.sc.warp_mode)
            self.assertEqual(self.sc.rails_warp_factor, 0)
            self.assertEqual(self.sc.physics_warp_factor, factor)
            self.assertEqual(rates[factor-1], self.sc.warp_rate)

        self.sc.physics_warp_factor = 4
        time.sleep(0.5)
        self.assertEqual(3, self.sc.physics_warp_factor)
        self.assertEqual(rates[2], self.sc.warp_rate)
        self.sc.physics_warp_factor = 42
        time.sleep(0.5)
        self.assertEqual(3, self.sc.physics_warp_factor)
        self.assertEqual(rates[2], self.sc.warp_rate)

        self.sc.physics_warp_factor = 0
        time.sleep(0.5)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_warp_switch_mode(self):
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.sc.rails_warp_factor = 2
        time.sleep(2)
        self.assertEqual(self.sc.WarpMode.rails, self.sc.warp_mode)
        self.assertEqual(10, self.sc.warp_rate)
        self.sc.physics_warp_factor = 2
        time.sleep(2)
        self.assertEqual(self.sc.WarpMode.physics, self.sc.warp_mode)
        self.assertEqual(3, self.sc.warp_rate)
        self.sc.rails_warp_factor = 0
        time.sleep(2)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)

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

        p3 = tuple(x-y for (x,y) in zip(p1,p2))
        #TODO: sometimes there is a large difference?!?! but only sometimes...
        self.assertClose(norm(p0), norm(p3), error=500)

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
        v = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_vessel, self.ref_nr_kerbin)
        self.assertClose(self.vessel.orbit.speed, norm(v))

    def test_transform_velocity_between_vessel_and_celestial_bodies(self):
        v0 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_vessel, self.ref_nr_kerbin)
        v1 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_vessel, self.ref_nr_sun)
        v2 = self.sc.transform_velocity((0,0,0), (0,0,0), self.ref_nr_kerbin, self.ref_nr_sun)
        v3 = tuple(x-y for (x,y) in zip(v1,v2))
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
