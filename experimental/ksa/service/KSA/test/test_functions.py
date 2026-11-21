import ksatest


class TestFunctions(ksatest.TestCase):
    """Server side functions over the KSA service."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.orbit = cls.find_vehicle("Gemini7").orbit

    def test_run(self):
        orbit = self.orbit
        function = self.conn.compile(lambda: (orbit.apoapsis + orbit.periapsis) / 2)
        self.assertAlmostEqual(
            (orbit.apoapsis + orbit.periapsis) / 2, self.conn.run(function), delta=1
        )

    def test_stream(self):
        orbit = self.orbit
        stream = self.conn.add_stream(lambda: orbit.apoapsis - orbit.periapsis)
        try:
            self.assertAlmostEqual(orbit.apoapsis - orbit.periapsis, stream(), delta=1)
        finally:
            stream.remove()

    def test_event(self):
        ksa = self.ksa
        start = ksa.ut
        event = self.conn.add_event(lambda: ksa.ut > start + 0.5)
        with event.condition:
            self.assertTrue(event.wait(timeout=10))
        self.assertGreater(ksa.ut, start + 0.5)

    def test_vehicle_equality(self):
        ksa = self.ksa
        vehicle = self.find_vehicle("Gemini7")
        function = self.conn.compile(
            lambda: [v.name for v in ksa.vehicles if v == vehicle]
        )
        self.assertEqual(["Gemini7"], self.conn.run(function))
