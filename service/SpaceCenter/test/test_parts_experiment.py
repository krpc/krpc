import unittest
import krpctest


class TestPartsExperiment(krpctest.TestCase):

    def setUp(self):
        self.new_save('krpctest_career', always_load=True)
        self.launch_vessel_from_vab('PartsExperiment')
        self.remove_other_vessels()
        self.sc = self.connect().space_center
        self.parts = self.sc.active_vessel.parts
        self.pod = self.parts.with_name('mk1pod')[0].experiment
        self.goo = self.parts.with_name('GooExperiment')[0].experiment

    def test_experiments(self):
        self.assertItemsEqual(
            ['crewReport', 'crewReport', 'mysteryGoo'],
            [x.name for x in self.parts.experiments])

    def test_pod(self):
        self.assertEqual('crewReport', self.pod.name)
        self.assertEqual('Crew Report', self.pod.title)
        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)
        self.assertEqual(True, self.pod.available)
        self.assertEqual('LaunchPad', self.pod.biome)
        subject = self.pod.science_subject
        self.assertAlmostEqual(0, subject.science)
        self.assertAlmostEqual(1.5, subject.science_cap)
        self.assertFalse(subject.is_complete)
        self.assertAlmostEqual(1.0, subject.data_scale)
        self.assertAlmostEqual(1.0, subject.scientific_value)
        self.assertAlmostEqual(0.3, subject.subject_value)
        self.assertEqual('Crew Report from LaunchPad', subject.title)

    def test_goo_container(self):
        self.assertEqual('mysteryGoo', self.goo.name)
        self.assertEqual('Mystery Goo\\u2122 Observation', self.goo.title)
        self.assertFalse(self.goo.deployed)
        self.assertFalse(self.goo.rerunnable)
        self.assertFalse(self.goo.inoperable)
        self.assertFalse(self.goo.has_data)
        self.assertItemsEqual([], self.goo.data)
        subject = self.goo.science_subject
        self.assertAlmostEqual(0, subject.science)
        self.assertAlmostEqual(3.9, subject.science_cap, places=4)
        self.assertFalse(subject.is_complete)
        self.assertAlmostEqual(1.0, subject.data_scale)
        self.assertAlmostEqual(1.0, subject.scientific_value)
        self.assertAlmostEqual(0.3, subject.subject_value)
        self.assertEqual(
            'Mystery Goo\\u2122 Observation from LaunchPad',
            subject.title)

    def test_run_and_dump_data(self):
        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

        self.pod.run()
        self.wait()

        self.assertTrue(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertTrue(self.pod.has_data)

        self.assertItemsEqual([5], [x.data_amount for x in self.pod.data])
        self.assertItemsEqual([1.5], [x.science_value for x in self.pod.data])
        self.assertItemsEqual([1.5], [x.transmit_value for x in self.pod.data])

        self.pod.dump()
        self.wait()

        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

    def test_run_and_transmit_data(self):
        self.assertEqual(7, self.sc.science)

        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

        self.pod.run()
        self.wait()

        self.assertTrue(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertTrue(self.pod.has_data)

        self.assertItemsEqual([5], [x.data_amount for x in self.pod.data])
        self.assertItemsEqual([1.5], [x.science_value for x in self.pod.data])
        self.assertItemsEqual([1.5], [x.transmit_value for x in self.pod.data])

        self.pod.transmit()
        self.wait()

        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

        self.assertEqual(7, self.sc.science)
        self.wait(5)
        self.assertEqual(8.5, self.sc.science)

    def test_run_twice_failure(self):
        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

        self.pod.run()
        self.wait()

        self.assertTrue(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertTrue(self.pod.has_data)

        self.assertRaises(RuntimeError, self.pod.run)

        self.pod.dump()
        self.wait()

        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

    def test_run_twice_success(self):
        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

        for _ in range(2):
            self.pod.run()
            self.wait()

            self.assertTrue(self.pod.deployed)
            self.assertTrue(self.pod.rerunnable)
            self.assertFalse(self.pod.inoperable)
            self.assertTrue(self.pod.has_data)

            self.pod.dump()
            self.wait()

            self.assertFalse(self.pod.deployed)
            self.assertTrue(self.pod.rerunnable)
            self.assertFalse(self.pod.inoperable)
            self.assertFalse(self.pod.has_data)
            self.assertItemsEqual([], self.pod.data)

            self.pod.reset()
            self.wait()

            self.assertFalse(self.pod.deployed)
            self.assertTrue(self.pod.rerunnable)
            self.assertFalse(self.pod.inoperable)
            self.assertFalse(self.pod.has_data)
            self.assertItemsEqual([], self.pod.data)


if __name__ == '__main__':
    unittest.main()
