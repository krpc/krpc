import unittest

import krpctest


class TestAlarm(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center
        cls.alarm_manager = cls.space_center.alarm_manager
        cls.destroyed = cls.conn.krpc.ObjectDestroyedException

    def tearDown(self):
        for alarm in list(self.alarm_manager.alarms):
            alarm.remove()

    def test_remove_alarm(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_remove_alarm", "to be removed")
        self.assertIn(alarm, self.alarm_manager.alarms)
        alarm.remove()
        self.assertNotIn(alarm, self.alarm_manager.alarms)

    def test_access_after_remove_raises(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_access_after_remove", "")
        alarm.remove()
        # The game no longer has an alarm with the id, so the object says the alarm is
        # gone rather than that the call was invalid.
        with self.assertRaises(self.destroyed):
            _ = alarm.title

    def test_remove_twice_raises(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_remove_twice", "")
        alarm.remove()
        with self.assertRaises(self.destroyed):
            alarm.remove()

    def test_alarm_survives_a_quickload(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_quickload", "notes")
        self.space_center.quicksave()
        self.wait(1)
        self.space_center.quickload()
        self.wait(1)
        # Loading a game state builds its alarms again. The object names the alarm by
        # the id the game writes into the save, so it reads the one that was built.
        self.assertEqual("test_quickload", alarm.title)
        self.assertEqual("notes", alarm.description)

    def test_alarm_with_name_after_remove(self):
        alarm = self.alarm_manager.add_alarm(
            3600, "test_alarm_with_name_after_remove", ""
        )
        self.assertIsNotNone(
            self.alarm_manager.alarm_with_name("test_alarm_with_name_after_remove")
        )
        alarm.remove()
        self.assertIsNone(
            self.alarm_manager.alarm_with_name("test_alarm_with_name_after_remove")
        )

    def test_alarms_list_stable_after_add_and_remove(self):
        a1 = self.alarm_manager.add_alarm(3600, "regression_a1", "")
        a2 = self.alarm_manager.add_alarm(3600, "regression_a2", "")
        for _ in range(3):
            self.assertEqual(2, len(self.alarm_manager.alarms))
        a1.remove()
        for _ in range(3):
            names = [a.title for a in self.alarm_manager.alarms]
            self.assertIn("regression_a2", names)
        a2.remove()

    def test_trigger_state_initially_false(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_trigger_state", "")
        self.assertFalse(alarm.triggered)
        self.assertFalse(alarm.actioned)

    def test_typed_property_on_wrong_alarm_raises(self):
        alarm = self.alarm_manager.add_alarm(3600, "test_wrong_type", "")
        with self.assertRaises(RuntimeError):
            _ = alarm.node
        with self.assertRaises(RuntimeError):
            _ = alarm.origin_body
        with self.assertRaises(RuntimeError):
            _ = alarm.destination_body


class TestAlarmEventOffset(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        # The game only adds a burn time to a maneuver alarm for a vessel with an engine
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        conn = cls.connect()
        cls.set_orbit("Kerbin", 1000000, 0.1, 0, 0, 0, 1, conn.space_center.ut)
        cls.conn = conn
        cls.space_center = cls.conn.space_center
        cls.alarm_manager = cls.space_center.alarm_manager
        cls.vessel = cls.space_center.active_vessel

    def tearDown(self):
        for alarm in list(self.alarm_manager.alarms):
            alarm.remove()
        self.vessel.control.remove_nodes()

    def check(self, alarm, event_time, offset):
        # The game computes the burn start of a maneuver from the previous frame
        self.assertAlmostEqual(offset, alarm.event_offset, delta=0.1)
        self.assertAlmostEqual(event_time(), alarm.time + alarm.event_offset, delta=1)

    def check_offset(self, alarm, event_time):
        # The game recomputes the alarm time from the margin on its next update
        self.wait()
        self.check(alarm, event_time, 300)
        alarm.event_offset = 500
        self.check(alarm, event_time, 500)
        self.wait()
        self.check(alarm, event_time, 500)

    def test_apoapsis_alarm(self):
        orbit = self.vessel.orbit
        alarm = self.alarm_manager.add_apoapsis_alarm(self.vessel, offset=300)
        self.check_offset(alarm, lambda: self.space_center.ut + orbit.time_to_apoapsis)

    def test_periapsis_alarm(self):
        orbit = self.vessel.orbit
        alarm = self.alarm_manager.add_periapsis_alarm(self.vessel, offset=300)
        self.check_offset(alarm, lambda: self.space_center.ut + orbit.time_to_periapsis)

    def test_maneuver_node_alarm(self):
        node = self.vessel.control.add_node(self.space_center.ut + 3600)
        alarm = self.alarm_manager.add_maneuver_node_alarm(
            self.vessel, node, offset=300
        )
        self.check(alarm, lambda: node.ut, 300)
        self.check_offset(alarm, lambda: node.ut)

    def test_maneuver_node_alarm_without_burn_time(self):
        node = self.vessel.control.add_node(self.space_center.ut + 3600, prograde=100)
        alarm = self.alarm_manager.add_maneuver_node_alarm(
            self.vessel, node, offset=300, add_burn_time=False
        )
        self.check_offset(alarm, lambda: node.ut)

    def test_maneuver_node_alarm_with_burn_time(self):
        node = self.vessel.control.add_node(self.space_center.ut + 3600, prograde=100)
        alarm = self.alarm_manager.add_maneuver_node_alarm(
            self.vessel, node, offset=300, add_burn_time=True
        )
        self.wait()
        # The event offset includes half the burn time
        burn = alarm.event_offset - 300
        self.assertGreater(burn, 0.5)
        self.assertAlmostEqual(node.ut, alarm.time + alarm.event_offset, delta=1)
        alarm.event_offset = 500
        self.wait()
        self.assertAlmostEqual(500 + burn, alarm.event_offset, delta=0.1)
        self.assertAlmostEqual(node.ut, alarm.time + alarm.event_offset, delta=1)


if __name__ == "__main__":
    unittest.main()
