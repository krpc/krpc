import ksatest

SITE = "CCSFS LC-39A"
VEHICLES = ksatest.ROOT / "testing" / "vehicles"


class ParachuteTestCase(ksatest.TestCase):
    """Launches a single parachute part, from the test vehicles, on the launch pad."""

    vehicle_folder = ""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        name = cls.conn.testing_tools.register_vehicle(
            str(VEHICLES / cls.vehicle_folder)
        )
        cls.vehicle = cls.ksa.launch_vehicle(name, SITE)
        cls.parachute = cls.vehicle.root_part.parachute

    @classmethod
    def tearDownClass(cls):
        cls.load_save()


class TestMainParachute(ParachuteTestCase):
    vehicle_folder = "ParachuteTest"

    def test_parachute(self):
        states = self.ksa.ParachuteState
        self.assertEqual(self.vehicle.root_part, self.parachute.part)
        self.assertEqual(states.stowed, self.parachute.state)
        self.assertEqual(0, self.parachute.fill)
        self.assertGreaterEqual(self.parachute.deploy_altitude, 0)
        # Main parachutes cannot be cut
        self.assertFalse(self.parachute.can_cut)
        self.parachute.deploy()
        self.assertNotEqual(states.stowed, self.parachute.state)
        with self.assertRaises(RuntimeError):
            self.parachute.deploy()
        with self.assertRaises(RuntimeError):
            self.parachute.cut()


class TestDrogueParachute(ParachuteTestCase):
    vehicle_folder = "DrogueTest"

    def test_cut(self):
        states = self.ksa.ParachuteState
        self.assertTrue(self.parachute.can_cut)
        self.parachute.deploy()
        ksatest.wait_until(lambda: self.parachute.state == states.reefed)
        self.parachute.cut()
        self.assertEqual(states.cut, self.parachute.state)
