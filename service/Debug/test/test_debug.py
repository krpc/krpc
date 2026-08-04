import math
import unittest

import krpctest
from krpctest.geometry import angle_between, norm


class TestDebug(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.debug = cls.connect().debug
        cls.space_center = cls.connect().space_center
        cls.kerbin = cls.space_center.bodies["Kerbin"]

    @property
    def vessel(self):
        # Teleporting reloads the vessel, so fetch it afresh rather than caching it.
        return self.space_center.active_vessel

    def test_set_circular_orbit(self):
        self.debug.set_circular_orbit(self.kerbin, 100000)
        orbit = self.vessel.orbit
        self.assertEqual(self.kerbin, orbit.body)
        self.assertAlmostEqual(0, orbit.eccentricity, places=3)
        self.assertAlmostEqual(100000, orbit.apoapsis_altitude, delta=1000)
        self.assertAlmostEqual(100000, orbit.periapsis_altitude, delta=1000)

    def test_set_orbit(self):
        # The angles are in radians, as SpaceCenter.Orbit reports them.
        self.debug.set_orbit(
            self.kerbin,
            900000,
            0.1,
            math.radians(30),
            math.radians(40),
            math.radians(50),
            1.5,
            0,
        )
        orbit = self.vessel.orbit
        self.assertEqual(self.kerbin, orbit.body)
        self.assertAlmostEqual(900000, orbit.semi_major_axis, delta=100)
        self.assertAlmostEqual(0.1, orbit.eccentricity, places=3)
        self.assertRadiansAlmostEqual(math.radians(30), orbit.inclination, places=3)
        self.assertRadiansAlmostEqual(
            math.radians(40), orbit.longitude_of_ascending_node, places=3
        )
        self.assertRadiansAlmostEqual(
            math.radians(50), orbit.argument_of_periapsis, places=3
        )

    def test_set_orbit_hyperbolic(self):
        # An escape trajectory. The game wants a negative semi-major axis for an open orbit;
        # given a positive one alongside an eccentricity above 1 its solver fills with NaN and
        # takes the flight scene down with it, so the sign comes from the eccentricity.
        self.debug.set_orbit(self.kerbin, 800000, 3, 0, 0, 0, 0, 0)
        orbit = self.vessel.orbit
        self.assertEqual(self.kerbin, orbit.body)
        self.assertAlmostEqual(3, orbit.eccentricity, places=3)
        self.assertAlmostEqual(-800000, orbit.semi_major_axis, delta=100)
        self.assertAlmostEqual(1600000, orbit.periapsis, delta=100)

    def test_set_landed(self):
        latitude = 10
        longitude = -30
        self.debug.set_landed(self.kerbin, latitude, longitude)
        self.assertEqual(
            self.space_center.VesselSituation.landed, self.vessel.situation
        )
        flight = self.vessel.flight(self.kerbin.reference_frame)
        self.assertAlmostEqual(latitude, flight.latitude, delta=0.1)
        self.assertAlmostEqual(longitude, flight.longitude, delta=0.1)
        self.assertAlmostEqual(0, flight.speed, delta=1)

    def test_set_flight(self):
        speed = 100
        heading = 45
        pitch = 10
        self.debug.set_flight(
            self.kerbin,
            self.KSC_LATITUDE,
            self.KSC_LONGITUDE,
            5000,
            speed,
            heading,
            pitch,
        )
        vessel = self.vessel
        # Airspeed is measured against the rotating body; the attitude angles come from
        # the vessel's own surface frame, which is what the navball shows.
        self.assertAlmostEqual(
            speed, vessel.flight(self.kerbin.reference_frame).speed, delta=5
        )
        flight = vessel.flight(vessel.surface_reference_frame)
        self.assertAlmostEqual(5000, flight.mean_altitude, delta=100)
        self.assertDegreesAlmostEqual(heading, flight.heading, delta=2)
        self.assertDegreesAlmostEqual(pitch, flight.pitch, delta=2)

    def test_set_position(self):
        self.set_circular_orbit("Kerbin", 100000)
        frame = self.kerbin.non_rotating_reference_frame
        vessel = self.vessel
        position = vessel.position(frame)
        # Move 1 km further out along the position vector, keeping the velocity.
        target = tuple(p * (1 + 1000 / norm(position)) for p in position)
        self.debug.set_position(target, frame)
        vessel = self.vessel
        self.assertAlmostEqual(norm(target), norm(vessel.position(frame)), delta=100)

    def test_set_velocity(self):
        self.set_circular_orbit("Kerbin", 100000)
        frame = self.kerbin.non_rotating_reference_frame
        vessel = self.vessel
        # Slow the vessel down by 100 m/s along its current velocity, which lowers the
        # periapsis without moving it.
        velocity = vessel.velocity(frame)
        periapsis = vessel.orbit.periapsis_altitude
        target = tuple(v * (1 - 100 / norm(velocity)) for v in velocity)
        self.debug.set_velocity(target, frame)
        vessel = self.vessel
        self.assertAlmostEqual(norm(target), norm(vessel.velocity(frame)), delta=5)
        self.assertLess(vessel.orbit.periapsis_altitude, periapsis - 1000)

    def test_set_pitch_heading_roll(self):
        self.set_circular_orbit("Kerbin", 100000)
        self.debug.set_pitch_heading_roll(20, 130, 45)
        vessel = self.vessel
        flight = vessel.flight(vessel.surface_reference_frame)
        self.assertDegreesAlmostEqual(20, flight.pitch, delta=1)
        self.assertDegreesAlmostEqual(130, flight.heading, delta=1)
        self.assertDegreesAlmostEqual(45, flight.roll, delta=1)

    def test_set_direction(self):
        self.set_circular_orbit("Kerbin", 100000)
        vessel = self.vessel
        # Straight up, in the surface reference frame.
        self.debug.set_direction((1, 0, 0), 0)
        flight = vessel.flight(vessel.surface_reference_frame)
        self.assertDegreesAlmostEqual(90, flight.pitch, delta=1)

    def test_apply_rotation(self):
        self.set_circular_orbit("Kerbin", 100000)
        self.debug.set_pitch_heading_roll(0, 90, 0)
        vessel = self.vessel
        frame = vessel.surface_reference_frame
        before = vessel.direction(frame)
        # A rotation about an axis across the vessel swings the nose by the same angle.
        self.debug.apply_rotation(30, (1, 0, 0))
        self.assertAlmostEqual(
            30, angle_between(before, vessel.direction(frame)), delta=2
        )

    def test_set_angular_velocity(self):
        self.set_circular_orbit("Kerbin", 100000)
        vessel = self.vessel
        frame = vessel.surface_reference_frame
        self.debug.set_angular_velocity((0, 0, 0.5), frame)
        self.wait(0.5)
        self.assertAlmostEqual(0.5, norm(vessel.angular_velocity(frame)), delta=0.1)
        self.debug.set_angular_velocity((0, 0, 0), frame)
        self.wait(0.5)
        self.assertAlmostEqual(0, norm(vessel.angular_velocity(frame)), delta=0.05)

    def test_fill_resources(self):
        self.set_circular_orbit("Kerbin", 100000)
        vessel = self.vessel
        resources = vessel.resources
        maximum = resources.max("ElectricCharge")
        # Drain some charge by holding the reaction wheels against the vessel.
        vessel.control.sas = False
        vessel.control.pitch = 1
        self.wait_until(
            lambda: resources.amount("ElectricCharge") < maximum,
            message="the reaction wheels did not draw any electric charge",
        )
        vessel.control.pitch = 0
        self.debug.fill_resources("ElectricCharge")
        self.assertAlmostEqual(maximum, resources.amount("ElectricCharge"), places=3)

    def test_fill_all_resources(self):
        self.set_circular_orbit("Kerbin", 100000)
        resources = self.vessel.resources
        self.debug.fill_all_resources()
        for name in resources.names:
            self.assertAlmostEqual(
                resources.max(name), resources.amount(name), places=3
            )

    def test_cheat_options(self):
        options = [
            "infinite_propellant",
            "infinite_electricity",
            "no_crash_damage",
            "unbreakable_joints",
            "ignore_max_temperature",
            "allow_part_clipping",
            "non_strict_attachment_orientation",
            "ignore_kerbal_inventory_limits",
            "ignore_eva_construction_mass_limit",
            "ignore_agency_mindset_on_contracts",
            "pause_on_vessel_unpack",
            "biomes_visible",
        ]
        for name in options:
            original = getattr(self.debug, name)
            try:
                setattr(self.debug, name, True)
                self.assertTrue(getattr(self.debug, name), name)
                setattr(self.debug, name, False)
                self.assertFalse(getattr(self.debug, name), name)
            finally:
                setattr(self.debug, name, original)

    def test_gravity_multiplier(self):
        self.assertAlmostEqual(1, self.debug.gravity_multiplier, places=3)
        try:
            self.debug.gravity_multiplier = 0.5
            self.assertAlmostEqual(0.5, self.debug.gravity_multiplier, places=3)
        finally:
            self.debug.gravity_multiplier = 1

    def test_career_not_available_in_sandbox(self):
        self.assertRaises(RuntimeError, getattr, self.debug, "funds")
        self.assertRaises(RuntimeError, getattr, self.debug, "science")
        self.assertRaises(RuntimeError, getattr, self.debug, "reputation")


class TestDebugCareer(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save("krpctest_career", always_load=True)
        cls.debug = cls.connect().debug
        cls.space_center = cls.connect().space_center

    def test_funds(self):
        self.debug.funds = 123456
        self.assertAlmostEqual(123456, self.debug.funds, places=3)
        self.assertAlmostEqual(123456, self.space_center.funds, places=3)

    def test_science(self):
        self.debug.science = 42
        self.assertAlmostEqual(42, self.debug.science, places=3)
        self.assertAlmostEqual(42, self.space_center.science, places=3)

    def test_reputation(self):
        self.debug.reputation = 33
        self.assertAlmostEqual(33, self.debug.reputation, places=3)
        self.assertAlmostEqual(33, self.space_center.reputation, places=3)

    # The next three call the game's own debug-menu implementations, and kRPC exposes no
    # technology, facility or roster state to compare against, so these check that the RPC
    # is reachable and completes rather than what it did.

    def test_unlock_technology_tree(self):
        self.debug.unlock_technology_tree()

    def test_upgrade_facilities(self):
        self.debug.upgrade_facilities()

    def test_max_kerbal_experience(self):
        self.debug.max_kerbal_experience()


if __name__ == "__main__":
    unittest.main()
