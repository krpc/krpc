import math

import ksatest


class TestBurn(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.auto_pilot = cls.vehicle.auto_pilot

    def tearDown(self):
        self.auto_pilot.auto_burn = False
        for burn in self.vehicle.burns:
            burn.remove()

    def test_add_burn(self):
        ut = self.ksa.ut + 900
        burn = self.vehicle.add_burn(ut, prograde=50)
        self.assertEqual([burn], self.vehicle.burns)
        self.assertAlmostEqual(ut, burn.ut, delta=1e-3)
        self.assertEqual((50, 0, 0), (burn.prograde, burn.normal, burn.radial))
        self.assertAlmostEqual(50, burn.delta_v)
        self.assertAlmostEqual(50, burn.remaining_delta_v, delta=1e-3)
        # The burn is centered on its time
        self.assertGreater(burn.burn_time, 0)
        self.assertAlmostEqual(
            ut - burn.burn_time / 2, burn.ignition_time, delta=burn.burn_time * 0.1
        )

    def test_remove(self):
        burn = self.vehicle.add_burn(self.ksa.ut + 900, prograde=10)
        burn.remove()
        self.assertEqual([], self.vehicle.burns)
        with self.assertRaises(RuntimeError):
            _ = burn.delta_v

    def test_order(self):
        later = self.vehicle.add_burn(self.ksa.ut + 1800, prograde=10)
        sooner = self.vehicle.add_burn(self.ksa.ut + 900, prograde=10)
        self.assertEqual([sooner, later], self.vehicle.burns)
        ksatest.wait_until(lambda: not math.isnan(sooner.ignition_time))
        # The flight computer times the next burn only
        self.assertTrue(math.isnan(later.ignition_time))
        self.assertTrue(math.isnan(later.burn_time))

    def test_set_delta_v(self):
        burn = self.vehicle.add_burn(self.ksa.ut + 900)
        burn.prograde = 30
        burn.normal = 40
        burn.radial = -10
        self.assertEqual((30, 40, -10), (burn.prograde, burn.normal, burn.radial))
        self.assertAlmostEqual(math.sqrt(30**2 + 40**2 + 10**2), burn.delta_v)

    def test_direction(self):
        # The orbit normal is the same along the orbit
        frame = self.vehicle.orbital_reference_frame
        burn = self.vehicle.add_burn(self.ksa.ut + 900, normal=10)
        direction = burn.direction(frame)
        self.assertAlmostEqual(1, direction[1], delta=1e-6)
        burn.normal = 0
        burn.prograde = 10
        self.assertAlmostEqual(0, burn.direction(frame)[1], delta=1e-6)

    def test_invalid_burns(self):
        with self.assertRaises(ValueError):
            self.vehicle.add_burn(self.ksa.ut - 10, prograde=10)
        with self.assertRaises(ValueError):
            self.vehicle.add_burn(self.ksa.ut + 900, prograde=math.nan)
        burn = self.vehicle.add_burn(self.ksa.ut + 900, prograde=10)
        with self.assertRaises(ValueError):
            burn.normal = math.inf

    def test_auto_burn_without_burn(self):
        with self.assertRaises(RuntimeError):
            self.auto_pilot.auto_burn = True
        self.assertFalse(self.auto_pilot.auto_burn)

    def test_auto_burn(self):
        orbit = self.vehicle.orbit
        mu = orbit.body.gravitational_parameter
        # A prograde burn in the nearly circular orbit raises the opposite side
        burn = self.vehicle.add_burn(self.ksa.ut + 900, prograde=50)
        radius = orbit.body.radius + orbit.periapsis_altitude
        speed = math.sqrt(mu / radius) + 50
        semi_major_axis = 1 / (2 / radius - speed**2 / mu)
        apoapsis = 2 * semi_major_axis - radius - orbit.body.radius
        self.ksa.warp_to(burn.ignition_time - 30)
        self.auto_pilot.auto_burn = True
        ksatest.wait_until(lambda: not self.auto_pilot.auto_burn, timeout=90)
        self.assertLess(burn.remaining_delta_v, 0.5)
        self.assertAlmostEqual(apoapsis, orbit.apoapsis_altitude, delta=5e3)
