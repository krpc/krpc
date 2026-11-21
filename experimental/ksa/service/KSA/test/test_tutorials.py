import subprocess
import sys

import ksatest
import pytest

TUTORIALS = ksatest.ROOT / "doc" / "src" / "scripts" / "tutorials"


@pytest.mark.slow
class TestTutorials(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def run_tutorial(self, path):
        result = subprocess.run(
            [sys.executable, str(TUTORIALS / path)],
            env=ksatest.python_env(),
            capture_output=True,
            text=True,
            timeout=1800,
            check=False,
        )
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_launch_into_orbit(self):
        self.run_tutorial("launch-into-orbit/launch_into_orbit.py")
        orbit = self.ksa.active_vehicle.orbit
        body = orbit.body
        self.assertGreater(
            orbit.periapsis_altitude, body.atmosphere_radius - body.radius
        )
