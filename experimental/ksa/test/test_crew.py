import ksatest

# The kittens the game starts on EVA. The roster names the others at random
EVA_KITTENS = ("Hunter", "Banjo", "Polaris")


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

    def test_free_kitten(self):
        kitten = self.free_kitten()
        self.assertIsNone(kitten.vehicle)
        self.assertIsNone(kitten.seat)
        self.assertEqual(0, kitten.mission_time)


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
