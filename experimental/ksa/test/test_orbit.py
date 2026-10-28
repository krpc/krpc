import math

import ksatest


class TestOrbit(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.orbit = cls.find_vehicle("Gemini7").orbit
        cls.earth = cls.ksa.bodies["Earth"]

    def test_start_orbit(self):
        # Gemini7StartingSituation in the game's Situations.xml
        semi_major_axis = (self.orbit.apoapsis + self.orbit.periapsis) / 2
        self.assertAlmostEqual(6713e3, semi_major_axis, delta=1e3)
        self.assertAlmostEqual(0.0077, self.orbit.eccentricity, delta=1e-4)
        self.assertAlmostEqual(math.radians(28.87), self.orbit.inclination, delta=1e-3)
        self.assertAlmostEqual(
            math.radians(60.48), self.orbit.longitude_of_ascending_node, delta=1e-3
        )
        self.assertAlmostEqual(
            math.radians(30.0015), self.orbit.argument_of_periapsis, delta=1e-3
        )
        self.assertEqual(self.earth, self.orbit.body)

    def test_consistency(self):
        apoapsis = self.orbit.apoapsis
        periapsis = self.orbit.periapsis
        self.assertGreaterEqual(apoapsis, periapsis)
        self.assertAlmostEqual(
            (apoapsis - periapsis) / (apoapsis + periapsis),
            self.orbit.eccentricity,
            places=6,
        )
        self.assertAlmostEqual(
            apoapsis - self.earth.radius, self.orbit.apoapsis_altitude, delta=1
        )
        self.assertAlmostEqual(
            periapsis - self.earth.radius, self.orbit.periapsis_altitude, delta=1
        )

    def test_semi_major_axis(self):
        self.assertAlmostEqual(
            (self.orbit.apoapsis + self.orbit.periapsis) / 2,
            self.orbit.semi_major_axis,
            delta=1,
        )

    def test_speed(self):
        # The vis-viva equation
        flight = self.find_vehicle("Gemini7").flight
        r = flight.mean_altitude + self.earth.radius
        mu = self.earth.gravitational_parameter
        speed = math.sqrt(mu * (2 / r - 1 / self.orbit.semi_major_axis))
        self.assertAlmostEqual(speed, self.orbit.speed, delta=1)

    def test_period(self):
        semi_major_axis = (self.orbit.apoapsis + self.orbit.periapsis) / 2
        mu = self.earth.gravitational_parameter
        period = 2 * math.pi * math.sqrt(semi_major_axis**3 / mu)
        self.assertAlmostEqual(period, self.orbit.period, delta=1e-6 * period)

    def test_time_to_apsides(self):
        period = self.orbit.period
        to_apoapsis = self.orbit.time_to_apoapsis
        to_periapsis = self.orbit.time_to_periapsis
        self.assertGreaterEqual(to_apoapsis, 0)
        self.assertLess(to_apoapsis, period)
        self.assertGreaterEqual(to_periapsis, 0)
        self.assertLess(to_periapsis, period)
        # The apsides are half an orbit apart
        self.assertAlmostEqual(
            period / 2, (to_apoapsis - to_periapsis) % period, delta=1
        )

    def test_position_and_velocity_at(self):
        vehicle = self.find_vehicle("Gemini7")
        frame = self.earth.non_rotating_reference_frame
        # Paused, so every call sees the same game time
        self.ksa.simulation_speed = 0
        try:
            ut = self.ksa.ut
            for expected, actual in zip(
                vehicle.position(frame), self.orbit.position_at(ut, frame)
            ):
                self.assertAlmostEqual(expected, actual, delta=1)
            # The vehicle's velocity can be a physics step from its orbit's, which
            # gravity changes by about 0.15 m/s
            for expected, actual in zip(
                vehicle.velocity(frame), self.orbit.velocity_at(ut, frame)
            ):
                self.assertAlmostEqual(expected, actual, delta=0.3)
            self.assertAlmostEqual(self.orbit.radius, self.orbit.radius_at(ut), delta=1)
        finally:
            self.ksa.simulation_speed = 1

    def test_anomalies(self):
        ut = self.ksa.ut + 1000
        true_anomaly = self.orbit.true_anomaly_at_ut(ut)
        self.assertAlmostEqual(
            ut, self.orbit.ut_at_true_anomaly(true_anomaly), delta=1e-3
        )
        self.assertAlmostEqual(
            self.ksa.ut + self.orbit.time_to_apoapsis,
            self.orbit.ut_at_true_anomaly(math.pi),
            delta=1,
        )
        # Nearly circular, so the mean anomaly is close to the true anomaly
        self.assertAlmostEqual(
            0,
            (self.orbit.mean_anomaly - self.orbit.true_anomaly + math.pi)
            % (2 * math.pi)
            - math.pi,
            delta=2 * self.orbit.eccentricity,
        )

    def test_no_transition(self):
        self.assertEqual(self.ksa.OrbitTransition.none, self.orbit.end_transition)
        self.assertEqual(math.inf, self.orbit.end_ut)
        self.assertLessEqual(self.orbit.start_ut, self.ksa.ut)
        self.assertTrue(math.isnan(self.orbit.time_to_soi_change))
        self.assertIsNone(self.orbit.next_orbit)

    def test_body_orbits(self):
        luna = self.ksa.bodies["Luna"].orbit
        self.assertEqual(self.earth, luna.body)
        self.assertAlmostEqual(3.8e8, luna.semi_major_axis, delta=0.1e8)
        self.assertAlmostEqual(27, luna.period / 86400, delta=1)
        self.assertTrue(math.isnan(luna.start_ut))
        self.assertEqual(math.inf, luna.end_ut)
        earth = self.earth.orbit
        self.assertEqual(self.ksa.bodies["Sol"], earth.body)
        self.assertAlmostEqual(365.25, earth.period / 86400, delta=1)
        self.assertIsNone(self.ksa.bodies["Sol"].orbit)

    def test_shape(self):
        a = self.orbit.semi_major_axis
        e = self.orbit.eccentricity
        self.assertAlmostEqual(
            a * math.sqrt(1 - e * e), self.orbit.semi_minor_axis, delta=1
        )
        self.assertAlmostEqual(
            2 * math.pi, self.orbit.mean_motion * self.orbit.period, places=6
        )

    def test_specific_energy(self):
        # Kinetic plus potential energy per unit mass
        vehicle = self.find_vehicle("Gemini7")
        frame = self.earth.non_rotating_reference_frame
        r = math.sqrt(sum(x * x for x in vehicle.position(frame)))
        v = math.sqrt(sum(x * x for x in vehicle.velocity(frame)))
        mu = self.earth.gravitational_parameter
        energy = v * v / 2 - mu / r
        self.assertAlmostEqual(
            energy, self.orbit.specific_energy, delta=abs(energy) * 1e-5
        )
        self.assertLess(self.orbit.specific_energy, 0)

    def test_eccentric_anomaly(self):
        e = self.orbit.eccentricity
        eccentric = self.orbit.eccentric_anomaly
        # Kepler's equation
        self.assertAlmostEqual(
            0,
            (eccentric - e * math.sin(eccentric) - self.orbit.mean_anomaly + math.pi)
            % (2 * math.pi)
            - math.pi,
            places=4,
        )

    def test_relative_inclination(self):
        luna = self.ksa.bodies["Luna"].orbit
        self.assertAlmostEqual(0, self.orbit.relative_inclination(self.orbit), places=6)
        inclination = self.orbit.relative_inclination(luna)
        self.assertGreater(inclination, 0)
        self.assertLess(inclination, math.pi)
        self.assertAlmostEqual(
            inclination, luna.relative_inclination(self.orbit), places=6
        )

    def test_nodes(self):
        luna = self.ksa.bodies["Luna"].orbit
        ascending = self.orbit.true_anomaly_at_an(luna)
        descending = self.orbit.true_anomaly_at_dn(luna)
        self.assertAlmostEqual(
            math.pi,
            (descending - ascending) % (2 * math.pi),
            delta=self.orbit.eccentricity * 4,
        )
        with self.assertRaises(ValueError):
            self.orbit.true_anomaly_at_an(self.ksa.bodies["Earth"].orbit)

    def test_closest_approach(self):
        vehicle = self.find_vehicle("Gemini7")
        self.assertTrue(math.isnan(self.orbit.time_of_closest_approach))
        self.assertTrue(math.isnan(self.orbit.distance_at_closest_approach))
        rocket = self.find_vehicle("Rocket")
        try:
            vehicle.target_vehicle = rocket
            ksatest.wait_until(
                lambda: not math.isnan(self.orbit.time_of_closest_approach), timeout=5
            )
            ut = self.orbit.time_of_closest_approach
            distance = self.orbit.distance_at_closest_approach
            self.assertGreater(ut, self.ksa.ut)
            self.assertLess(ut, self.ksa.ut + 2 * self.orbit.period)
            self.assertGreater(distance, self.orbit.periapsis_altitude - 20e3)
            # The positions at that time are that far apart
            frame = self.earth.non_rotating_reference_frame
            a = self.orbit.position_at(ut, frame)
            b = rocket.orbit.position_at(ut, frame)
            self.assertAlmostEqual(distance, math.dist(a, b), delta=distance * 0.01)
        finally:
            vehicle.target_vehicle = None


class TestPredictedOrbit(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle

    def tearDown(self):
        for burn in self.vehicle.burns:
            burn.remove()

    def burn_orbit(self, prograde, transition):
        burn = self.vehicle.add_burn(self.ksa.ut + 900, prograde=prograde)
        # The game predicts the orbit after the burn on a worker thread
        ksatest.wait_until(lambda: burn.orbit.end_transition == transition)
        return burn, burn.orbit

    def test_escape(self):
        burn, orbit = self.burn_orbit(3300, self.ksa.OrbitTransition.escape)
        self.assertGreater(orbit.eccentricity, 1)
        self.assertLess(orbit.semi_major_axis, 0)
        self.assertTrue(math.isnan(orbit.period))
        self.assertTrue(math.isnan(orbit.semi_minor_axis))
        self.assertGreater(orbit.specific_energy, 0)
        self.assertAlmostEqual(burn.ut, orbit.start_ut, delta=burn.burn_time)
        self.assertGreater(orbit.time_to_soi_change, 0)
        self.assertAlmostEqual(
            orbit.end_ut - self.ksa.ut, orbit.time_to_soi_change, delta=1
        )
        next_orbit = orbit.next_orbit
        self.assertIsNotNone(next_orbit)
        self.assertEqual(self.ksa.bodies["Sol"], next_orbit.body)
        self.assertAlmostEqual(orbit.end_ut, next_orbit.start_ut, delta=1)

    def test_impact(self):
        _, orbit = self.burn_orbit(-300, self.ksa.OrbitTransition.impact)
        self.assertLess(orbit.periapsis_altitude, 0)
        self.assertGreater(orbit.end_ut, self.ksa.ut)
        self.assertIsNone(orbit.next_orbit)
        # The vehicle's own trajectory does not hit the surface
        self.assertTrue(math.isnan(self.vehicle.flight.time_to_impact))
