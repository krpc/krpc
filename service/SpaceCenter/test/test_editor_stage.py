import time
import unittest

import krpctest


class TestEditorStage(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Staging")
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.editor.vessel
        cls.wait_for_delta_v(cls.vessel)

    @classmethod
    def tearDownClass(cls):
        cls.leave_editor()

    @classmethod
    def wait_for_delta_v(cls, vessel, timeout=30):
        """Wait until the delta-v figures are current. They are recalculated in the
        background whenever the vessel or the situation it assumes changes."""
        deadline = time.time() + timeout
        while not vessel.delta_v_ready:
            if time.time() > deadline:
                raise RuntimeError("Timed out waiting for delta-v to be calculated")
            time.sleep(0.1)

    def test_totals(self):
        self.assertGreater(self.vessel.vacuum_delta_v, 0)
        self.assertGreater(self.vessel.sea_level_delta_v, 0)
        self.assertGreater(self.vessel.vacuum_delta_v, self.vessel.sea_level_delta_v)
        self.assertGreater(self.vessel.burn_time, 0)

    def test_stages(self):
        stages = self.vessel.stages
        self.assertGreater(len(stages), 0)
        self.assertEqual(
            sorted(stage.number for stage in stages), [stage.number for stage in stages]
        )
        burning = 0
        for stage in stages:
            self.assertGreaterEqual(stage.vacuum_delta_v, 0)
            self.assertGreaterEqual(stage.vacuum_thrust, 0)
            self.assertGreaterEqual(stage.start_mass, stage.end_mass)
            if stage.vacuum_delta_v > 0:
                burning += 1
                self.assertGreater(stage.vacuum_thrust, 0)
                self.assertGreater(stage.vacuum_specific_impulse, 0)
                self.assertGreater(stage.burn_time, 0)
        self.assertGreater(burning, 0)

    def test_stage_at(self):
        stage = self.vessel.stages[0]
        self.assertEqual(stage, self.vessel.stage_at(stage.number))
        self.assertRaises(ValueError, self.vessel.stage_at, 1000)

    def test_stage_parts(self):
        parts = self.vessel.stages[0].parts
        self.assertGreater(len(parts), 0)
        for part in parts:
            self.assertEqual(self.vessel.stages[0].number, part.stage)

    def test_decouple_stages(self):
        stages = self.vessel.decouple_stages
        self.assertGreater(len(stages), 0)
        # Stock delta-v data does not apply to decouple stages.
        self.assertRaises(RuntimeError, getattr, stages[0], "vacuum_delta_v")
        self.assertGreater(len(stages[-1].parts), 0)

    def test_stage_resources_are_flight_only(self):
        self.assertRaises(RuntimeError, self.ship.stages[0].resources)

    def burn_stage(self):
        # The stage with the most vacuum delta-v, so there is something to move.
        return max(self.vessel.stages, key=lambda stage: stage.vacuum_delta_v)

    def test_altitude_moves_the_figures_from_sea_level_to_vacuum(self):
        editor = self.space_center.editor
        altitude = editor.delta_v_altitude
        stage = self.burn_stage()
        try:
            editor.delta_v_altitude = 0
            self.wait_for_delta_v(self.vessel)
            self.assertAlmostEqual(
                self.vessel.sea_level_delta_v, self.vessel.delta_v, delta=1
            )
            self.assertAlmostEqual(
                stage.sea_level_specific_impulse, stage.specific_impulse, delta=0.1
            )

            # Above Kerbin's atmosphere, which ends at 70km.
            editor.delta_v_altitude = 70000
            self.wait_for_delta_v(self.vessel)
            self.assertAlmostEqual(
                self.vessel.vacuum_delta_v, self.vessel.delta_v, delta=1
            )
            self.assertAlmostEqual(
                stage.vacuum_specific_impulse, stage.specific_impulse, delta=0.1
            )

            # In between, and rising with altitude.
            editor.delta_v_altitude = 10000
            self.wait_for_delta_v(self.vessel)
            lower = stage.specific_impulse
            editor.delta_v_altitude = 30000
            self.wait_for_delta_v(self.vessel)
            higher = stage.specific_impulse
            self.assertLess(stage.sea_level_specific_impulse, lower)
            self.assertLess(lower, higher)
            self.assertLess(higher, stage.vacuum_specific_impulse)
        finally:
            editor.delta_v_altitude = altitude
            self.wait_for_delta_v(self.vessel)

    def test_body_sets_the_gravity_and_the_atmosphere(self):
        editor = self.space_center.editor
        body = editor.delta_v_body
        altitude = editor.delta_v_altitude
        stage = self.burn_stage()
        try:
            editor.delta_v_altitude = 0
            editor.delta_v_body = self.space_center.bodies["Kerbin"]
            self.wait_for_delta_v(self.vessel)
            kerbin_twr = stage.twr
            kerbin_isp = stage.specific_impulse

            # The Mun has no atmosphere and a sixth of Kerbin's surface gravity, so at
            # its surface the engines reach their vacuum performance and weigh less.
            editor.delta_v_body = self.space_center.bodies["Mun"]
            self.wait_for_delta_v(self.vessel)
            self.assertAlmostEqual(
                stage.vacuum_specific_impulse, stage.specific_impulse, delta=0.1
            )
            self.assertGreater(stage.twr, kerbin_twr)

            # Eve's atmosphere is far thicker than Kerbin's, which throttles the
            # engines' specific impulse right down.
            editor.delta_v_body = self.space_center.bodies["Eve"]
            self.wait_for_delta_v(self.vessel)
            self.assertLess(stage.specific_impulse, kerbin_isp)
        finally:
            editor.delta_v_body = body
            editor.delta_v_altitude = altitude
            self.wait_for_delta_v(self.vessel)
        self.assertEqual("Kerbin", editor.delta_v_body.name)

    def test_setting_the_situation_marks_the_figures_out_of_date(self):
        # The setter returns straight away, and the readiness flag reports that the
        # figures it asked for are not there yet.
        editor = self.space_center.editor
        altitude = editor.delta_v_altitude
        try:
            editor.delta_v_altitude = 0
            self.wait_for_delta_v(self.vessel)
            editor.delta_v_altitude = 60000
            self.assertFalse(self.vessel.delta_v_ready)
            self.assertRaises(RuntimeError, getattr, self.vessel, "delta_v")
            self.wait_for_delta_v(self.vessel)
            self.assertGreater(self.vessel.delta_v, self.vessel.sea_level_delta_v)
        finally:
            editor.delta_v_altitude = altitude
            self.wait_for_delta_v(self.vessel)

    def test_situation_only_selects_what_the_game_displays(self):
        # The situation picks the column the game's own delta-v app shows. It does not
        # feed the simulation, so nothing kRPC reports moves with it.
        editor = self.space_center.editor
        situations = self.space_center.DeltaVSituation
        situation = editor.delta_v_situation
        altitude = editor.delta_v_altitude
        stage = self.burn_stage()
        try:
            editor.delta_v_altitude = 30000
            self.wait_for_delta_v(self.vessel)
            editor.delta_v_situation = situations.sea_level
            expected = (self.vessel.delta_v, stage.delta_v, stage.specific_impulse)
            for name in ("altitude", "vacuum", "sea_level"):
                editor.delta_v_situation = getattr(situations, name)
                self.assertEqual(getattr(situations, name), editor.delta_v_situation)
                self.assertEqual(
                    expected,
                    (self.vessel.delta_v, stage.delta_v, stage.specific_impulse),
                )
        finally:
            editor.delta_v_situation = situation
            editor.delta_v_altitude = altitude
            self.wait_for_delta_v(self.vessel)


class TestEditorStageMatchesFlight(krpctest.TestCase):
    """The same vessel must report the same per-stage vacuum delta-v in the editor and
    once it has been launched."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.enter_editor("VAB", craft="Staging")
        vessel = cls.connect().space_center.editor.vessel
        TestEditorStage.wait_for_delta_v(vessel)
        cls.editor_delta_v = [stage.vacuum_delta_v for stage in vessel.stages]
        cls.editor_numbers = [stage.number for stage in vessel.stages]
        cls.leave_editor()
        cls.launch_vessel_from_vab("Staging")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel

    def test_same_stages(self):
        self.assertEqual(
            self.editor_numbers, [stage.number for stage in self.vessel.stages]
        )

    def test_same_vacuum_delta_v(self):
        flight = [stage.vacuum_delta_v for stage in self.vessel.stages]
        self.assertEqual(len(self.editor_delta_v), len(flight))
        for expected, actual in zip(self.editor_delta_v, flight):
            self.assertAlmostEqual(expected, actual, delta=max(1, expected * 0.01))


if __name__ == "__main__":
    unittest.main()
