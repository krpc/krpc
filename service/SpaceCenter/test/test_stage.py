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
