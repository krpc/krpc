import math

import ksatest
from test_parachute import SITE, VEHICLES

SEA_LEVEL = 101325


class TestSolidBooster(ksatest.TestCase):
    """Launches the smallest solid booster on its own. With no control module, it fires
    once activated."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        name = cls.conn.testing_tools.register_vehicle(
            str(VEHICLES / "SolidBoosterTest")
        )
        cls.vehicle = cls.ksa.launch_vehicle(name, SITE)
        cls.engine = cls.vehicle.root_part.engine

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_booster(self):
        engine = self.engine
        self.assertTrue(engine.solid)
        self.assertFalse(engine.active)
        grain = engine.grain_mass
        self.assertGreater(grain, 0)
        self.assertEqual(0, engine.chamber_pressure)
        self.assertEqual(0, engine.thrust)
        self.assertEqual(0, engine.available_thrust_at(0))
        self.assertGreater(engine.max_vacuum_thrust, 0)
        self.assertGreater(engine.max_thrust, 0)
        self.assertLess(engine.max_thrust, engine.max_vacuum_thrust)

        engine.active = True
        ksatest.wait_until(lambda: engine.thrust > 0)
        self.assertGreater(engine.chamber_pressure, SEA_LEVEL)
        self.assertGreater(engine.available_thrust_at(0), 0)
        self.assertGreater(
            engine.available_thrust_at(0), engine.available_thrust_at(SEA_LEVEL)
        )
        self.assertGreater(
            engine.specific_impulse_at(0), engine.specific_impulse_at(SEA_LEVEL)
        )
        ksatest.wait_until(lambda: engine.grain_mass < grain)


class TestEnginePerformance(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle

    def test_liquid_engines(self):
        engines = [p.engine for p in self.vehicle.parts if p.engine is not None]
        self.assertTrue(engines)
        for engine in engines:
            self.assertFalse(engine.solid)
            with self.assertRaises(RuntimeError):
                _ = engine.grain_mass
            self.assertEqual(0, engine.chamber_pressure)
            self.assertGreater(engine.max_vacuum_thrust, 0)
            # In orbit, the current pressure is a vacuum
            self.assertAlmostEqual(
                engine.max_vacuum_thrust, engine.max_thrust, delta=1e-3
            )
            if engine.active and engine.has_propellant:
                self.assertAlmostEqual(
                    engine.available_thrust, engine.available_thrust_at(0), delta=1
                )
                self.assertAlmostEqual(
                    engine.specific_impulse, engine.specific_impulse_at(0), delta=0.1
                )
            with self.assertRaises(ValueError):
                engine.available_thrust_at(-1)


class TestMassProperties(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.control = cls.vehicle.control

    def tearDown(self):
        self.control.pitch = 0

    def test_dry_mass(self):
        self.assertGreater(self.vehicle.dry_mass, 0)
        self.assertLess(self.vehicle.dry_mass, self.vehicle.mass)
        self.assertAlmostEqual(
            sum(part.dry_mass for part in self.vehicle.parts),
            self.vehicle.dry_mass,
            delta=1,
        )

    def test_inertia(self):
        tensor = self.vehicle.inertia_tensor
        self.assertEqual(3, len(tensor))
        for i in range(3):
            self.assertGreater(tensor[i][i], 0)
            for j in range(3):
                self.assertAlmostEqual(
                    tensor[i][j], tensor[j][i], delta=tensor[0][0] * 1e-6
                )
        self.assertEqual(
            (tensor[0][0], tensor[1][1], tensor[2][2]), self.vehicle.moment_of_inertia
        )

    def test_coasting(self):
        frame = self.vehicle.reference_frame
        acceleration = self.vehicle.acceleration(frame)
        self.assertLess(math.sqrt(sum(x * x for x in acceleration)), 1e-3)
        angular = self.vehicle.angular_acceleration(frame)
        self.assertLess(math.sqrt(sum(x * x for x in angular)), 1e-3)

    def test_rcs_torque(self):
        # Pitching with RCS turns the vehicle at the RCS torque over its moment of inertia
        self.control.stabilization = False
        self.control.rcs = True
        torque = self.vehicle.available_rcs_torque
        self.assertGreater(torque[1], 0)
        expected = torque[1] / self.vehicle.moment_of_inertia[1]
        frame = self.vehicle.reference_frame
        self.control.pitch = 1
        ksatest.wait_until(
            lambda: self.vehicle.angular_acceleration(frame)[1] > expected / 2
        )
        self.assertAlmostEqual(
            expected,
            self.vehicle.angular_acceleration(frame)[1],
            delta=expected * 0.3,
        )
