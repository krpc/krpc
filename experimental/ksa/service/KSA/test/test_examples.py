import subprocess
import sys

import ksatest
import pytest

EXAMPLES = ksatest.ROOT / "examples"


@pytest.mark.slow
class TestExamples(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def run_example(self, name):
        result = subprocess.run(
            [sys.executable, str(EXAMPLES / name)],
            capture_output=True,
            text=True,
            timeout=1800,
            check=False,
        )
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_launch_into_orbit(self):
        self.run_example("launch_into_orbit.py")
        orbit = self.ksa.active_vehicle.orbit
        body = orbit.body
        self.assertGreater(
            orbit.periapsis_altitude, body.atmosphere_radius - body.radius
        )
