import unittest

import krpctest


class TestPartsModule(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts")
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    @staticmethod
    def _visible_event(module, gui_name):
        """Return the event with the given gui name that is currently shown in
        the part's right-click menu (visible and active), or None. Replaces the
        deprecated Module.has_event/trigger_event lookups."""
        return next(
            (
                event
                for event in module.event_list
                if event.gui_name == gui_name and event.visible and event.active
            ),
            None,
        )

    def test_command_module(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        self.assertEqual("ModuleCommand", module.name)
        self.assertEqual(part, module.part)
        self.assertEqual(
            {
                "controlSrcStatusText": "Operational",
                "commNetSignal": "NA",
                "commNetFirstHopDistance": "NA",
            },
            {field.name: field.value for field in module.field_list if field.visible},
        )
        self.assertEqual(
            {"MakeReference", "RenameVessel", "ChangeControlPoint"},
            {event.name for event in module.event_list if event.visible},
        )
        self.assertEqual(
            {"MakeReferenceToggle", "HibernateToggle"},
            {action.name for action in module.action_list},
        )

        # Trigger an event
        event = None
        for candidate in module.event_list:
            if candidate.name == "MakeReference":
                event = candidate
        event.trigger()

        # Set an action
        action = None
        for candidate in module.action_list:
            if candidate.name == "MakeReferenceToggle":
                action = candidate
        action.set(True)
        action.set(False)

    def test_solar_panel(self):
        part = self.parts.with_name("solarPanels2")[0]
        module = next(m for m in part.modules if m.name == "ModuleDeployableSolarPanel")
        self.assertEqual("ModuleDeployableSolarPanel", module.name)
        self.assertEqual(part, module.part)
        self.assertEqual(
            {"sunAOA": "0", "flowRate": "0", "status": "Retracted"},
            {field.name: field.value for field in module.field_list if field.visible},
        )
        self.assertEqual(
            {"Extend", "Retract"},
            {event.name for event in module.event_list if event.visible},
        )
        self.assertEqual(
            {"ExtendAction", "RetractAction", "ExtendPanelsAction"},
            {action.name for action in module.action_list},
        )

    def test_set_and_reset_hidden_field(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        minimum_crew = next(f for f in module.field_list if f.name == "minimumCrew")
        self.assertFalse(minimum_crew.visible)
        self.addCleanup(setattr, minimum_crew, "int_value", 1)
        self.assertEqual(1, minimum_crew.int_value)
        minimum_crew.int_value = 2
        self.wait(1)
        self.assertEqual(2, minimum_crew.int_value)
        # reset() restores the value the field had when the part was loaded. KSP
        # snapshots original values for all fields, hidden included, so reset
        # works on a hidden field too.
        minimum_crew.reset()
        self.wait(1)
        self.assertEqual(1, minimum_crew.int_value)

    def test_field_list(self):
        field_type = self.connect().space_center.FieldType
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")

        field_list = module.field_list
        self.assertGreater(len(field_list), 0)

        # Every field refers back to its module (exercises Equals), and fetching
        # the list twice yields equal field objects.
        for field in field_list:
            self.assertEqual(module, field.module)
        self.assertEqual(module.field_list[0], module.field_list[0])

        # A visible field carries the right metadata and value.
        command_state = next(f for f in field_list if f.gui_name == "Command State")
        self.assertEqual("Command State", command_state.gui_name)
        self.assertTrue(command_state.visible)
        self.assertEqual("Operational", command_state.value)

        # A hidden integer field (not shown in the right-click menu) is included,
        # with its type and typed value.
        minimum_crew = next(f for f in field_list if f.name == "minimumCrew")
        self.assertFalse(minimum_crew.visible)
        self.assertEqual(field_type.integer, minimum_crew.type)
        self.assertEqual(1, minimum_crew.int_value)
        self.assertEqual("1", minimum_crew.value)
        # A typed getter of the wrong type raises.
        self.assertRaises(RuntimeError, lambda: minimum_crew.bool_value)

    def test_field_list_set_and_reset(self):
        field_type = self.connect().space_center.FieldType
        part = self.parts.with_name("SmallGearBay")[0]
        module = next(m for m in part.modules if m.name == "ModuleWheelBrakes")
        brakes = next(f for f in module.field_list if f.gui_name == "Brakes")
        self.addCleanup(setattr, brakes, "float_value", 100)

        self.assertEqual(field_type.float, brakes.type)
        self.assertEqual(100, brakes.float_value)

        brakes.float_value = 50
        self.wait(1)
        self.assertEqual(50, brakes.float_value)
        self.assertEqual("50", brakes.value)

        # reset() restores the value the field had when the part was loaded.
        brakes.reset()
        self.wait(1)
        self.assertEqual(100, brakes.float_value)

    def test_event_list(self):
        part = self.parts.with_name("spotLight1")[0]
        module = next(m for m in part.modules if m.name == "ModuleLight")

        event_list = module.event_list
        self.assertGreater(len(event_list), 0)
        for event in event_list:
            self.assertEqual(module, event.module)

        # Only "Lights On" is shown in the menu (visible and active) initially.
        self.assertCountEqual(
            ["Lights On"],
            [e.gui_name for e in event_list if e.visible and e.active],
        )

        lights_on = next(e for e in event_list if e.gui_name == "Lights On")
        self.assertTrue(lights_on.visible)
        self.assertTrue(lights_on.active)

        lights_on.trigger()
        self.wait()
        # Triggering flips which events are shown in the menu.
        self.assertIn(
            "Lights Off",
            [e.gui_name for e in module.event_list if e.visible and e.active],
        )

        next(e for e in module.event_list if e.gui_name == "Lights Off").trigger()
        self.wait()
        self.assertIn(
            "Lights On",
            [e.gui_name for e in module.event_list if e.visible and e.active],
        )

    def test_action_list(self):
        part = self.parts.with_name("spotLight1")[0]
        module = next(m for m in part.modules if m.name == "ModuleLight")

        action_list = module.action_list
        self.assertGreater(len(action_list), 0)
        for action in action_list:
            self.assertEqual(module, action.module)

        self.assertCountEqual(
            ["Toggle Light", "Turn Light On", "Turn Light Off"],
            [a.gui_name for a in action_list],
        )

        toggle = next(a for a in action_list if a.gui_name == "Toggle Light")
        self.assertIsNotNone(self._visible_event(module, "Lights On"))
        toggle.set(True)
        self.wait()
        self.assertIsNotNone(self._visible_event(module, "Lights Off"))
        toggle.set(False)
        self.wait()
        self.assertIsNotNone(self._visible_event(module, "Lights On"))

    def test_config_node(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")

        # Module.config returns the module's MODULE node from the part's cfg file.
        config = module.config
        self.assertIsNotNone(config)
        self.assertEqual("MODULE", config.name)
        self.assertEqual("ModuleCommand", config.get_value("name"))
        self.assertTrue(config.has_value("name"))
        self.assertFalse(config.has_value("DoesntExist"))
        self.assertRaises(RuntimeError, config.get_value, "DoesntExist")
        self.assertEqual("ModuleCommand", config.values["name"])
        self.assertEqual(["ModuleCommand"], config.get_values("name"))

        # Part.config returns the whole part cfg, including module and resource nodes.
        part_config = part.config
        self.assertIsNotNone(part_config)
        self.assertTrue(part_config.has_node("MODULE"))
        module_names = [n.get_value("name") for n in part_config.get_nodes("MODULE")]
        self.assertIn("ModuleCommand", module_names)
        self.assertIn("MODULE", [n.name for n in part_config.nodes])
        self.assertFalse(part_config.has_node("DoesntExist"))
        self.assertRaises(RuntimeError, part_config.get_node, "DoesntExist")

        # Static data not exposed as a field: the electric charge stored by the pod.
        electric_charge = next(
            r
            for r in part_config.get_nodes("RESOURCE")
            if r.get_value("name") == "ElectricCharge"
        )
        self.assertGreater(float(electric_charge.get_value("amount")), 0)

        # The static rate of a generator (issue #831): the electric charge it
        # produces is in an OUTPUT_RESOURCE node of its config, not in a field.
        generator = next(
            (
                m
                for p in self.parts.all
                for m in p.modules
                if m.name == "ModuleGenerator"
            ),
            None,
        )
        self.assertIsNotNone(generator)
        gen_config = generator.config
        self.assertIsNotNone(gen_config)
        self.assertEqual("ModuleGenerator", gen_config.get_value("name"))
        output = next(
            (
                n
                for n in gen_config.get_nodes("OUTPUT_RESOURCE")
                if n.get_value("name") == "ElectricCharge"
            ),
            None,
        )
        self.assertIsNotNone(output)
        self.assertGreater(float(output.get_value("rate")), 0)


if __name__ == "__main__":
    unittest.main()
