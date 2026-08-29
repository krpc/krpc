import krpctest

from service.SpaceCenter.test.resources_equivalence import assert_resources_equivalent


class TestStage(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Staging")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.space_center = cls.connect().space_center

    def test_stages_count(self):
        stages = self.vessel.stages
        self.assertGreaterEqual(
            len(stages), 2, "Multi-stage vessel should have multiple activation stages"
        )

    def test_stage_properties(self):
        stage = self.vessel.stages[0]
        self.assertIsInstance(stage.number, int)
        # Delta-v properties should be available on activation stages
        self.assertGreaterEqual(stage.delta_v, 0)
        self.assertGreaterEqual(stage.vacuum_delta_v, 0)
        self.assertGreaterEqual(stage.sea_level_delta_v, 0)
        self.assertGreaterEqual(stage.twr, 0)
        self.assertGreaterEqual(stage.vacuum_twr, 0)
        self.assertGreaterEqual(stage.sea_level_twr, 0)
        self.assertGreaterEqual(stage.thrust, 0)
        self.assertGreaterEqual(stage.vacuum_thrust, 0)
        self.assertGreaterEqual(stage.sea_level_thrust, 0)
        self.assertGreaterEqual(stage.specific_impulse, 0)
        self.assertGreaterEqual(stage.vacuum_specific_impulse, 0)
        self.assertGreaterEqual(stage.sea_level_specific_impulse, 0)
        self.assertGreaterEqual(stage.burn_time, 0)
        self.assertGreaterEqual(stage.start_mass, 0)
        self.assertGreaterEqual(stage.end_mass, 0)
        self.assertGreaterEqual(stage.dry_mass, 0)
        self.assertGreaterEqual(stage.fuel_mass, 0)
        self.assertIsInstance(stage.parts, list)
        resources = stage.resources()
        self.assertIsNotNone(resources)

    def test_decouple_stage_throws(self):
        decouple_stage = self.vessel.decouple_stages[0]
        with self.assertRaises(RuntimeError) as cm:
            _ = decouple_stage.delta_v
        self.assertIn("decouple stage", str(cm.exception).lower())

    def test_vessel_delta_v_aggregates(self):
        self.assertGreaterEqual(self.vessel.delta_v, 0)
        self.assertGreaterEqual(self.vessel.vacuum_delta_v, 0)
        self.assertGreaterEqual(self.vessel.sea_level_delta_v, 0)
        self.assertGreaterEqual(self.vessel.burn_time, 0)

    def test_delta_v_ready(self):
        # The game drops the first calculation of a vessel whose staging is not built yet,
        # and never repeats it. A read asks for another calculation when the game holds no
        # figures, so a figure is available at any point in the flight. The read reports
        # what the game holds, so the figures become current a run later.
        self.assertGreater(self.vessel.vacuum_delta_v, 0)
        self.wait_until(
            lambda: self.vessel.delta_v_ready, message="delta-v figures current"
        )

    def test_recalculate_delta_v(self):
        before = self.vessel.vacuum_delta_v
        self.vessel.recalculate_delta_v()
        self.assertTrue(self.vessel.delta_v_ready)
        self.assertAlmostEqual(before, self.vessel.vacuum_delta_v, delta=1)

    def test_stage_at_and_decouple_stage_at(self):
        stage = self.vessel.stage_at(0)
        self.assertIsNotNone(stage)
        dec_stage = self.vessel.decouple_stage_at(0)
        self.assertIsNotNone(dec_stage)

    def test_legacy_deprecation_compatibility(self):
        # Default migration path: legacy omits cumulative (defaults to True)
        legacy_default = self.vessel.resources_in_decouple_stage(0)
        new_default = self.vessel.decouple_stage_at(0).resources()
        assert_resources_equivalent(self, legacy_default, new_default)

        # Explicit non-cumulative path
        legacy = self.vessel.resources_in_decouple_stage(0, False)
        new_way = self.vessel.decouple_stage_at(0).resources(False)
        assert_resources_equivalent(self, legacy, new_way)


class TestStageRevertToLaunch(krpctest.TestCase):
    # Regression for stages breaking after a Revert to Launch. The revert destroys the
    # vessel and recreates it under the same id, so a stage that holds the vessel object
    # is left pointing at the destroyed one and reports that delta-v has not been
    # calculated. Because stages compare equal on vessel id, stage number and kind, the
    # broken stage stays in the object store and is handed back to every later call, so
    # even a freshly requested stage is affected.

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab("Staging")

    def wait_for_flight(self):
        conn = self.connect()

        def in_flight():
            try:
                return (
                    conn.krpc.game_scene == conn.krpc.GameScene.flight
                    and conn.space_center.active_vessel is not None
                    and conn.space_center.active_vessel.parts.root is not None
                )
            except RuntimeError:
                return False

        self.wait_until(in_flight, timeout=60, message="flight scene after revert")

    def wait_for_delta_v(self, vessel):
        # The recreated vessel builds its delta-v simulation over the first few frames of
        # the reloaded scene, and the game revises its first figures once the vessel has
        # come to rest, so wait for one that stops changing before reading any stage. This
        # uses the vessel-level figure, which resolves the vessel by id and so is
        # unaffected by what this test is checking.
        def settled():
            try:
                first = vessel.vacuum_delta_v
            except RuntimeError:
                return False
            if first <= 0:
                return False
            self.wait(0.5)
            return vessel.vacuum_delta_v == first

        self.wait_until(settled, timeout=30, message="delta-v after revert")

    def stage_state(self, stage, decouple_stage):
        # Everything a stage exposes that has to survive the reload: the delta-v figures
        # the bug report is about, the part list, and the resource collection.
        return (
            round(stage.vacuum_delta_v),
            round(stage.vacuum_thrust),
            len(stage.parts),
            len(decouple_stage.parts),
            sorted(decouple_stage.resources().names),
        )

    def test_stages_after_revert_to_launch(self):
        space_center = self.connect().space_center
        vessel = space_center.active_vessel
        # Pick stages that actually carry the data being checked, so that reading zeros
        # after the revert is a failure rather than the craft's own layout.
        stage = max(vessel.stages, key=lambda s: s.vacuum_delta_v)
        decouple_stage = max(vessel.decouple_stages, key=lambda s: len(s.parts))
        stage_number = stage.number
        decouple_stage_number = decouple_stage.number
        before = self.stage_state(stage, decouple_stage)
        self.assertGreater(before[0], 0)
        self.assertGreater(before[2], 0)
        self.assertGreater(before[3], 0)

        self.assertTrue(space_center.can_revert_to_launch)
        space_center.revert_to_launch()
        self.wait_for_flight()

        space_center = self.connect().space_center
        vessel = space_center.active_vessel
        self.wait_for_delta_v(vessel)

        # Stage handles kept across the reload, as a client that does not reconnect has.
        self.assertEqual(before, self.stage_state(stage, decouple_stage))

        # And stages requested after the reload, which the object store hands back as the
        # same instances because they compare equal to the ones from before it.
        self.assertEqual(
            before,
            self.stage_state(
                vessel.stage_at(stage_number),
                vessel.decouple_stage_at(decouple_stage_number),
            ),
        )


class TestStageDeltaVJettisoned(krpctest.TestCase):
    """The game calculates delta-v for the active vessel alone, so a vessel left behind by
    staging can never report figures and says so."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Staging")
        cls.remove_other_vessels()
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.set_circular_orbit("Kerbin", 250000)
        # The first few stages ignite engines without separating anything, so keep going
        # until one leaves a vessel behind.
        cls.jettisoned = None
        for _ in range(6):
            jettisoned = cls.vessel.control.activate_next_stage()
            cls.wait(1)
            if jettisoned:
                cls.jettisoned = jettisoned[0]
                break
        assert cls.jettisoned is not None, "no stage of the craft left a vessel behind"

    def test_jettisoned_vessel_reports_no_figures(self):
        self.assertFalse(self.jettisoned.delta_v_ready)
        with self.assertRaises(RuntimeError) as cm:
            _ = self.jettisoned.vacuum_delta_v
        self.assertIn("delta-v", str(cm.exception).lower())

    def test_active_vessel_still_reports_figures(self):
        self.assertTrue(self.vessel.delta_v_ready)


class TestStageDeltaVWithoutEngines(krpctest.TestCase):
    """CrewlessCommandPod carries no engine. The game runs its simulation but leaves the
    ready flag down, and the zeros it computed are what there is to report."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.space_center = cls.connect().space_center
        if cls.space_center.get_kerbal("Solo Kerman") is None:
            cls.space_center.create_kerbal("Solo Kerman", "Pilot", True)
        cls._stage_craft("CrewlessCommandPod", "VAB", None)
        cls.space_center.launch_vessel(
            "VAB", "CrewlessCommandPod", "LaunchPad", ["Solo Kerman"]
        )
        cls.remove_other_vessels()
        cls.vessel = cls.space_center.active_vessel

    def test_figures_are_zero(self):
        self.assertTrue(self.vessel.delta_v_ready)
        self.assertEqual(0, self.vessel.vacuum_delta_v)
        self.assertEqual(0, self.vessel.sea_level_delta_v)
        self.assertEqual(0, self.vessel.delta_v)
        self.assertEqual(0, self.vessel.burn_time)
