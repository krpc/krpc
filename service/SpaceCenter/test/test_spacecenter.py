import unittest
import time
import krpctest
from krpctest.geometry import norm, normalize

class TestSpaceCenter(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 1000000)
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.set_circular_orbit('Kerbin', 1010000)
        cls.conn = krpctest.connect(cls)
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.other_vessel = next(v for v in cls.sc.vessels if v != cls.vessel)
        cls.vessel.name = 'Vessel'
        cls.other_vessel.name = 'OtherVessel'
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
        self.assertEqual('Vessel', active.name)

        self.sc.active_vessel = self.other_vessel

        active = self.sc.active_vessel
        self.assertEqual('OtherVessel', active.name)

        other_vessel = next(v for v in self.sc.vessels if v != active)
        self.sc.active_vessel = other_vessel

        active = self.sc.active_vessel
        self.assertEqual('Vessel', active.name)

        self.vessel = active
        self.other_vessel = next(v for v in self.sc.vessels if v != active)

    def test_vessels(self):
        active = self.sc.active_vessel
        active.name = 'Active'
        vessels = self.sc.vessels
        self.assertEqual(['Active', 'OtherVessel'], sorted(v.name for v in vessels))
        self.assertEqual(self.sc.vessels, vessels)

    def test_bodies(self):
        self.assertEqual(
            set(['Sun', 'Moho', 'Eve', 'Gilly', 'Kerbin', 'Mun', 'Minmus',
                 'Duna', 'Ike', 'Dres', 'Jool', 'Laythe', 'Vall', 'Tylo',
                 'Bop', 'Pol', 'Eeloo']),
            set(self.sc.bodies.keys()))

    def test_target_body(self):
        self.assertIsNone(self.sc.target_body)
        self.sc.target_body = self.mun
        time.sleep(0.1)
        self.assertEqual(self.mun, self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)
        self.sc.target_body = None
        time.sleep(0.1)
        self.assertIsNone(self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)

    def test_target_vessel(self):
        self.assertIsNone(self.sc.target_vessel)
        self.sc.target_vessel = self.other_vessel
        time.sleep(0.1)
        self.assertIsNone(self.sc.target_body)
        self.assertEqual(self.other_vessel, self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)
        self.sc.target_vessel = None
        time.sleep(0.1)
        self.assertIsNone(self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)

    def test_clear_target(self):
        self.assertIsNone(self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)

        self.sc.target_body = self.mun
        self.assertEqual(self.mun, self.sc.target_body)
        self.sc.clear_target()

        self.assertIsNone(self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)

        self.sc.target_vessel = self.other_vessel
        self.assertEqual(self.other_vessel, self.sc.target_vessel)
        self.sc.clear_target()

        self.assertIsNone(self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)

    def test_save_and_load(self):
        name = self.vessel.name
        self.sc.save('test_save_and_load')
        self.vessel.name = 'vessel_name_before_load'
        time.sleep(0.1)
        self.assertEqual('vessel_name_before_load', self.vessel.name)
        self.sc.load('test_save_and_load')
        self.assertEqual(name, self.vessel.name)

    def test_ut(self):
        ut = self.sc.ut
        time.sleep(1)
        self.assertClose(ut + 1, self.sc.ut, error=0.25)

    def test_g(self):
        self.assertClose(6.673e-11, self.sc.g, error=0.0005e-11)

    def test_transform_position_same_reference_frame(self):
        self.assertClose(
            (1, 2, 3),
            self.sc.transform_position((1, 2, 3), self.ref_vessel, self.ref_vessel))

    def test_transform_position_between_celestial_bodies(self):
        pos = self.sc.transform_position((0, 0, 0), self.ref_kerbin, self.ref_mun)
        self.assertClose(norm(pos), self.mun.orbit.radius)

        pos = self.sc.transform_position((0, 0, 0), self.ref_sun, self.ref_kerbin)
        self.assertClose(norm(pos), self.kerbin.orbit.radius)

    def test_transform_position_between_vessel_and_celestial_body(self):
        pos = self.sc.transform_position((0, 0, 0), self.ref_vessel, self.ref_kerbin)
        self.assertClose(norm(pos), self.vessel.orbit.radius, error=0.01)

    def test_transform_position_between_vessel_and_celestial_bodies(self):
        p0 = self.sc.transform_position((0, 0, 0), self.ref_vessel, self.ref_kerbin)
        p1 = self.sc.transform_position((0, 0, 0), self.ref_vessel, self.ref_sun)
        p2 = self.sc.transform_position((0, 0, 0), self.ref_kerbin, self.ref_sun)

        p3 = tuple(x-y for (x, y) in zip(p1, p2))
        #TODO: sometimes there is a large difference?!?! but only sometimes...
        self.assertClose(norm(p0), norm(p3), error=500)

    #TODO: improve transform direction tests

    def test_transform_direction_same_reference_frame(self):
        direction = normalize((1, 2, 3))
        self.assertClose(direction,
                         self.sc.transform_direction(direction, self.ref_vessel, self.ref_vessel))

    def test_transform_direction_between_celestial_bodies(self):
        up = (0, 1, 0)
        forward = (0, 0, 1)
        self.assertClose(up, self.sc.transform_direction(up, self.ref_kerbin, self.ref_mun))
        self.assertNotClose(
            forward,
            self.sc.transform_direction(forward, self.ref_kerbin, self.ref_mun))
        self.assertClose(
            up,
            self.sc.transform_direction(up, self.ref_sun, self.ref_kerbin))
        self.assertNotClose(
            forward,
            self.sc.transform_direction(forward, self.ref_sun, self.ref_kerbin))

    def test_transform_direction_between_vessel_and_celestial_body(self):
        up = (0, 1, 0)
        self.assertNotClose(up, self.sc.transform_direction(up, self.ref_vessel, self.ref_kerbin))

    #TODO: improve transform rotation tests

    def test_transform_rotation_same_reference_frame(self):
        r = (1, 0, 0, 0)
        self.assertClose(r, self.sc.transform_rotation(r, self.ref_vessel, self.ref_vessel))

    #TODO: improve transform velcoity tests - check it includes rotational velocities

    def test_transform_velocity_same_reference_frame(self):
        pos = (0, 0, 0)
        vel = (1, 2, 3)
        ref = self.ref_vessel
        self.assertClose(vel, self.sc.transform_velocity(pos, vel, ref, ref))
        self.assertClose(vel, self.sc.transform_velocity(pos + (10, 20, 30), vel, ref, ref))

    def test_transform_velocity_between_vessel_and_celestial_body(self):
        vel = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_vessel, self.ref_nr_kerbin)
        self.assertClose(norm(vel), self.vessel.orbit.speed)

    def test_transform_velocity_between_vessel_and_celestial_bodies(self):
        v0 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_vessel, self.ref_nr_kerbin)
        v1 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_vessel, self.ref_nr_sun)
        v2 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_nr_kerbin, self.ref_nr_sun)
        v3 = tuple(x-y for (x, y) in zip(v1, v2))
        self.assertClose(norm(v0), norm(v3))

    def test_transform_velocity_between_celestial_bodies(self):
        v1 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_nr_mun, self.ref_nr_kerbin)
        v2 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_nr_kerbin, self.ref_nr_mun)
        self.assertClose(self.mun.orbit.speed, norm(v1))
        self.assertClose(self.mun.orbit.speed, norm(v2))
        self.assertClose(v1, tuple(-x for x in v2))

        v1 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_nr_kerbin, self.ref_nr_sun)
        v2 = self.sc.transform_velocity((0, 0, 0), (0, 0, 0), self.ref_nr_sun, self.ref_nr_kerbin)
        self.assertClose(self.kerbin.orbit.speed, norm(v1))
        self.assertClose(self.kerbin.orbit.speed, norm(v2))
        self.assertClose(v1, tuple(-x for x in v2))

    def test_transform_velocity_with_rotational_velocity(self):
        direction = 100000 + 600000
        vel = self.sc.transform_velocity((direction, 0, 0), (0, 0, 0),
                                         self.ref_kerbin, self.ref_nr_kerbin)
        self.assertClose(norm(vel), direction * self.kerbin.rotational_speed)

class WarpTestBase(object):

    def test_no_warp(self):
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_can_rails_warp_at(self):
        for factor in range(0, self.maximum_rails_warp_factor+1):
            self.assertTrue(self.sc.can_rails_warp_at(factor))
        self.assertFalse(self.sc.can_rails_warp_at(-1))
        self.assertFalse(self.sc.can_rails_warp_at(self.maximum_rails_warp_factor+1))

    def test_maximum_rails_warp_factor(self):
        self.assertEqual(self.maximum_rails_warp_factor, self.sc.maximum_rails_warp_factor)

    def test_rails_warp(self):
        rates = [1, 5, 10, 50, 100, 1000, 10000, 100000]
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(1, self.sc.warp_rate)
        for factor in range(1, self.maximum_rails_warp_factor+1):
            self.sc.rails_warp_factor = factor
            time.sleep(2)
            self.assertEqual(rates[factor], self.sc.warp_rate)
            self.assertEqual(self.sc.WarpMode.rails, self.sc.warp_mode)
            self.assertEqual(factor, self.sc.rails_warp_factor)
            self.assertEqual(0, self.sc.physics_warp_factor)

        self.sc.rails_warp_factor = 8
        time.sleep(1)
        self.assertEqual(rates[self.maximum_rails_warp_factor], self.sc.warp_rate)
        self.assertEqual(self.maximum_rails_warp_factor, self.sc.rails_warp_factor)
        self.sc.rails_warp_factor = 42
        time.sleep(0.5)
        self.assertEqual(rates[self.maximum_rails_warp_factor], self.sc.warp_rate)
        self.assertEqual(self.maximum_rails_warp_factor, self.sc.rails_warp_factor)

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

    def test_rails_warp_with_active_engine(self):
        if not self.landed:
            self.vessel.control.throttle = 1
            for engine in self.vessel.parts.engines:
                engine.active = True
            time.sleep(0.1)
            self.assertEqual(0, self.sc.maximum_rails_warp_factor)
            self.vessel.control.throttle = 0
            for engine in self.vessel.parts.engines:
                engine.active = False
            time.sleep(1)
            self.assertEqual(self.maximum_rails_warp_factor, self.sc.maximum_rails_warp_factor)

    def test_physics_warp(self):
        rates = [1, 2, 3, 4]
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        for factor in range(1, 4):
            self.sc.physics_warp_factor = factor
            time.sleep(2)
            self.assertEqual(rates[factor], self.sc.warp_rate)
            self.assertEqual(self.sc.WarpMode.physics, self.sc.warp_mode)
            self.assertEqual(0, self.sc.rails_warp_factor)
            self.assertEqual(factor, self.sc.physics_warp_factor)

        self.sc.physics_warp_factor = 4
        time.sleep(0.5)
        self.assertEqual(rates[3], self.sc.warp_rate)
        self.assertEqual(3, self.sc.physics_warp_factor)
        self.sc.physics_warp_factor = 42
        time.sleep(0.5)
        self.assertEqual(rates[3], self.sc.warp_rate)
        self.assertEqual(3, self.sc.physics_warp_factor)

        self.sc.physics_warp_factor = 0
        time.sleep(0.5)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_switch_mode(self):
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
        ut = self.sc.ut + (30*60) # 30 minutes in future
        self.sc.warp_to(ut)
        self.assertClose(ut, self.sc.ut, error=2)

class TestWarpOnLaunchpad(krpctest.TestCase, WarpTestBase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.maximum_rails_warp_factor = 7
        cls.landed = True

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_warp_to_long(self):
        ut = self.sc.ut + (100*60*60) # 100 hours in future
        self.sc.warp_to(ut)
        self.assertClose(ut, self.sc.ut, error=2)

class TestWarpInOrbit(krpctest.TestCase, WarpTestBase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 200000)
        cls.conn = krpctest.connect(cls)
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.maximum_rails_warp_factor = 4
        cls.landed = False

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

class TestWarpInSpace(krpctest.TestCase, WarpTestBase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('Basic')
        krpctest.remove_other_vessels()
        krpctest.set_circular_orbit('Kerbin', 700000)
        cls.conn = krpctest.connect(cls)
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.maximum_rails_warp_factor = 7
        cls.landed = False

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

if __name__ == '__main__':
    unittest.main()
