import unittest
import krpctest


class TestPartsExperimentDMagic(krpctest.TestCase):

    # Exercises kRPC's support for science experiments backed by DMagic's
    # DMModuleScienceAnimateGeneric, which subclasses ModuleScienceExperiment but stores its
    # results in its own IScienceDataContainer. The dmagicSensorTest part (a thermometer running
    # temperatureScan through that module) is added by tools/mods/config/DMagicScienceAnimate when
    # the DMagic mod is installed. Part lookups use the language-independent with_name/part.name.

    mods = ["DMagic"]

    def setUp(self):
        self.new_save("krpctest_career", always_load=True)
        self.launch_vessel_from_vab("PartsExperimentDMagic")
        self.remove_other_vessels()
        self.sc = self.connect().space_center
        self.parts = self.sc.active_vessel.parts
        self.experiment = self.parts.with_name("dmagicSensorTest")[0].experiment

    def test_experiment(self):
        self.assertEqual("temperatureScan", self.experiment.name)
        self.assertFalse(self.experiment.deployed)
        self.assertTrue(self.experiment.rerunnable)
        self.assertFalse(self.experiment.inoperable)
        self.assertFalse(self.experiment.has_data)
        self.assertCountEqual([], self.experiment.data)
        self.assertTrue(self.experiment.available)
        self.assertEqual("LaunchPad", self.experiment.biome)

    def test_science_subject(self):
        subject = self.experiment.science_subject
        self.assertIsNotNone(subject)
        # is_complete reflects the subject being fully mined (banked science reaching the cap),
        # which is only credited after transmission/recovery, so it is false for a fresh subject.
        self.assertFalse(subject.is_complete)
        self.assertAlmostEqual(0, subject.science)
        self.assertGreater(subject.science_cap, 0)

    def test_run_and_dump_data(self):
        self.assertFalse(self.experiment.has_data)

        self.experiment.run()
        self.wait()

        self.assertTrue(self.experiment.deployed)
        self.assertTrue(self.experiment.has_data)
        self.assertGreater(len(self.experiment.data), 0)

        self.experiment.dump()
        self.wait()

        self.assertFalse(self.experiment.has_data)
        self.assertCountEqual([], self.experiment.data)

    def test_run_reset_run(self):
        # Regression for issue #550: reset() must clear the DMagic data container so the
        # experiment can be run again without an intervening dump(). Before the fix, reset() only
        # cleared the base ModuleScienceExperiment slot, leaving the DMagic container populated, so
        # has_data stayed true and the second run() raised "Experiment already contains data".
        self.experiment.run()
        self.wait()
        self.assertTrue(self.experiment.has_data)

        self.experiment.reset()
        self.wait()
        self.assertFalse(self.experiment.has_data)
        self.assertCountEqual([], self.experiment.data)

        self.experiment.run()
        self.wait()
        self.assertTrue(self.experiment.has_data)


if __name__ == "__main__":
    unittest.main()
