import math

import ksatest
from test_parachute import SITE, VEHICLES

DAY = 24 * 3600


class TestNoMission(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    def test_no_mission(self):
        vehicle = self.find_vehicle("Gemini7")
        self.assertIsNone(vehicle.mission)
        for zone in vehicle.habitat_zones:
            self.assertIsNone(zone.mission_supplies)
            self.assertEqual(
                len(zone.crew), zone.planned_kittens(self.ksa.MissionLeg.stay)
            )


class TestMission(ksatest.TestCase):
    """Launches the supply test vehicle with a plan of two days in low orbit."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        name = cls.conn.testing_tools.register_vehicle(str(VEHICLES / "MissionTest"))
        cls.vehicle = cls.ksa.launch_vehicle(name, SITE)
        cls.mission = cls.vehicle.mission
        (cls.zone,) = cls.vehicle.habitat_zones

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def tearDown(self):
        for seat in self.zone.seats:
            seat.kitten = None

    def free_kitten(self):
        for kitten in self.ksa.kittens.values():
            if (
                kitten.vehicle is None
                and not kitten.on_eva
                and not kitten.killed_in_action
            ):
                return kitten
        raise AssertionError("No free kitten")

    def test_plan(self):
        mission = self.mission
        self.assertIsNotNone(mission)
        self.assertEqual(self.ksa.MissionDestination.low_orbit, mission.destination)
        self.assertEqual(self.ksa.bodies["Earth"], mission.body)
        self.assertEqual(self.ksa.MissionArrival.orbit, mission.arrival)
        self.assertEqual(self.ksa.MissionTrip.return_, mission.trip)
        self.assertFalse(mission.custom)
        self.assertAlmostEqual(2 * DAY, mission.stay, delta=1)
        self.assertTrue(math.isnan(mission.arrival_ut))
        self.assertTrue(math.isnan(mission.departure_ut))

    def test_legs(self):
        mission = self.mission
        self.assertTrue(mission.feasible)
        self.assertAlmostEqual(2 * DAY, mission.stay_time, delta=1)
        self.assertAlmostEqual(
            mission.out_time + mission.stay_time + mission.back_time,
            mission.total_time,
            delta=1,
        )
        # Counted down from the launch
        elapsed = self.ksa.ut - self.vehicle.launch_time
        self.assertAlmostEqual(mission.total_time - elapsed, mission.time_left, delta=5)

    def test_unused_zone(self):
        supplies = self.zone.mission_supplies
        self.assertIsNotNone(supplies)
        self.assertEqual(self.ksa.MissionSupplyVerdict.unused, supplies.verdict)
        for leg in (
            self.ksa.MissionLeg.out,
            self.ksa.MissionLeg.stay,
            self.ksa.MissionLeg.back,
        ):
            self.assertEqual(0, self.zone.planned_kittens(leg))

    def test_supplies(self):
        self.zone.seats[0].kitten = self.free_kitten()
        self.assertEqual(1, self.zone.planned_kittens(self.ksa.MissionLeg.stay))
        supplies = self.zone.mission_supplies
        self.assertEqual(self.ksa.MissionSupplyVerdict.enough, supplies.verdict)
        self.assertEqual(0, supplies.short_by)
        self.assertAlmostEqual(self.mission.time_left, supplies.end, delta=5)
        # Breathable air on the launch pad
        self.assertEqual(math.inf, supplies.time_left(self.ksa.LifeSupportSupply.air))

        self.ksa.Debug.empty_resources(self.vehicle)
        ksatest.wait_until(
            lambda: supplies.verdict == self.ksa.MissionSupplyVerdict.short
        )
        first = supplies.first_supply
        self.assertIn(
            first, (self.ksa.LifeSupportSupply.water, self.ksa.LifeSupportSupply.food)
        )
        self.assertGreater(supplies.short_by, 0)
        self.assertAlmostEqual(
            supplies.end - supplies.time_left(first), supplies.short_by, delta=5
        )
        self.ksa.Debug.fill_resources(self.vehicle)
