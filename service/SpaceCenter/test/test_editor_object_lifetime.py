import krpctest


class EditorObjectLifetimeTest(krpctest.TestCase):
    """Shared setup for the editor object lifetime tests.

    A vessel is launched before the editor is entered so that the tests that leave
    the editor are checked against a game that has something in flight, which is what
    decides whether the object store is swept once the editor has been left.
    """

    craft = "Parts"
    other_craft = "Basic"

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.enter_editor("VAB", craft=cls.craft)
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center
        cls.editor = cls.space_center.editor
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    def a_part(self, name):
        return self.editor.vessel.parts.with_name(name)[0]

    def swept(self, before):
        """Wait for the object store to drop something, and fail if it does not.

        The sweep runs on the game's own schedule rather than on demand, so a test
        that needs one to have happened has to wait for the store to shrink.
        """
        self.wait_until(
            lambda: self.conn.testing_tools.object_store_size < before,
            message="the object store was not swept",
        )


class TestEditorObjectLifetime(EditorObjectLifetimeTest):
    """What a client's editor objects do as the vessel under construction changes.

    A part in the editor is named by its craft id, which is only unique within one
    vessel: a craft file saved from another carries its craft ids over, so two
    vessels routinely give the same id to different parts. A part object therefore
    also records which loaded vessel it was taken from, and a part of any earlier one
    is gone rather than whatever answers to its id now.

    The editor holds exactly one vessel, so a part that is not in it is gone rather
    than merely out of reach, as a part of an unloaded vessel in flight would be.
    Loading another craft is what makes a part leave the vessel here: the game's own
    delete refreshes the editor's attach node icons before it removes anything, which
    fails when the call did not come from the editor UI, so it never gets as far as
    deleting the part.
    """

    def setUp(self):
        # Tests here replace the vessel, so each starts from the craft the class was
        # entered with.
        self.editor.load_vessel("VAB", self.craft)

    def test_a_part_survives_an_undo(self):
        # An undo hands back a vessel object of its own, built from the game's backup,
        # so the vessel the editor has is a different object afterwards. The parts it
        # builds carry the craft ids they had, so a part the undo did not touch is the
        # same part and its object goes on working. Only the vessel being replaced by
        # one loaded as a new vessel makes a part of the old one gone.
        part = self.a_part("sensorThermometer")
        module = part.modules[0]
        name = part.name
        module_name = module.name

        # Two states with no change between them, so the undo restores a vessel with
        # the same parts; nothing here can add or remove one to be affected by it.
        self.conn.testing_tools.editor_set_backup()
        self.wait(0.5)
        self.conn.testing_tools.editor_set_backup()
        self.wait(0.5)
        self.conn.testing_tools.editor_undo()
        self.wait(1)

        self.assertEqual(name, part.name)
        self.assertEqual(module_name, module.name)
        # And the sweep that the undo asks for must not have taken them either.
        self.assertGreater(len(self.editor.vessel.parts.all), 0)

    def test_a_part_of_a_reloaded_vessel_is_destroyed(self):
        part = self.a_part("sensorThermometer")
        module = part.modules[0]

        self.editor.load_vessel("VAB", self.craft)

        # Loading a craft starts a new vessel, whether or not it is the one the editor
        # already had. Nothing tells the two apart: a craft file saved from this one
        # would carry the same craft ids, so a part that answered to the old id could
        # be a different part. Rather than guess, the old vessel's parts are gone.
        self.assertRaises(self.destroyed, getattr, part, "name")
        self.assertRaises(self.destroyed, getattr, module, "name")

    def test_a_part_of_a_replaced_vessel_is_destroyed(self):
        part = self.a_part("liquidEngineMainsail.v2")
        module = part.modules[0]
        # A typed part-module object as well as the generic one. Only the members of
        # these that describe the design are available in the editor, so the object is
        # asked for one of those rather than for anything a vessel in flight has.
        engine = part.engine
        self.assertEqual("liquidEngineMainsail.v2", part.name)
        self.assertGreater(engine.thrust_limit, 0)

        self.editor.load_vessel("VAB", self.other_craft)

        # The part is not in the vessel any more, and the editor has no other vessel
        # it could be in, so it is gone rather than waiting to be loaded.
        self.assertRaises(self.destroyed, getattr, part, "name")
        self.assertRaises(self.destroyed, getattr, module, "name")
        self.assertRaises(self.destroyed, getattr, engine, "thrust_limit")

    def test_the_store_drops_a_part_of_a_replaced_vessel(self):
        part = self.a_part("liquidEngineMainsail.v2")
        self.assertEqual("liquidEngineMainsail.v2", part.name)
        before = self.conn.testing_tools.object_store_size

        self.editor.load_vessel("VAB", self.other_craft)
        self.swept(before)

        # The server no longer has the object at all, and says so in the same terms
        # as an object it still has whose part is gone.
        self.assertRaises(self.destroyed, getattr, part, "name")

    def test_the_vessels_objects_outlive_a_sweep(self):
        # Everything named against the vessel in the editor rather than against a
        # vessel in flight. None of it has a vessel id, so a sweep that judged it by
        # one would find no such vessel and drop the lot.
        parts = self.editor.vessel.parts
        resources = self.editor.vessel.resources
        stages = self.editor.vessel.decouple_stages
        self.assertGreater(len(stages), 0)
        stage = stages[0]

        # Replacing the vessel is what makes the sweep run and find something to
        # drop, so the assertions below are made against a store that has been swept.
        doomed = self.a_part("liquidEngineMainsail.v2")
        before = self.conn.testing_tools.object_store_size
        self.editor.load_vessel("VAB", self.other_craft)
        self.swept(before)
        self.assertRaises(self.destroyed, getattr, doomed, "name")

        # The editor still has a vessel, so everything named against it still works,
        # and reports the vessel that is there now.
        self.assertGreater(len(parts.all), 0)
        self.assertGreater(len(resources.all), 0)
        self.assertGreater(len(stage.parts), 0)


class TestEditorObjectLifetimeOnLeaving(EditorObjectLifetimeTest):
    """The editor takes its vessel with it, and everything named against it."""

    @classmethod
    def tearDownClass(cls):
        pass

    def test_leaving_the_editor_destroys_the_vessels_objects(self):
        parts = self.editor.vessel.parts
        part = self.a_part("mk1-3pod")
        self.assertEqual("mk1-3pod", part.name)
        before = self.conn.testing_tools.object_store_size

        self.leave_editor()
        self.swept(before)

        # There is no vessel in an editor that is not open, and nothing will build
        # one again, so the objects are gone rather than dormant.
        self.assertRaises(self.destroyed, getattr, part, "name")
        self.assertRaises(self.destroyed, getattr, parts, "all")


class TestEditorPartIdCollision(EditorObjectLifetimeTest):
    """A part is not confused with the part of another vessel that shares its id.

    ActionGroups.craft was saved from Basic.craft, so it carries every one of its
    craft ids while being a vessel of its own, which is what any craft saved from
    another looks like. Resolving by craft id alone hands back the part of the new
    vessel that answers to the old id, with no error and nothing to say the answer
    came from a vessel the client never asked about.
    """

    craft = "Basic"
    other_craft = "ActionGroups"

    def test_a_part_is_not_confused_with_one_of_another_vessel(self):
        part = self.a_part("mk1pod.v2")
        self.assertEqual("mk1pod.v2", part.name)

        self.editor.load_vessel("VAB", self.other_craft)

        # The new vessel has a part with exactly this craft id, and it is not this part.
        self.assertRaises(self.destroyed, getattr, part, "name")
        # The vessel that is there now is readable, through parts obtained from it.
        self.assertEqual("mk1pod.v2", self.a_part("mk1pod.v2").name)
