import math

import ksatest
from test_parachute import SITE

# The kittens the game starts on EVA. The roster names the others at random
EVA_KITTENS = ("Hunter", "Banjo", "Polaris")


def distance(position):
    return math.sqrt(sum(x * x for x in position))


class CrewCase(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def setUp(self):
        self.vehicle = self.find_vehicle("Gemini7")
        (self.kitten,) = self.vehicle.crew

    def free_kitten(self):
        """A kitten that is not in a seat or on EVA."""
        for kitten in self.ksa.kittens.values():
            if (
                kitten.vehicle is None
                and not kitten.on_eva
                and not kitten.killed_in_action
            ):
                return kitten
        raise AssertionError("No free kitten")


class TestCrew(CrewCase):
    def test_kittens(self):
        kittens = self.ksa.kittens
        for name in EVA_KITTENS:
            self.assertIn(name, kittens)
        for name, kitten in kittens.items():
            self.assertEqual(name, kitten.name)
        indices = sorted(kitten.roster_index for kitten in kittens.values())
        self.assertEqual(list(range(len(kittens))), indices)
        self.assertIn(self.kitten, kittens.values())

    def test_vehicle_crew(self):
        self.assertEqual(2, self.vehicle.crew_capacity)
        self.assertEqual(2, len(self.vehicle.seats))
        self.assertEqual(self.vehicle, self.kitten.vehicle)
        self.assertIn(self.kitten.seat, self.vehicle.seats)
        self.assertIn(self.kitten.seat.part, self.vehicle.parts)
        self.assertEqual([self.kitten], self.kitten.seat.part.crew)
        self.assertFalse(self.kitten.on_eva)

    def test_part_seats(self):
        seats = [seat for part in self.vehicle.parts for seat in part.seats]
        self.assertEqual(seats, self.vehicle.seats)
        for seat in seats:
            self.assertIn(seat, seat.part.seats)

    def test_stats(self):
        self.assertFalse(self.kitten.killed_in_action)
        self.assertGreaterEqual(self.kitten.mission_count, 1)
        self.assertGreaterEqual(self.kitten.mission_time, 0)
        self.assertGreaterEqual(self.kitten.distance_traveled, 0)
        self.assertGreaterEqual(self.kitten.fastest_speed, 0)

    def test_radiation(self):
        flight = self.vehicle.flight
        # The game credits the crew's dose in its solver step
        ksatest.wait_until(lambda: self.kitten.dose_rate > 0)
        # The capsule shields its crew
        self.assertLess(self.kitten.dose_rate, flight.dose_rate)
        self.assertGreater(self.kitten.acute_dose, 0)
        self.assertGreaterEqual(self.kitten.lifetime_dose, self.kitten.acute_dose)
        self.assertGreaterEqual(self.kitten.recovery_time, 0)
        self.assertFalse(self.kitten.radiation_sickness)
        self.assertEqual(0, self.kitten.time_until_fit_to_fly)
        self.assertFalse(self.kitten.retired)

    def test_free_kitten(self):
        kitten = self.free_kitten()
        self.assertIsNone(kitten.vehicle)
        self.assertIsNone(kitten.seat)
        self.assertEqual(0, kitten.mission_time)
        self.assertEqual(0, kitten.dose_rate)

    def test_launch_kitten(self):
        with self.assertRaises(ValueError):
            self.ksa.launch_vehicle("Banjo", SITE)


class TestSeats(CrewCase):
    def setUp(self):
        self.load_save()
        super().setUp()
        (self.empty_seat,) = [
            seat for seat in self.vehicle.seats if seat.kitten is None
        ]

    def test_assign(self):
        kitten = self.free_kitten()
        self.empty_seat.kitten = kitten
        self.assertEqual(kitten, self.empty_seat.kitten)
        self.assertEqual(self.empty_seat, kitten.seat)
        self.assertEqual(self.vehicle, kitten.vehicle)
        self.assertCountEqual([self.kitten, kitten], self.vehicle.crew)
        self.empty_seat.kitten = None
        self.assertIsNone(self.empty_seat.kitten)
        self.assertIsNone(kitten.vehicle)
        self.assertEqual([self.kitten], self.vehicle.crew)

    def test_move(self):
        seat = self.kitten.seat
        self.empty_seat.kitten = self.kitten
        self.assertEqual(self.empty_seat, self.kitten.seat)
        self.assertIsNone(seat.kitten)
        self.assertEqual([self.kitten], self.vehicle.crew)

    def test_kitten_on_eva(self):
        with self.assertRaises(RuntimeError):
            self.empty_seat.kitten = self.ksa.kittens["Hunter"]

    def test_kitten_in_another_vehicle(self):
        (kitten,) = self.find_vehicle("Rocket").crew
        with self.assertRaises(RuntimeError):
            self.empty_seat.kitten = kitten
        self.assertEqual(self.find_vehicle("Rocket"), kitten.vehicle)


class TestEva(CrewCase):
    def setUp(self):
        self.load_save()
        super().setUp()
        self.frame = self.vehicle.reference_frame

    def test_kittens_on_eva(self):
        for name in EVA_KITTENS:
            with self.subTest(kitten=name):
                kitten = self.ksa.kittens[name]
                self.assertTrue(kitten.on_eva)
                self.assertIsNone(kitten.vehicle)
                self.assertIsNone(kitten.seat)
                self.assertEqual(self.ksa.LocomotionMode.mmu, kitten.locomotion_mode)
                self.assertLess(distance(kitten.position(self.frame)), 10000)
                self.assertLess(distance(kitten.velocity(self.frame)), 10)
                self.assertNotIn(name, [vehicle.name for vehicle in self.ksa.vehicles])

    def test_not_on_eva(self):
        # A kitten in a seat is where the seat's part is
        part = self.kitten.seat.part
        for expected, actual in (
            (part.position(self.frame), self.kitten.position(self.frame)),
            (part.velocity(self.frame), self.kitten.velocity(self.frame)),
        ):
            for x, y in zip(expected, actual):
                self.assertAlmostEqual(x, y, delta=1e-3)
        with self.assertRaises(RuntimeError):
            self.kitten.locomotion_mode
        with self.assertRaises(RuntimeError):
            self.kitten.board()

    def test_not_on_a_mission(self):
        kitten = self.free_kitten()
        with self.assertRaises(RuntimeError):
            kitten.position(self.frame)
        with self.assertRaises(RuntimeError):
            kitten.velocity(self.frame)

    def test_go_on_eva_and_board(self):
        self.kitten.go_on_eva()
        self.assertTrue(self.kitten.on_eva)
        self.assertIsNone(self.kitten.seat)
        self.assertEqual([], self.vehicle.crew)
        self.assertEqual(self.kitten, self.ksa.active_kitten)
        self.assertIsNone(self.ksa.active_vehicle)
        self.assertLess(distance(self.kitten.position(self.frame)), 10)

        # The kitten grabs the hatch's rail, which puts the hatch in reach
        mode = self.ksa.LocomotionMode
        ksatest.wait_until(lambda: self.kitten.locomotion_mode == mode.ladder)
        self.assertEqual(self.vehicle, self.kitten.board())
        self.assertFalse(self.kitten.on_eva)
        self.assertEqual(self.vehicle, self.kitten.vehicle)
        self.assertEqual([self.kitten], self.vehicle.crew)
        self.assertEqual(self.vehicle, self.ksa.active_vehicle)
        self.assertIsNone(self.ksa.active_kitten)

    def test_go_on_eva_without_a_seat(self):
        with self.assertRaises(RuntimeError):
            self.free_kitten().go_on_eva()

    def test_board_out_of_range(self):
        with self.assertRaises(RuntimeError):
            self.ksa.kittens["Banjo"].board()

    def test_active_kitten(self):
        self.assertIsNone(self.ksa.active_kitten)
        kitten = self.ksa.kittens["Hunter"]
        self.ksa.active_kitten = kitten
        ksatest.wait_until(lambda: self.ksa.active_kitten == kitten)
        self.assertIsNone(self.ksa.active_vehicle)
        self.ksa.active_vehicle = self.vehicle
        ksatest.wait_until(lambda: self.ksa.active_vehicle == self.vehicle)
        self.assertIsNone(self.ksa.active_kitten)

    def test_active_kitten_not_on_eva(self):
        with self.assertRaises(RuntimeError):
            self.ksa.active_kitten = self.kitten
