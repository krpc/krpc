import math

import ksatest

DAY = 24 * 3600


class TestSpaceWeather(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")
        cls.flight = cls.vehicle.flight
        cls.sol = cls.ksa.bodies["Sol"]
        cls.earth = cls.ksa.bodies["Earth"]

    def test_solar_wind(self):
        # Sol's wind blows at 400 km/s and 2 nPa at 1 AU, faster and denser in a CME
        self.assertGreaterEqual(self.flight.solar_wind_speed, 400e3 - 1)
        self.assertLess(self.flight.solar_wind_speed, 3000e3)
        self.assertGreater(self.flight.solar_wind_pressure, 0.5e-9)
        self.assertLess(self.flight.solar_wind_pressure, 1e-7)
        self.assertLess(abs(self.flight.solar_wind_northward_field), 1e-6)

    def test_radiation_storm(self):
        flux = self.flight.proton_flux
        level = self.flight.radiation_storm_level
        self.assertGreaterEqual(flux, 0)
        # The S scale counts decades of flux from 10
        expected = 0 if flux < 10 else min(5, math.floor(math.log10(flux)))
        self.assertEqual(expected, level)

    def test_geomagnetic_storm(self):
        level = self.earth.geomagnetic_storm_level
        dst = self.earth.dst_index
        self.assertGreaterEqual(level, 0)
        self.assertLessEqual(level, 5)
        self.assertLessEqual(dst, 0)
        # The G scale starts at a Dst index of -40 nT
        self.assertEqual(level > 0, dst <= -40e-9)

    def test_geomagnetic_storm_without_magnetosphere(self):
        # Luna has no magnetosphere, and Ganymede's is driven by Jupiter's
        for name in ("Luna", "Ganymede", "Sol"):
            body = self.ksa.bodies[name]
            self.assertEqual(0, body.geomagnetic_storm_level)
            self.assertEqual(0, body.dst_index)

    def test_star_activity(self):
        self.assertGreaterEqual(self.sol.solar_activity, 0)
        self.assertLessEqual(self.sol.solar_activity, 1)
        self.assertIsInstance(self.sol.solar_activity_rising, bool)
        # Sol's cycle is about 11 years
        self.assertGreater(self.sol.time_to_solar_activity_turn, 0)
        self.assertLess(self.sol.time_to_solar_activity_turn, 15 * 365 * DAY)
        # Between the rates at the minimum and maximum of the cycle
        self.assertGreaterEqual(self.sol.coronal_mass_ejections_per_day, 1.1 - 1e-3)
        self.assertLessEqual(self.sol.coronal_mass_ejections_per_day, 4.5 + 1e-3)
        self.assertGreaterEqual(self.sol.active_region_count, 0)

    def test_star_activity_of_planet(self):
        self.assertEqual(0, self.earth.solar_activity)
        self.assertFalse(self.earth.solar_activity_rising)
        self.assertTrue(math.isnan(self.earth.time_to_solar_activity_turn))
        self.assertEqual(0, self.earth.coronal_mass_ejections_per_day)
        self.assertEqual(0, self.earth.active_region_count)
        self.assertEqual([], self.earth.recent_coronal_mass_ejections)

    def test_recent_coronal_mass_ejections(self):
        ut = self.ksa.ut
        recent = self.sol.recent_coronal_mass_ejections
        for cme in recent:
            self.assertEqual(self.sol, cme.star)
            self.assertLessEqual(cme.ut, ut)
            self.assertGreaterEqual(cme.ut, ut - 3 * DAY - 1)
            self.assertGreater(cme.speed, 0)
            self.assertLessEqual(cme.speed, 3000e3)
        self.assertEqual(recent, self.sol.recent_coronal_mass_ejections)

    def test_incoming_coronal_mass_ejections(self):
        ut = self.ksa.ut
        for cme in self.flight.incoming_coronal_mass_ejections:
            self.assertLessEqual(cme.ut, ut)
            self.assertFalse(math.isnan(cme.arrival_time(self.vehicle)))
            self.assertGreaterEqual(cme.arrival_uncertainty(self.vehicle), 0)


class TestCoronalMassEjection(ksatest.TestCase):
    """Follows a CME to Earth. The space weather of a save is set by the star's seed, so the
    start save has a CME 12 days in, heading for Earth at 1148 km/s."""

    @classmethod
    def warp_to(cls, ut):
        # The game stops the warp for space weather alerts
        for _ in range(20):
            if cls.ksa.ut >= ut - 1:
                return
            cls.ksa.warp_to(ut)
        raise AssertionError("The warp did not reach the time")

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")
        cls.flight = cls.vehicle.flight
        cls.sol = cls.ksa.bodies["Sol"]
        cls.warp_to(cls.ksa.ut + 12 * DAY)

    def test_arrival(self):
        incoming = self.flight.incoming_coronal_mass_ejections
        self.assertEqual(1, len(incoming))
        cme = incoming[0]
        self.assertEqual(self.sol, cme.star)
        self.assertIn(cme, self.sol.recent_coronal_mass_ejections)
        self.assertAlmostEqual(1148e3, cme.speed, delta=1e3)

        # A day out the forecast is within half a day
        ut = self.ksa.ut
        arrival = cme.arrival_time(self.vehicle)
        uncertainty = cme.arrival_uncertainty(self.vehicle)
        self.assertGreater(arrival, ut + DAY)
        self.assertLess(arrival, ut + 3 * DAY)
        self.assertGreater(uncertainty, 0)
        self.assertLess(uncertainty, DAY / 2)
        self.assertEqual(400e3, round(self.flight.solar_wind_speed, -3))

        # The uncertainty shrinks as the CME nears
        self.warp_to(ut + DAY)
        self.assertLess(cme.arrival_uncertainty(self.vehicle), uncertainty / 2)

        # The shock brings a faster, denser solar wind, and the CME is no longer incoming
        for _ in range(48):
            if cme not in self.flight.incoming_coronal_mass_ejections:
                break
            self.warp_to(self.ksa.ut + 3600)
        self.assertNotIn(cme, self.flight.incoming_coronal_mass_ejections)
        self.assertTrue(math.isnan(cme.arrival_time(self.vehicle)))
        self.assertTrue(math.isnan(cme.arrival_uncertainty(self.vehicle)))
        self.assertGreater(self.flight.solar_wind_speed, 600e3)
        self.assertGreater(self.flight.solar_wind_pressure, 3e-9)
