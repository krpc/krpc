import unittest
import krpctest
from krpctest.geometry import norm, normalize


class TestSpaceCenter(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Basic')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 1000000)
        cls.launch_vessel_from_vab('Basic')
        cls.set_circular_orbit('Kerbin', 1010000)
        cls.sc = cls.connect().space_center
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

    def test_launchable_vessels(self):
        # TODO: implement test
        # print self.sc.launchable_vessels("SPH")
        # print self.sc.launchable_vessels("VAB")
        pass

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
        self.assertItemsEqual(
            ['Active', 'OtherVessel'], [v.name for v in vessels])
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
        self.wait()
        self.assertEqual(self.mun, self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)
        self.sc.target_body = None
        self.wait()
        self.assertIsNone(self.sc.target_body)
        self.assertIsNone(self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)

    def test_target_vessel(self):
        self.assertIsNone(self.sc.target_vessel)
        self.sc.target_vessel = self.other_vessel
        self.wait()
        self.assertIsNone(self.sc.target_body)
        self.assertEqual(self.other_vessel, self.sc.target_vessel)
        self.assertIsNone(self.sc.target_docking_port)
        self.sc.target_vessel = None
        self.wait()
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
        self.wait()
        self.assertEqual('vessel_name_before_load', self.vessel.name)
        self.sc.load('test_save_and_load')
        self.assertEqual(name, self.vessel.name)

    def test_ut(self):
        ut = self.sc.ut
        self.wait(1)
        self.assertAlmostEqual(ut + 1, self.sc.ut, delta=0.25)

    def test_g(self):
        self.assertAlmostEqual(6.67408e-11, self.sc.g, delta=0.001e-11)

    def test_ui_visible(self):
        self.assertTrue(self.sc.ui_visible)
        self.sc.ui_visible = False
        self.wait(1)
        self.assertFalse(self.sc.ui_visible)
        self.sc.ui_visible = True
        self.wait(1)
        self.assertTrue(self.sc.ui_visible)

    def test_navball(self):
        self.assertTrue(self.sc.navball)
        self.sc.navball = False
        self.wait(1)
        self.assertFalse(self.sc.navball)
        self.sc.navball = True
        self.wait(1)
        self.assertTrue(self.sc.navball)

    def test_transform_position_same_reference_frame(self):
        self.assertAlmostEqual(
            (1, 2, 3),
            self.sc.transform_position(
                (1, 2, 3), self.ref_vessel, self.ref_vessel))

    def test_transform_position_between_celestial_bodies(self):
        pos = self.sc.transform_position(
            (0, 0, 0), self.ref_kerbin, self.ref_mun)
        self.assertAlmostEqual(norm(pos), self.mun.orbit.radius, places=3)

        pos = self.sc.transform_position(
            (0, 0, 0), self.ref_sun, self.ref_kerbin)
        self.assertAlmostEqual(norm(pos), self.kerbin.orbit.radius, places=3)

    def test_transform_position_between_vessel_and_celestial_body(self):
        pos = self.sc.transform_position(
            (0, 0, 0), self.ref_vessel, self.ref_kerbin)
        self.assertAlmostEqual(norm(pos), self.vessel.orbit.radius, places=2)

    def test_transform_position_between_vessel_and_celestial_bodies(self):
        p0 = self.sc.transform_position(
            (0, 0, 0), self.ref_vessel, self.ref_kerbin)
        p1 = self.sc.transform_position(
            (0, 0, 0), self.ref_vessel, self.ref_sun)
        p2 = self.sc.transform_position(
            (0, 0, 0), self.ref_kerbin, self.ref_sun)

        p3 = tuple(x-y for (x, y) in zip(p1, p2))
        # TODO: sometimes there is a large difference?!?! but only sometimes...
        self.assertAlmostEqual(norm(p0), norm(p3), delta=5000)

    # TODO: improve transform direction tests

    def test_transform_direction_same_reference_frame(self):
        direction = normalize((1, 2, 3))
        self.assertAlmostEqual(
            direction,
            self.sc.transform_direction(
                direction, self.ref_vessel, self.ref_vessel))

    def test_transform_direction_between_celestial_bodies(self):
        up = (0, 1, 0)
        forward = (0, 0, 1)
        self.assertAlmostEqual(
            up, self.sc.transform_direction(
                up, self.ref_kerbin, self.ref_mun))
        self.assertNotAlmostEqual(
            forward,
            self.sc.transform_direction(
                forward, self.ref_kerbin, self.ref_mun))
        self.assertAlmostEqual(
            up,
            self.sc.transform_direction(up, self.ref_sun, self.ref_kerbin))
        self.assertNotAlmostEqual(
            forward,
            self.sc.transform_direction(
                forward, self.ref_sun, self.ref_kerbin))

    def test_transform_direction_between_vessel_and_celestial_body(self):
        up = (0, 1, 0)
        self.assertNotAlmostEqual(
            up,
            self.sc.transform_direction(up, self.ref_vessel, self.ref_kerbin))

    # TODO: improve transform rotation tests

    def test_transform_rotation_same_reference_frame(self):
        r = (1, 0, 0, 0)
        self.assertAlmostEqual(
            r, self.sc.transform_rotation(r, self.ref_vessel, self.ref_vessel))

    # TODO: improve transform velocity tests
    #       - check it includes rotational velocities

    def test_transform_velocity_same_reference_frame(self):
        vel = (1, 2, 3)
        ref = self.ref_vessel
        self.assertAlmostEqual(
            vel, self.sc.transform_velocity((0, 0, 0), vel, ref, ref))
        self.assertAlmostEqual(
            vel, self.sc.transform_velocity((10, 20, 30), vel, ref, ref))

    def test_transform_velocity_between_vessel_and_celestial_body(self):
        vel = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_vessel, self.ref_nr_kerbin)
        self.assertAlmostEqual(norm(vel), self.vessel.orbit.speed, places=3)

    def test_transform_velocity_between_vessel_and_celestial_bodies(self):
        v0 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_vessel, self.ref_nr_kerbin)
        v1 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_vessel, self.ref_nr_sun)
        v2 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_nr_kerbin, self.ref_nr_sun)
        v3 = tuple(x-y for (x, y) in zip(v1, v2))
        self.assertAlmostEqual(norm(v0), norm(v3), places=3)

    def test_transform_velocity_between_celestial_bodies(self):
        v1 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_nr_mun, self.ref_nr_kerbin)
        v2 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_nr_kerbin, self.ref_nr_mun)
        self.assertAlmostEqual(self.mun.orbit.speed, norm(v1), places=3)
        self.assertAlmostEqual(self.mun.orbit.speed, norm(v2), places=3)
        self.assertAlmostEqual(v1, tuple(-x for x in v2), places=3)

        v1 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_nr_kerbin, self.ref_nr_sun)
        v2 = self.sc.transform_velocity(
            (0, 0, 0), (0, 0, 0), self.ref_nr_sun, self.ref_nr_kerbin)
        self.assertAlmostEqual(self.kerbin.orbit.speed, norm(v1), places=3)
        self.assertAlmostEqual(self.kerbin.orbit.speed, norm(v2), places=3)
        self.assertAlmostEqual(v1, tuple(-x for x in v2), places=3)

    def test_transform_velocity_with_rotational_velocity(self):
        direction = 100000 + 600000
        vel = self.sc.transform_velocity(
            (direction, 0, 0), (0, 0, 0), self.ref_kerbin, self.ref_nr_kerbin)
        self.assertAlmostEqual(
            norm(vel), direction * self.kerbin.rotational_speed, places=3)


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
        self.assertFalse(
            self.sc.can_rails_warp_at(self.maximum_rails_warp_factor+1))

    def test_maximum_rails_warp_factor(self):
        self.assertEqual(
            self.maximum_rails_warp_factor, self.sc.maximum_rails_warp_factor)

    def test_rails_warp(self):
        rates = [1, 5, 10, 50, 100, 1000, 10000, 100000]
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(1, self.sc.warp_rate)
        for factor in range(1, self.maximum_rails_warp_factor+1):
            self.sc.rails_warp_factor = factor
            self.wait(2)
            self.assertEqual(rates[factor], self.sc.warp_rate)
            self.assertEqual(self.sc.WarpMode.rails, self.sc.warp_mode)
            self.assertEqual(factor, self.sc.rails_warp_factor)
            self.assertEqual(0, self.sc.physics_warp_factor)

        self.sc.rails_warp_factor = 8
        self.wait(1)
        self.assertEqual(
            rates[self.maximum_rails_warp_factor], self.sc.warp_rate)
        self.assertEqual(
            self.maximum_rails_warp_factor, self.sc.rails_warp_factor)
        self.sc.rails_warp_factor = 42
        self.wait(0.5)
        self.assertEqual(
            rates[self.maximum_rails_warp_factor], self.sc.warp_rate)
        self.assertEqual(
            self.maximum_rails_warp_factor, self.sc.rails_warp_factor)

        self.sc.rails_warp_factor = 0
        self.wait(1)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

        self.sc.rails_warp_factor = -1
        self.wait(0.5)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_rails_warp_with_active_engine(self):
        if not self.landed:
            self.vessel.control.throttle = 1
            for engine in self.vessel.parts.engines:
                engine.active = True
            self.wait()
            self.assertEqual(0, self.sc.maximum_rails_warp_factor)
            self.vessel.control.throttle = 0
            for engine in self.vessel.parts.engines:
                engine.active = False
            self.wait(1)
            self.assertEqual(
                self.maximum_rails_warp_factor,
                self.sc.maximum_rails_warp_factor)

    def test_physics_warp(self):
        rates = [1, 2, 3, 4]
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        for factor in range(1, 4):
            self.sc.physics_warp_factor = factor
            self.wait(2)
            self.assertEqual(rates[factor], self.sc.warp_rate)
            self.assertEqual(self.sc.WarpMode.physics, self.sc.warp_mode)
            self.assertEqual(0, self.sc.rails_warp_factor)
            self.assertEqual(factor, self.sc.physics_warp_factor)

        self.sc.physics_warp_factor = 4
        self.wait(0.5)
        self.assertEqual(rates[3], self.sc.warp_rate)
        self.assertEqual(3, self.sc.physics_warp_factor)
        self.sc.physics_warp_factor = 42
        self.wait(0.5)
        self.assertEqual(rates[3], self.sc.warp_rate)
        self.assertEqual(3, self.sc.physics_warp_factor)

        self.sc.physics_warp_factor = 0
        self.wait(0.5)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.assertEqual(0, self.sc.rails_warp_factor)
        self.assertEqual(0, self.sc.physics_warp_factor)
        self.assertEqual(1, self.sc.warp_rate)

    def test_switch_mode(self):
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)
        self.sc.rails_warp_factor = 2
        self.wait(2)
        self.assertEqual(self.sc.WarpMode.rails, self.sc.warp_mode)
        self.assertEqual(10, self.sc.warp_rate)
        self.sc.physics_warp_factor = 2
        self.wait(2)
        self.assertEqual(self.sc.WarpMode.physics, self.sc.warp_mode)
        self.assertEqual(3, self.sc.warp_rate)
        self.sc.rails_warp_factor = 0
        self.wait(2)
        self.assertEqual(self.sc.WarpMode.none, self.sc.warp_mode)

    def test_warp_to(self):
        ut = self.sc.ut + (30*60)  # 30 minutes in future
        self.sc.warp_to(ut)
        self.assertAlmostEqual(ut, self.sc.ut, delta=2)


class TestWarpOnLaunchpad(krpctest.TestCase, WarpTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Basic')
        cls.remove_other_vessels()
        cls.sc = cls.connect().space_center
        cls.vessel = cls.sc.active_vessel
        cls.maximum_rails_warp_factor = 7
        cls.landed = True
        cls.wait(1)  # TODO: why is this wait needed?

    def test_warp_to_long(self):
        ut = self.sc.ut + (100*60*60)  # 100 hours in future
        self.sc.warp_to(ut)
        self.assertAlmostEqual(ut, self.sc.ut, delta=2)


class TestWarpInOrbit(krpctest.TestCase, WarpTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Basic')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 200000)
        cls.sc = cls.connect().space_center
        cls.vessel = cls.sc.active_vessel
        cls.maximum_rails_warp_factor = 4
        cls.landed = False
        cls.wait(1)  # TODO: why is this wait needed?


class TestWarpInSpace(krpctest.TestCase, WarpTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab('Basic')
        cls.remove_other_vessels()
        cls.set_circular_orbit('Kerbin', 700000)
        cls.sc = cls.connect().space_center
        cls.vessel = cls.sc.active_vessel
        cls.maximum_rails_warp_factor = 7
        cls.landed = False
        cls.wait(1)  # TODO: why is this wait needed?


if __name__ == '__main__':
    unittest.main()
