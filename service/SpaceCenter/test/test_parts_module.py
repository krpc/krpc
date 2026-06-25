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

    def test_command_module(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        self.assertEqual("ModuleCommand", module.name)
        self.assertEqual(part, module.part)
        self.assertEqual(
            {
                "Command State": "Operational",
                "Comm First Hop Dist": "NA",
                "Comm Signal": "NA",
            },
            module.fields,
        )
        self.assertTrue(module.has_field("Command State"))
        self.assertFalse(module.has_field("DoesntExist"))
        self.assertEqual("Operational", module.get_field("Command State"))
        self.assertRaises(RuntimeError, module.get_field, "DoesntExist")
        self.assertCountEqual(
            ["Control From Here", "Rename Vessel", "Control Point: Default"],
            module.events,
        )
        self.assertTrue(module.has_event("Control From Here"))
        self.assertFalse(module.has_event("DoesntExist"))
        module.trigger_event("Control From Here")
        self.assertRaises(RuntimeError, module.trigger_event, "DoesntExist")
        self.assertEqual(["Control From Here", "Toggle Hibernation"], module.actions)
        self.assertFalse(module.has_action("DoesntExist"))
        self.assertRaises(RuntimeError, module.set_action, "DoesntExist", True)
        self.assertRaises(RuntimeError, module.set_action, "DoesntExist", False)

    def test_solar_panel(self):
        part = self.parts.with_name("solarPanels2")[0]
        module = next(m for m in part.modules if m.name == "ModuleDeployableSolarPanel")
        self.assertEqual("ModuleDeployableSolarPanel", module.name)
        self.assertEqual(part, module.part)
        self.assertEqual(
            {"Energy Flow": "0", "Status": "Retracted", "Sun Exposure": "0"},
            module.fields,
        )
        self.assertTrue(module.has_field("Status"))
        self.assertFalse(module.has_field("DoesntExist"))
        self.assertEqual("Retracted", module.get_field("Status"))
        self.assertRaises(RuntimeError, module.get_field, "DoesntExist")
        self.assertCountEqual(["Extend Solar Panel"], module.events)
        self.assertTrue(module.has_event("Extend Solar Panel"))
        self.assertFalse(module.has_event("DoesntExist"))
        self.assertRaises(RuntimeError, module.trigger_event, "DoesntExist")
        self.assertCountEqual(
            ["Extend Solar Panel", "Retract Solar Panel", "Toggle Solar Panel"],
            module.actions,
        )
        self.assertFalse(module.has_action("DoesntExist"))
        self.assertRaises(RuntimeError, module.set_action, "DoesntExist", True)
        self.assertRaises(RuntimeError, module.set_action, "DoesntExist", False)

    def test_set_field_int(self):
        part = self.parts.with_name("SmallGearBay")[0]
        module = next(m for m in part.modules if m.name == "ModuleWheelBrakes")
        self.assertEqual({"Brakes": "100"}, module.fields)
        module.set_field_float("Brakes", 50)
        self.wait(1)
        self.assertEqual({"Brakes": "50"}, module.fields)
        module.set_field_float("Brakes", 100)
        self.assertEqual({"Brakes": "100"}, module.fields)

    def test_all_fields_by_id(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        all_fields = module.all_fields_by_id
        # Hidden fields, not shown in the right-click menu, are included.
        self.assertEqual("1", all_fields["minimumCrew"])
        self.assertNotIn("minimumCrew", module.fields_by_id)
        # The visible fields are a subset of all fields, with matching values.
        for identifier, value in module.fields_by_id.items():
            self.assertEqual(value, all_fields[identifier])

    def test_get_field_by_id_fallback(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        # minimumCrew is hidden (not in the right-click menu), so the by-id
        # lookups reach it only via the fallback to all fields.
        self.assertNotIn("minimumCrew", module.fields_by_id)
        self.assertTrue(module.has_field_with_id("minimumCrew"))
        self.assertEqual("1", module.get_field_by_id("minimumCrew"))
        # Visible fields are still reachable by id; the fallback only applies
        # when there is no visible match, so existing behavior is unchanged.
        for identifier, value in module.fields_by_id.items():
            self.assertEqual(value, module.get_field_by_id(identifier))

    def test_set_and_reset_field_by_id_fallback(self):
        part = self.parts.with_name("mk1-3pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        # minimumCrew is hidden, so set/reset-by-id reach it via the fallback.
        self.assertNotIn("minimumCrew", module.fields_by_id)
        # reset_field_by_id cannot restore a hidden field (see below), so restore
        # the original value on cleanup with an explicit set instead.
        self.addCleanup(module.set_field_int_by_id, "minimumCrew", 1)
        self.assertEqual("1", module.get_field_by_id("minimumCrew"))
        module.set_field_int_by_id("minimumCrew", 2)
        self.wait(1)
        self.assertEqual("2", module.get_field_by_id("minimumCrew"))
        # reset_field_by_id reaches the field via the same fallback (it raised
        # before this change), but KSP only snapshots a field's original value
        # when its GUI control is created, so reset is a no-op for hidden fields
        # and we don't assert the resulting value.
        module.reset_field_by_id("minimumCrew")
        # Restore the original value with an explicit set.
        module.set_field_int_by_id("minimumCrew", 1)
        self.wait(1)
        self.assertEqual("1", module.get_field_by_id("minimumCrew"))

    def test_events(self):
        part = self.parts.with_name("spotLight1")[0]
        module = next(m for m in part.modules if m.name == "ModuleLight")
        self.assertTrue(module.has_event("Lights On"))
        self.assertFalse(module.has_event("Lights Off"))
        module.trigger_event("Lights On")
        self.wait()
        self.assertFalse(module.has_event("Lights On"))
        self.assertTrue(module.has_event("Lights Off"))
        module.trigger_event("Lights Off")
        self.wait()
        self.assertTrue(module.has_event("Lights On"))
        self.assertFalse(module.has_event("Lights Off"))

    def test_actions(self):
        part = self.parts.with_name("spotLight1")[0]
        module = next(m for m in part.modules if m.name == "ModuleLight")
        self.assertTrue(module.has_event("Lights On"))
        self.assertFalse(module.has_event("Lights Off"))
        module.set_action("Toggle Light", True)
        self.wait()
        self.assertFalse(module.has_event("Lights On"))
        self.assertTrue(module.has_event("Lights Off"))
        module.set_action("Toggle Light", False)
        self.wait()
        self.assertTrue(module.has_event("Lights On"))
        self.assertFalse(module.has_event("Lights Off"))

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
