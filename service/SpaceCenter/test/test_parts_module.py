import unittest
import krpctest


class TestPartsModule(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts")
            cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def test_command_module(self):
        part = self.parts.with_title("Mk1-3 Command Pod")[0]
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
        part = self.parts.with_title("SP-L 1x6 Photovoltaic Panels")[0]
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
        part = self.parts.with_title("LY-10 Small Landing Gear")[0]
        module = next(m for m in part.modules if m.name == "ModuleWheelBrakes")
        self.assertEqual({"Brakes": "100"}, module.fields)
        module.set_field_int("Brakes", 50)
        self.wait(1)
        self.assertEqual({"Brakes": "50"}, module.fields)
        module.set_field_int("Brakes", 100)
        self.assertEqual({"Brakes": "100"}, module.fields)

    def test_all_fields_by_id(self):
        part = self.parts.with_title("Mk1-3 Command Pod")[0]
        module = next(m for m in part.modules if m.name == "ModuleCommand")
        all_fields = module.all_fields_by_id
        # Hidden fields, not shown in the right-click menu, are included.
        self.assertEqual("1", all_fields["minimumCrew"])
        self.assertNotIn("minimumCrew", module.fields_by_id)
        # The visible fields are a subset of all fields, with matching values.
        for identifier, value in module.fields_by_id.items():
            self.assertEqual(value, all_fields[identifier])

    def test_get_field_by_id_fallback(self):
        part = self.parts.with_title("Mk1-3 Command Pod")[0]
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
        part = self.parts.with_title("Mk1-3 Command Pod")[0]
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
        part = self.parts.with_title("Illuminator Mk1")[0]
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
        part = self.parts.with_title("Illuminator Mk1")[0]
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


if __name__ == "__main__":
    unittest.main()
