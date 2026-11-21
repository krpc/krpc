import math

import ksatest
from test_power import TestVehicleCase

DAY = 24 * 60 * 60


class TestLifeSupport(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.find_vehicle("Gemini7")

    def test_rules(self):
        self.assertTrue(self.ksa.life_support_enabled)
        self.assertTrue(self.ksa.radiation_enabled)

    def test_vehicle(self):
        life_support = self.vehicle.life_support
        self.assertEqual(2, life_support.kittens)
        # Three days for both kittens when full
        self.assertGreater(life_support.air_mass, 0)
        self.assertLessEqual(life_support.air_mass, 5.41)
        self.assertGreater(life_support.water_mass, 0)
        self.assertLessEqual(life_support.water_mass, 19.21)
        self.assertGreater(life_support.meals, 0)
        self.assertLessEqual(life_support.meals, 18)
        times = {
            self.ksa.LifeSupportSupply.air: life_support.air_time,
            self.ksa.LifeSupportSupply.scrubber: life_support.scrubber_time,
            self.ksa.LifeSupportSupply.water: life_support.water_time,
            self.ksa.LifeSupportSupply.food: life_support.food_time,
        }
        for time in times.values():
            self.assertGreater(time, 0)
            self.assertLess(time, 4 * DAY)
        self.assertAlmostEqual(
            min(times.values()), life_support.time, delta=life_support.time * 1e-6
        )
        self.assertEqual(
            min(times, key=lambda supply: times[supply]), life_support.limiting_supply
        )
        # Oxygen at 0.895 kg per kitten per day
        self.assertAlmostEqual(
            life_support.air_mass / (2 * 0.895) * DAY,
            life_support.air_time,
            delta=life_support.air_time * 0.01,
        )
        if life_support.time > DAY:
            self.assertEqual(self.ksa.LifeSupportLevel.ok, life_support.level)
            self.assertAlmostEqual(
                life_support.time - DAY, life_support.time_to_next_level, delta=1
            )

    def cabin(self):
        (cabin,) = [h for h in self.vehicle.habitats if h.habitable_volume > 0]
        return cabin

    def test_zones(self):
        # The capsule's cabin and the service module's bays form one zone
        (zone,) = self.vehicle.habitat_zones
        self.assertEqual(self.vehicle, zone.vehicle)
        self.assertCountEqual(self.vehicle.crew, zone.crew)
        self.assertCountEqual(self.vehicle.seats, zone.seats)
        self.assertCountEqual(self.vehicle.habitats, zone.habitats)
        self.assertEqual(2, len(zone.habitats))
        for habitat in zone.habitats:
            self.assertIn(habitat.part, zone.parts)
            self.assertIn(habitat, habitat.part.habitats)
            self.assertEqual(zone, habitat.zone)
        self.assertAlmostEqual(1.4, zone.habitable_volume, delta=0.01)
        self.assertAlmostEqual(
            sum(h.storage_volume for h in zone.habitats), zone.storage_volume
        )
        # The vehicle's one zone is its worst
        vehicle = self.vehicle.life_support
        life_support = zone.life_support
        self.assertEqual(vehicle.kittens, life_support.kittens)
        self.assertEqual(vehicle.meals, life_support.meals)
        self.assertAlmostEqual(vehicle.air_mass, life_support.air_mass, delta=1e-3)
        self.assertAlmostEqual(vehicle.time, life_support.time, delta=60)

    def test_habitat(self):
        habitat = self.cabin()
        self.assertEqual(18, habitat.meal_capacity)
        self.assertEqual(4, habitat.canister_capacity)
        self.assertEqual(self.vehicle.life_support.meals, habitat.meals)
        self.assertLessEqual(habitat.meals, habitat.meal_capacity)
        self.assertLessEqual(habitat.canisters, habitat.canister_capacity)


class TestNoHabitats(TestVehicleCase):
    vehicle_folder = "ScoopTest"

    def test_no_habitats(self):
        self.assertIsNone(self.vehicle.life_support)
        self.assertEqual([], self.vehicle.habitat_zones)
        self.assertEqual([], self.vehicle.habitats)


class TestSupplyTransfer(TestVehicleCase):
    vehicle_folder = "SupplyTest"

    def setUp(self):
        (self.zone,) = self.vehicle.habitat_zones
        habitats = sorted(self.zone.habitats, key=lambda h: h.habitable_volume)
        self.locker, self.cabin = habitats
        self.assertEqual(0, self.locker.meals)

    def free_kitten(self):
        for kitten in self.ksa.kittens.values():
            if (
                kitten.vehicle is None
                and not kitten.on_eva
                and not kitten.killed_in_action
            ):
                return kitten
        raise AssertionError("No free kitten")

    def test_zone(self):
        self.assertEqual(0, self.locker.habitable_volume)
        self.assertEqual(0, self.locker.canisters)
        self.assertGreater(self.locker.meal_capacity, 0)
        self.assertGreater(self.locker.storage_volume, 0)
        self.assertEqual(self.zone, self.locker.zone)
        self.assertAlmostEqual(
            self.cabin.storage_volume + self.locker.storage_volume,
            self.zone.storage_volume,
        )
        # Breathable air on the launch pad
        self.assertTrue(math.isinf(self.zone.life_support.air_time))

    def test_transfer(self):
        for seat in self.zone.seats:
            seat.kitten = None
        with self.assertRaises(RuntimeError):
            self.cabin.transfer(self.locker, meals=1)
        with self.assertRaises(ValueError):
            self.cabin.transfer(self.cabin, meals=1)
        with self.assertRaises(ValueError):
            self.cabin.transfer(self.locker, meals=-1)

        self.zone.seats[0].kitten = self.free_kitten()
        meals = self.cabin.meals
        canisters = self.cabin.canisters
        locker_meals = self.locker.meals
        locker_canisters = self.locker.canisters
        self.cabin.transfer(self.locker, meals=2, canisters=1)
        self.assertEqual(meals - 2, self.cabin.meals)
        self.assertEqual(canisters - 1, self.cabin.canisters)
        self.assertEqual(locker_meals + 2, self.locker.meals)
        self.assertEqual(locker_canisters + 1, self.locker.canisters)
        self.locker.transfer(self.cabin, meals=2, canisters=1)
        self.assertEqual(meals, self.cabin.meals)
        self.assertEqual(canisters, self.cabin.canisters)


class TestKittenCondition(ksatest.TestCase):
    def warp_to(self, ut):
        # The game stops the warp when supplies run out, and when a kitten's condition
        # worsens
        for _ in range(10):
            if self.ksa.ut >= ut - 1:
                return
            self.ksa.warp_to(ut)
        self.fail("The warp did not reach the time")

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def setUp(self):
        self.vehicle = self.find_vehicle("Gemini7")
        self.kittens = self.vehicle.crew

    def test_healthy(self):
        supply = self.ksa.LifeSupportSupply
        healthy = self.ksa.KittenCondition.healthy
        for kitten in self.kittens:
            self.assertIsNone(kitten.cause_of_death)
            self.assertEqual(healthy, kitten.worst_condition)
            self.assertFalse(kitten.incapacitated)
            for s in (supply.air, supply.scrubber, supply.water, supply.food):
                self.assertEqual(0, kitten.time_without(s))
                self.assertEqual(healthy, kitten.condition(s))
        self.assertTrue(self.vehicle.has_control_module)
        self.assertTrue(self.vehicle.controllable)

    def test_without_supplies(self):
        # With two kittens in 1.4 cubic meters, the canisters run out first: degraded
        # after 0.875 hours without, incapacitated after 1.925 and dead after 2.8
        supply = self.ksa.LifeSupportSupply
        condition = self.ksa.KittenCondition
        self.load_save()
        self.setUp()
        start = self.ksa.ut
        self.ksa.Debug.empty_resources(self.vehicle)
        self.assertEqual(self.ksa.LifeSupportLevel.out, self.vehicle.life_support.level)

        self.warp_to(start + 3600)
        for kitten in self.kittens:
            self.assertAlmostEqual(3600, kitten.time_without(supply.air), delta=300)
            self.assertEqual(condition.degraded, kitten.condition(supply.scrubber))
            self.assertEqual(condition.warning, kitten.condition(supply.air))
            self.assertEqual(condition.degraded, kitten.worst_condition)
            self.assertFalse(kitten.incapacitated)
            self.assertFalse(kitten.can_go_on_eva)
        self.assertTrue(self.vehicle.controllable)

        self.warp_to(start + 2.3 * 3600)
        for kitten in self.kittens:
            self.assertEqual(condition.incapacitated, kitten.worst_condition)
            self.assertTrue(kitten.incapacitated)
            self.assertIsNone(kitten.cause_of_death)
        self.assertTrue(self.vehicle.has_control_module)
        self.assertFalse(self.vehicle.controllable)
        kitten, crewmate = self.kittens
        seat = crewmate.seat
        with self.assertRaises(RuntimeError):
            seat.kitten = kitten
        self.assertEqual(crewmate, seat.kitten)
        with self.assertRaises(RuntimeError):
            kitten.go_on_eva()

        self.warp_to(start + 3.3 * 3600)
        for kitten in self.kittens:
            self.assertTrue(kitten.killed_in_action)
            self.assertEqual(self.ksa.DeathCause.no_scrubber, kitten.cause_of_death)
