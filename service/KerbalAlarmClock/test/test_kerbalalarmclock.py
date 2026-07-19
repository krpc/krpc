import unittest
import krpctest


class TestKerbalAlarmClock(krpctest.TestCase):
    mods = ["KerbalAlarmClock"]

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        # A vessel is only needed for the Alarm.vessel round-trip, which
        # requires a matching vessel to exist in the game.
        cls.launch_vessel_from_vab("KerbalAlarmClock")
        cls.remove_other_vessels()
        conn = cls.connect()
        cls.kac = conn.kerbal_alarm_clock
        cls.sc = conn.space_center
        cls.type = cls.kac.AlarmType
        cls.action = cls.kac.AlarmAction

    def tearDown(self):
        for alarm in list(self.kac.alarms):
            alarm.remove()

    def create(self, alarm_type=None, name="test", offset=3600):
        if alarm_type is None:
            alarm_type = self.type.raw
        return self.kac.create_alarm(alarm_type, name, self.sc.ut + offset)

    def test_available(self):
        self.assertTrue(self.kac.available)

    def test_no_alarms(self):
        self.assertCountEqual([], self.kac.alarms)
        self.assertIsNone(self.kac.alarm_with_name("missing"))
        self.assertCountEqual([], self.kac.alarms_with_type(self.type.raw))

    def test_create_alarm(self):
        ut = self.sc.ut + 3600
        alarm = self.kac.create_alarm(self.type.raw, "test_create", ut)
        self.assertEqual(self.type.raw, alarm.type)
        self.assertEqual("test_create", alarm.name)
        self.assertAlmostEqual(ut, alarm.time, delta=1)
        self.assertNotEqual("", alarm.id)
        self.assertAlmostEqual(3600, alarm.remaining, delta=60)
        self.assertIn(alarm, self.kac.alarms)

    def test_alarm_with_name(self):
        alarm = self.create(name="test_with_name")
        self.assertEqual(alarm, self.kac.alarm_with_name("test_with_name"))
        self.assertIsNone(self.kac.alarm_with_name("nonexistent"))

    def test_alarms_with_type(self):
        raw = self.create(self.type.raw, "test_raw")
        maneuver = self.create(self.type.maneuver, "test_maneuver")
        self.assertCountEqual([raw], self.kac.alarms_with_type(self.type.raw))
        self.assertCountEqual([maneuver], self.kac.alarms_with_type(self.type.maneuver))
        self.assertCountEqual([], self.kac.alarms_with_type(self.type.apoapsis))

    def test_attributes_round_trip(self):
        alarm = self.create()
        alarm.name = "renamed"
        self.assertEqual("renamed", alarm.name)
        alarm.notes = "some notes"
        self.assertEqual("some notes", alarm.notes)
        alarm.margin = 60
        self.assertAlmostEqual(60, alarm.margin)
        ut = self.sc.ut + 7200
        alarm.time = ut
        self.assertAlmostEqual(ut, alarm.time, delta=1)
        self.assertTrue(alarm.enabled)
        alarm.enabled = False
        self.assertFalse(alarm.enabled)
        alarm.enabled = True
        sound = alarm.play_sound
        alarm.play_sound = not sound
        self.assertEqual(not sound, alarm.play_sound)

    def test_repeat(self):
        # Raw alarms support repeating and a repeat period
        alarm = self.create()
        self.assertTrue(alarm.supports_repeat)
        self.assertTrue(alarm.supports_repeat_period)
        self.assertFalse(alarm.repeat)
        alarm.repeat = True
        self.assertTrue(alarm.repeat)
        alarm.repeat_period = 600
        self.assertAlmostEqual(600, alarm.repeat_period)
        # Maneuver alarms support neither
        maneuver = self.create(self.type.maneuver, "test_repeat_maneuver")
        self.assertFalse(maneuver.supports_repeat)
        self.assertFalse(maneuver.supports_repeat_period)

    def test_property_action(self):
        alarm = self.create()
        # The six preset actions round-trip
        for action in (
            self.action.do_nothing_delete_when_passed,
            self.action.do_nothing,
            self.action.message_only,
            self.action.kill_warp_only,
            self.action.kill_warp,
            self.action.pause_game,
        ):
            alarm.action = action
            self.assertEqual(action, alarm.action)
        # Custom and Converted describe action states that cannot be set
        # directly; setting them is ignored by the mod
        alarm.action = self.action.pause_game
        alarm.action = self.action.custom
        self.assertEqual(self.action.pause_game, alarm.action)
        alarm.action = self.action.converted
        self.assertEqual(self.action.pause_game, alarm.action)

    def test_property_type_readonly(self):
        alarm = self.create()
        with self.assertRaises(AttributeError):
            alarm.type = self.type.maneuver

    def test_property_vessel(self):
        alarm = self.create()
        vessel = self.sc.active_vessel
        alarm.vessel = vessel
        self.assertEqual(vessel, alarm.vessel)

    def test_property_xfer_bodies(self):
        alarm = self.create(self.type.transfer, "test_xfer")
        kerbin = self.sc.bodies["Kerbin"]
        duna = self.sc.bodies["Duna"]
        alarm.xfer_origin_body = kerbin
        alarm.xfer_target_body = duna
        self.assertEqual(kerbin, alarm.xfer_origin_body)
        self.assertEqual(duna, alarm.xfer_target_body)

    def test_triggered(self):
        alarm = self.create(name="test_triggered", offset=5)
        alarm.action = self.action.do_nothing
        self.assertFalse(alarm.triggered)
        self.wait_until(lambda: alarm.triggered, timeout=60, message="alarm to fire")
        self.assertTrue(alarm.triggered)

    def test_enum_values(self):
        self.assertEqual(
            {
                "raw",
                "maneuver",
                "maneuver_auto",
                "apoapsis",
                "periapsis",
                "ascending_node",
                "descending_node",
                "closest",
                "contract",
                "contract_auto",
                "crew",
                "distance",
                "earth_time",
                "launch_rendevous",
                "soi_change",
                "soi_change_auto",
                "transfer",
                "transfer_modelled",
                "science_lab",
            },
            {x.name for x in self.type},
        )
        self.assertEqual(
            {
                "do_nothing",
                "do_nothing_delete_when_passed",
                "kill_warp",
                "kill_warp_only",
                "message_only",
                "pause_game",
                "custom",
                "converted",
            },
            {x.name for x in self.action},
        )

    def test_remove(self):
        alarm = self.create(name="test_remove")
        self.assertIn(alarm, self.kac.alarms)
        alarm.remove()
        self.assertNotIn(alarm, self.kac.alarms)
        self.assertIsNone(self.kac.alarm_with_name("test_remove"))

    def test_access_after_remove_raises(self):
        alarm = self.create()
        alarm.remove()
        with self.assertRaises(RuntimeError):
            _ = alarm.name

    def test_remove_twice_raises(self):
        alarm = self.create()
        alarm.remove()
        with self.assertRaises(RuntimeError):
            alarm.remove()


if __name__ == "__main__":
    unittest.main()
