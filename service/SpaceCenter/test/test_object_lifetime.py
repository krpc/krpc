import krpctest


class TestObjectLifetime(krpctest.TestCase):
    """What a client's objects do when the game objects behind them go away.

    A client object names something in the game and looks it up on each access, so
    the interesting cases are the ones where that lookup fails: the thing was
    destroyed, or a game state was loaded on top of it.
    """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Parts")
        cls.remove_other_vessels()
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException

    @classmethod
    def a_part(cls, name):
        return cls.space_center.active_vessel.parts.with_name(name)[0]

    def test_reading_a_destroyed_part(self):
        part = self.a_part("strutConnector")
        self.assertEqual("strutConnector", part.name)
        self.conn.testing_tools.destroy_part(part)
        self.wait(0.5)
        self.assertRaises(self.destroyed, getattr, part, "name")
        self.assertRaises(self.destroyed, getattr, part, "mass")

    def test_reading_a_module_of_a_destroyed_part(self):
        part = self.a_part("sensorThermometer")
        module = part.modules[0]
        sensor = part.sensor
        self.assertNotEqual("", module.name)
        self.conn.testing_tools.destroy_part(part)
        self.wait(0.5)
        # The module belongs to the part's game object, so it goes when the part does,
        # and says so rather than failing with a null reference.
        self.assertRaises(self.destroyed, getattr, module, "name")
        self.assertRaises(self.destroyed, getattr, sensor, "active")

    def test_modules_survive_a_quickload(self):
        part = self.a_part("longAntenna")
        module = part.modules[0]
        antenna = part.antenna
        name = module.name
        power = antenna.power
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        # The game built new part modules, and the objects the client holds name the
        # same ones, so they read the new modules rather than the replaced ones.
        self.assertEqual(name, module.name)
        self.assertEqual(power, antenna.power)

    def test_modules_survive_a_load_of_a_save_that_predates_them(self):
        # The game names a part module only when something asks it to, and writes out only
        # the names it has already given, so a save taken before the client named a module
        # holds nothing that says which module was meant. Loading one still has to give the
        # client back the module it named.
        self.space_center.quicksave()
        self.wait(1)
        part = self.a_part("longAntenna")
        module = part.modules[0]
        antenna = part.antenna
        name = module.name
        power = antenna.power
        self.space_center.quickload()
        self.wait(1)
        self.assertEqual(name, module.name)
        self.assertEqual(power, antenna.power)

    def test_objects_survive_a_quickload(self):
        vessel = self.space_center.active_vessel
        part = self.a_part("sensorThermometer")
        name = vessel.name
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        # The game built a new vessel and new parts, and the objects the client
        # already holds name the same things, so they find them again.
        self.assertEqual(name, vessel.name)
        self.assertEqual("sensorThermometer", part.name)


class TestObjectLifetimeUnloaded(krpctest.TestCase):
    """A part of a vessel the game has unloaded is not a destroyed part.

    The game keeps an unloaded vessel as a snapshot and builds its parts again when it
    comes back into range, so a part object for one of them has to say the part is not
    loaded, and has to survive being swept for as long as the snapshot is there.
    """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException
        next(iter(cls.space_center.active_vessel.parts.docking_ports)).undock()
        cls.wait(1)
        cls.vessel = cls.space_center.active_vessel
        cls.other = next(v for v in cls.space_center.vessels if v != cls.vessel)

    def assert_not_loaded(self, obj, attribute):
        """Assert that reading the attribute says the object is not loaded.

        Not loaded and destroyed have to be told apart, and asserting on RuntimeError
        cannot do it: the client builds ObjectDestroyedException as a subclass of
        RuntimeError, and the not-loaded error is a RuntimeError itself, so either one
        satisfies an assertion made against the base class.
        """
        with self.assertRaises(RuntimeError) as raised:
            getattr(obj, attribute)
        self.assertNotIsInstance(raised.exception, self.destroyed)
        self.assertIn("not loaded", str(raised.exception))

    def test_parts_of_an_unloaded_vessel(self):
        part = self.other.parts.all[0]
        name = part.name
        other_name = self.other.name

        # Put the other vessel far enough away that the game unloads it.
        self.set_circular_orbit("Kerbin", 200000)
        self.wait(2)
        self.wait_while(lambda: self.other.loaded, message="vessel did not unload")

        # The vessel is still there to be found, and the part is not, but it is not
        # gone either: it is waiting for its vessel to be loaded again.
        self.assertEqual(other_name, self.other.name)
        self.assert_not_loaded(part, "name")

        # A load boundary sweeps the store, and must not take the part with it.
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        self.assert_not_loaded(part, "name")

        # Loading the vessel gives the part back, rather than a new object for it.
        self.space_center.active_vessel = self.other
        self.wait(2)
        self.assertEqual(name, part.name)


class TestObjectLifetimeDestroyedVessel(krpctest.TestCase):
    """A vessel the game destroys takes with it every object reached through it."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Multi")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException

    def test_using_a_destroyed_vessel(self):
        vessel = self.space_center.active_vessel
        next(iter(vessel.parts.docking_ports)).undock()
        self.wait(1)
        other = next(v for v in self.space_center.vessels if v != vessel)
        control = other.control
        part = other.parts.all[0]
        self.assertNotEqual("", other.name)

        # The game destroys the vessel, which is the end of it: it was not merely put out
        # of range, and there is nothing left to build it again from.
        self.remove_other_vessels()
        self.wait(1)
        self.assertRaises(self.destroyed, getattr, other, "name")
        self.assertRaises(self.destroyed, getattr, control, "sas")
        self.assertRaises(self.destroyed, getattr, part, "name")
