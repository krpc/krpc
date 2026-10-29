import math

import ksatest


class TestPart(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.parts = cls.vehicle.parts

    def engine_parts(self):
        return [part for part in self.parts if part.engine is not None]

    def test_tree(self):
        root = self.vehicle.root_part
        self.assertIn(root, self.parts)
        self.assertIsNone(root.parent)
        for part in self.parts:
            self.assertEqual(self.vehicle, part.vehicle)
            for child in part.children:
                self.assertEqual(part, child.parent)
        # Every part is reachable from the root
        reached = []
        pending = [root]
        while pending:
            part = pending.pop()
            reached.append(part)
            pending.extend(part.children)
        self.assertEqual(len(self.parts), len(reached))

    def test_names(self):
        for part in self.parts:
            self.assertTrue(part.name)
            self.assertTrue(part.title)

    def test_mass(self):
        self.assertAlmostEqual(
            self.vehicle.mass,
            sum(part.mass for part in self.parts),
            delta=self.vehicle.mass * 1e-4,
        )
        for part in self.parts:
            self.assertGreaterEqual(part.mass, part.dry_mass)

    def test_resources(self):
        resources = self.vehicle.resources
        self.assertTrue(resources)
        self.assertAlmostEqual(
            self.vehicle.propellant_mass,
            sum(resource.amount for resource in resources),
            delta=self.vehicle.propellant_mass * 1e-4,
        )
        for resource in resources:
            self.assertGreater(resource.amount, 0)
            self.assertLessEqual(resource.amount, resource.max * (1 + 1e-5))
            in_parts = sum(
                r.amount
                for part in self.parts
                for r in part.resources
                if r.name == resource.name
            )
            self.assertAlmostEqual(resource.amount, in_parts, delta=1)

    def test_sequences(self):
        numbers = [sequence.number for sequence in self.vehicle.sequences]
        for part in self.engine_parts():
            self.assertIn(part.sequence, numbers)

    def test_position(self):
        # The vehicle frame points forward along x, so the engines are behind the capsule
        frame = self.vehicle.reference_frame
        capsule = self.vehicle.root_part.position(frame)
        for part in self.engine_parts():
            self.assertLess(part.position(frame)[0], capsule[0])

    def test_rotation(self):
        rotation = self.vehicle.root_part.rotation(self.vehicle.reference_frame)
        self.assertAlmostEqual(1, sum(x * x for x in rotation), delta=1e-6)

    def test_velocity(self):
        # The parts are fixed in the vehicle's frame, and move with the vehicle
        for part in self.parts:
            for x in part.velocity(self.vehicle.reference_frame):
                self.assertAlmostEqual(0, x, delta=1e-3)
        frame = self.ksa.bodies["Earth"].non_rotating_reference_frame
        expected = self.vehicle.velocity(frame)
        actual = self.vehicle.root_part.velocity(frame)
        for x, y in zip(expected, actual):
            self.assertAlmostEqual(x, y, delta=0.1)

    def test_parts_with_name(self):
        root = self.vehicle.root_part
        self.assertIn(root, self.vehicle.parts_with_name(root.name))
        for part in self.vehicle.parts_with_name(root.name):
            self.assertEqual(root.name, part.name)
        self.assertEqual([], self.vehicle.parts_with_name("NoSuchPart"))

    def test_module_lists(self):
        for name in ("engine", "rcs", "decoupler", "parachute", "docking_port"):
            with self.subTest(module=name):
                expected = [
                    getattr(part, name)
                    for part in self.parts
                    if getattr(part, name) is not None
                ]
                attribute = "rcs" if name == "rcs" else name + "s"
                self.assertEqual(expected, getattr(self.vehicle, attribute))
        self.assertTrue(self.vehicle.engines)
        self.assertTrue(self.vehicle.decouplers)

    def test_engines(self):
        engines = [part.engine for part in self.engine_parts()]
        self.assertTrue(engines)
        # An engine's performance updates in the solver step after it changes
        ksatest.wait_until(
            lambda: (
                abs(
                    self.vehicle.available_thrust
                    - sum(engine.available_thrust for engine in engines)
                )
                < self.vehicle.available_thrust * 1e-3
            )
        )
        for engine in engines:
            self.assertEqual(0, engine.thrust)
            if engine.available_thrust > 0:
                self.assertTrue(engine.active)
                self.assertTrue(engine.has_propellant)
                self.assertGreater(engine.specific_impulse, 0)

    def test_gimbals(self):
        gimbals = [gimbal for part in self.engine_parts() for gimbal in part.gimbals]
        self.assertTrue(gimbals)
        for gimbal in gimbals:
            self.assertIn(gimbal.part, self.parts)
            self.assertGreaterEqual(gimbal.max_angle_y, 0)
            self.assertGreaterEqual(gimbal.max_angle_z, 0)
            self.assertLessEqual(gimbal.max_angle_y, 90)
            self.assertLessEqual(gimbal.max_angle_z, 90)
            self.assertGreater(gimbal.max_angle_y + gimbal.max_angle_z, 0)
            self.assertLessEqual(abs(gimbal.angle_y), gimbal.max_angle_y + 1e-6)
            self.assertLessEqual(abs(gimbal.angle_z), gimbal.max_angle_z + 1e-6)
        # The LR91 Sea's main nozzle turns 5 degrees about both axes, in the game's data
        sea = [gimbal for gimbal in gimbals if gimbal.part.title == "LR91 Sea"]
        self.assertIn(5, [round(gimbal.max_angle_y, 3) for gimbal in sea])
        self.assertTrue(all(gimbal.active for gimbal in sea))

    def test_engine_gimbals(self):
        steered = []
        for part in self.engine_parts():
            gimbals = part.engine.gimbals
            for gimbal in gimbals:
                self.assertIn(gimbal, part.gimbals)
            steered.extend(gimbals)
        self.assertTrue(steered)

    def test_gimbal_rotation(self):
        frame = self.vehicle.reference_frame
        for part in self.engine_parts():
            for gimbal in part.gimbals:
                rotation = gimbal.rotation(frame)
                self.assertAlmostEqual(
                    1, math.sqrt(sum(x * x for x in rotation)), places=6
                )

    def test_tanks(self):
        tanks = self.vehicle.tanks
        self.assertTrue(tanks)
        for tank in tanks:
            self.assertGreater(tank.volume, 0)
            self.assertGreaterEqual(tank.filled_fraction, 0)
            self.assertLessEqual(tank.filled_fraction, 1)
            held = sum(resource.amount for resource in tank.part.resources)
            self.assertEqual(held > 0, tank.filled_fraction > 0)
            self.assertTrue(tank.propellant_use_enabled)
            self.assertEqual(self.ksa.TankTransferMode.none, tank.transfer_mode)
            self.assertIn(tank, tank.part.tanks)
        self.assertEqual(len(tanks), sum(len(part.tanks) for part in self.parts))

    def test_tank_transfer_mode(self):
        tank = self.vehicle.tanks[0]
        try:
            for mode in (self.ksa.TankTransferMode.in_, self.ksa.TankTransferMode.out):
                tank.transfer_mode = mode
                # The mode holds with the part window closed
                ksatest.wait_until(lambda end=self.ksa.ut + 1: self.ksa.ut > end)
                self.assertEqual(mode, tank.transfer_mode)
        finally:
            tank.transfer_mode = self.ksa.TankTransferMode.none
        self.assertEqual(self.ksa.TankTransferMode.none, tank.transfer_mode)

    def test_propellant_use(self):
        # With no tank to draw from, the engines stop
        tanks = self.vehicle.tanks
        control = self.vehicle.control
        throttle = control.throttle
        try:
            control.throttle = 1
            control.engine_on = True
            ksatest.wait_until(lambda: self.vehicle.thrust > 0, timeout=5)
            for tank in tanks:
                tank.propellant_use_enabled = False
            self.assertFalse(tanks[0].propellant_use_enabled)
            ksatest.wait_until(lambda: self.vehicle.thrust == 0, timeout=5)
            active = [engine for engine in self.vehicle.engines if engine.active]
            self.assertFalse(any(engine.has_propellant for engine in active))
        finally:
            for tank in tanks:
                tank.propellant_use_enabled = True
            control.engine_on = False
            control.throttle = throttle
        ksatest.wait_until(lambda: self.vehicle.thrust == 0, timeout=5)

    def test_engine_propellants(self):
        names = {resource.name for resource in self.vehicle.resources}
        for engine in self.vehicle.engines:
            self.assertGreaterEqual(engine.minimum_throttle, 0)
            self.assertLess(engine.minimum_throttle, 1)
            self.assertTrue(engine.propellants)
            self.assertEqual(len(engine.propellants), len(set(engine.propellants)))
            for name in engine.propellants:
                self.assertIn(name, names)

    def test_engine_active(self):
        engine = next(
            part.engine
            for part in self.engine_parts()
            if part.engine.available_thrust > 0
        )
        thrust = self.vehicle.available_thrust
        try:
            engine.active = False
            self.assertFalse(engine.active)
            self.assertEqual(0, engine.available_thrust)
            self.assertLess(self.vehicle.available_thrust, thrust)
        finally:
            engine.active = True
        self.assertTrue(engine.active)

    def test_rcs(self):
        rcs = next(part.rcs for part in self.parts if part.rcs is not None)
        active = rcs.active
        try:
            rcs.active = not active
            self.assertEqual(not active, rcs.active)
        finally:
            rcs.active = active
        self.assertEqual(active, rcs.active)

    def test_burn_uses_propellant(self):
        amounts = {r.name: r.amount for r in self.vehicle.resources}
        control = self.vehicle.control
        throttle = control.throttle
        try:
            control.throttle = 1
            control.engine_on = True
            ksatest.wait_until(lambda: self.vehicle.thrust > 0, timeout=5)
            engine = next(
                part.engine for part in self.engine_parts() if part.engine.thrust > 0
            )
            self.assertGreater(engine.thrust, 0)
            ksatest.wait_until(
                lambda: any(r.amount < amounts[r.name] for r in self.vehicle.resources)
            )
        finally:
            control.engine_on = False
            control.throttle = throttle
        ksatest.wait_until(lambda: self.vehicle.thrust == 0, timeout=5)

    def test_docking_port(self):
        port = next(
            part.docking_port for part in self.parts if part.docking_port is not None
        )
        self.assertFalse(port.docked)
        self.assertIsNone(port.docked_part)
        with self.assertRaises(RuntimeError):
            port.undock()


class TestDecoupler(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle

    @classmethod
    def tearDownClass(cls):
        cls.load_save()

    def test_decouple(self):
        # The lowest decoupler separates the first stage
        decouplers = [part for part in self.vehicle.parts if part.decoupler is not None]
        part = min(decouplers, key=lambda part: part.sequence)
        decoupler = part.decoupler
        self.assertTrue(decoupler.enabled)
        self.assertFalse(decoupler.decoupled)
        count = len(self.ksa.vehicles)
        parts = len(self.vehicle.parts)
        separated = decoupler.decouple()
        self.assertTrue(decoupler.decoupled)
        self.assertEqual(1, len(separated))
        self.assertEqual(count + 1, len(self.ksa.vehicles))
        self.assertIn(separated[0], self.ksa.vehicles)
        self.assertNotEqual(self.vehicle, separated[0])
        self.assertEqual(parts, len(self.vehicle.parts) + len(separated[0].parts))
        with self.assertRaises(RuntimeError):
            decoupler.decouple()
