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
        module = next(m for m in part.modules if m.field_list)
        field = module.field_list[0]
        sensor = part.sensor
        self.assertNotEqual("", module.name)
        self.assertNotEqual("", field.name)
        self.conn.testing_tools.destroy_part(part)
        self.wait(0.5)
        # The module belongs to the part's game object, so it goes when the part does,
        # and says so rather than failing with a null reference.
        self.assertRaises(self.destroyed, getattr, module, "name")
        self.assertRaises(self.destroyed, getattr, sensor, "active")
        # A field is found on its module on each access, so it goes the same way. The
        # store has let the object go by now, so even the members that read nothing
        # raise, which is what makes the two cases indistinguishable to a client.
        self.assertRaises(self.destroyed, getattr, field, "value")
        self.assertRaises(self.destroyed, getattr, field, "name")

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

    def test_module_members_survive_a_quickload(self):
        part = self.a_part("mk1-3pod")
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        field = next(f for f in module.field_list if f.name == "controlSrcStatusText")
        event = next(e for e in module.event_list if e.name == "MakeReference")
        action = next(a for a in module.action_list if a.name == "MakeReferenceToggle")
        value = field.value
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        # A field, event or action is found by name on whichever module the object's
        # part carries now, so it reads the module the load built rather than the one
        # it replaced. Reading one is what proves it: the game rebuilt the module, so
        # anything held from before the load would be reading a replaced game state.
        self.assertEqual(value, field.value)
        self.assertEqual(module, field.module)
        self.assertTrue(event.visible)
        action.enabled = True
        self.assertTrue(action.enabled)
        action.enabled = False

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

    def test_the_store_drops_a_destroyed_part(self):
        part = self.a_part("strutConnector")
        self.assertEqual("strutConnector", part.name)
        before = self.conn.testing_tools.object_store_size
        self.conn.testing_tools.destroy_part(part)
        # The game says the part died, so the object goes without waiting for a load.
        self.wait_until(
            lambda: self.conn.testing_tools.object_store_size < before,
            message="destroyed part was not dropped from the object store",
        )
        # The server no longer has the object at all, and says so in the same terms
        # as an object it still has whose part is gone.
        self.assertRaises(self.destroyed, getattr, part, "name")

    def test_the_store_drops_a_force_on_a_destroyed_part(self):
        # A force is applied by the game's own update rather than by a client call, so
        # there is nobody to raise an error at when its part goes; it is dropped instead.
        part = self.a_part("longAntenna")
        force = part.add_force((1, 2, 3), (0, 0, 0), part.reference_frame)
        self.assertEqual((1, 2, 3), force.force_vector)
        before = self.conn.testing_tools.object_store_size
        self.conn.testing_tools.destroy_part(part)
        self.wait_until(
            lambda: self.conn.testing_tools.object_store_size < before,
            message="the force on the destroyed part was not dropped from the store",
        )
        self.assertRaises(self.destroyed, getattr, force, "force_vector")

    def test_removing_a_force(self):
        # The command pod, which nothing here destroys: the tests run in name order and
        # the thermometer and the strut have both been destroyed by this point.
        part = self.a_part("mk1-3pod")
        force = part.add_force((1, 2, 3), (0, 0, 0), part.reference_frame)
        self.assertEqual((1, 2, 3), force.force_vector)
        before = self.conn.testing_tools.object_store_size
        force.remove()
        # Nothing applies the force again, so the object stands for nothing as soon as
        # it is taken off the part, rather than once the part itself goes.
        self.assertRaises(self.destroyed, getattr, force, "force_vector")
        self.assertRaises(self.destroyed, force.remove)
        # Taking a force off a part destroys nothing the game raises an event for, so
        # the removal is what has to get the object out of the store.
        self.wait_until(
            lambda: self.conn.testing_tools.object_store_size < before,
            message="the removed force was not dropped from the object store",
        )

    def test_the_store_drops_dead_objects_on_a_load(self):
        # A maneuver node is what a load definitively kills. The game builds the vessel's
        # parts again under the ids their objects name them by, so those survive a load
        # and show nothing, but it offers nothing to find a node by and the flight plan it
        # loads is a new one. Nothing raises a destruction event for a node either, so the
        # sweep at the load boundary is the only thing that can drop the object.
        vessel = self.space_center.active_vessel
        node = vessel.control.add_node(self.space_center.ut + 60, 100, 0, 0)
        self.assertAlmostEqual(100, node.prograde, places=3)
        before = self.conn.testing_tools.object_store_size
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        self.assertEqual("Parts", vessel.name)
        self.wait_until(
            lambda: self.conn.testing_tools.object_store_size < before,
            message="the load did not drop the maneuver node from the object store",
        )
        self.assertRaises(self.destroyed, getattr, node, "prograde")

    def test_an_orbit_survives_a_quickload(self):
        vessel = self.space_center.active_vessel
        orbit = vessel.orbit
        self.assertEqual("Kerbin", orbit.body.name)
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        # The game built a new orbit along with the vessel it rebuilt. The object names
        # the vessel whose orbit it is rather than the orbit it was made from, so it
        # reads the new one, and asking the vessel again gives back the same object
        # rather than a second one for the orbit that replaced it.
        self.assertEqual("Kerbin", orbit.body.name)
        self.assertEqual(vessel.orbit, orbit)


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
