"""Where in a physics tick the auto-pilot's control loop runs, relative to the calls a
program makes in that tick.

The diagnostic log commits one row per tick the loop ran on, stamped with that tick. Reading
it from inside a held tick says whether the loop has already run for the tick the program is
holding, which is the whole question. The stamp is the game's fixed clock rather than
universal time, so the two are tied together first in manual mode, where one update inside a
held tick commits exactly one row and that row is that tick's.
"""

import unittest

import krpctest

# The game's physics time step.
TICK = 0.02


def parse(text):
    """The diagnostic log as (tick, target pitch) pairs, oldest first."""
    lines = [line for line in text.splitlines() if line.strip()]
    header = lines[0].split(",")
    tick = header.index("t")
    pitch = header.index("tgt.pitch")
    return [
        (float(row.split(",")[tick]), float(row.split(",")[pitch])) for row in lines[1:]
    ]


class TestAutoPilotUpdateMode(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab("AutoPilot")
        # In orbit: the loop writes no rows while the vessel is held on the launch clamps.
        cls.set_orbit("Eve", 1070000, 0.15, 16.2, 70.5, 180.8, 1.83, 251.1)
        cls.conn = cls.connect()
        cls.space_center = cls.conn.space_center
        cls.vessel = cls.space_center.active_vessel
        cls.ap = cls.vessel.auto_pilot
        cls.mode = cls.space_center.AutoPilotUpdateMode

    def setUp(self):
        self.ap.reset()
        self.ap.sas = False
        self.ap.reference_frame = self.vessel.surface_reference_frame
        self.ap.target_pitch_and_heading(0, 90)
        self.ap.engaged = True
        self.wait(0.5)

    def tearDown(self):
        self.ap.diagnostic_logging = False
        self.ap.engaged = False
        # A test that left the auto-pilot in manual mode must not leave it there for the next.
        self.ap.update_mode = self.mode.after_calls
        self.conn.krpc.release_tick()

    def tick_offset(self):
        """Universal time minus the log's tick stamp, for one and the same tick.

        Calibrated in manual mode, where the single row an update commits inside a held tick
        is that tick's by construction, whatever the modes under test do.
        """
        self.ap.update_mode = self.mode.manual
        self.ap.diagnostic_logging = True
        self.conn.krpc.hold_tick()
        try:
            ut = self.space_center.ut
            self.ap.update()
        finally:
            self.conn.krpc.release_tick()
        rows = parse(self.ap.diagnostic_log)
        self.ap.diagnostic_logging = False
        self.assertEqual(1, len(rows))
        return ut - rows[0][0]

    def hold_a_tick_and_write(self, offset, pitch):
        """Set a target inside one held tick. Returns whether the loop had already run for
        that tick, and the log as it stood once several more ticks had passed."""
        self.ap.diagnostic_logging = True
        self.wait(0.25)
        self.conn.krpc.hold_tick()
        try:
            ut = self.space_center.ut
            during = parse(self.ap.diagnostic_log)
            self.ap.target_pitch_and_heading(pitch, 90)
        finally:
            self.conn.krpc.release_tick()
        self.wait(0.25)
        after = parse(self.ap.diagnostic_log)
        self.ap.diagnostic_logging = False
        already_run = abs(during[-1][0] - (ut - offset)) < TICK / 2
        return already_run, during, after

    def test_after_calls_runs_the_loop_once_the_tick_s_calls_are_done(self):
        offset = self.tick_offset()
        self.ap.update_mode = self.mode.after_calls
        already_run, during, after = self.hold_a_tick_and_write(offset, 20)
        # The loop has not run for the held tick, so its row is the next one committed, and it
        # carries the target the hold wrote: a target set in a tick is flown on that tick.
        self.assertFalse(already_run)
        self.assertAlmostEqual(20, after[len(during)][1], places=1)

    def test_before_calls_runs_the_loop_before_the_tick_s_calls(self):
        offset = self.tick_offset()
        self.ap.update_mode = self.mode.before_calls
        already_run, during, after = self.hold_a_tick_and_write(offset, 25)
        # The held tick's row is already committed, and carries the previous target: what is
        # written in a tick is not flown until the next one.
        self.assertTrue(already_run)
        self.assertNotAlmostEqual(25, during[-1][1], places=1)
        self.assertAlmostEqual(25, after[len(during)][1], places=1)

    def test_manual_runs_the_loop_only_when_it_is_updated(self):
        self.ap.update_mode = self.mode.manual
        self.ap.diagnostic_logging = True
        # Several ticks in which the loop is never run.
        self.wait(0.5)
        self.assertEqual("", self.ap.diagnostic_log.strip())

        targets = [30, 35, 40]
        for pitch in targets:
            self.conn.krpc.hold_tick()
            try:
                self.ap.target_pitch_and_heading(pitch, 90)
                self.ap.update()
            finally:
                self.conn.krpc.release_tick()
            self.wait(0.1)
        rows = parse(self.ap.diagnostic_log)
        self.assertEqual(len(targets), len(rows))
        for target, row in zip(targets, rows):
            self.assertAlmostEqual(target, row[1], places=1)

    def test_updating_twice_in_a_tick_runs_the_loop_once(self):
        self.ap.update_mode = self.mode.manual
        self.ap.diagnostic_logging = True
        self.conn.krpc.hold_tick()
        try:
            self.ap.update()
            self.ap.update()
            self.ap.update()
        finally:
            self.conn.krpc.release_tick()
        self.assertEqual(1, len(parse(self.ap.diagnostic_log)))

    def test_an_auto_pilot_that_stops_being_updated_stops_flying(self):
        self.ap.update_mode = self.mode.manual
        self.ap.target_pitch_and_heading(80, 90)
        self.ap.update()
        self.wait(0.05)
        self.assertNotEqual(0, self.vessel.control.pitch)
        # Well past the tenth of a second the last output is held for.
        self.wait(0.5)
        self.assertEqual(0, self.vessel.control.pitch)
        # Not flying it is not the same as giving it up.
        self.assertTrue(self.ap.engaged)

    def test_the_auto_pilot_cannot_be_waited_on_in_manual_mode(self):
        self.ap.update_mode = self.mode.manual
        with self.assertRaises(RuntimeError):
            self.ap.wait()

    def test_updating_an_auto_pilot_that_is_not_engaged_raises(self):
        self.ap.engaged = False
        with self.assertRaises(RuntimeError):
            self.ap.update()


if __name__ == "__main__":
    unittest.main()
