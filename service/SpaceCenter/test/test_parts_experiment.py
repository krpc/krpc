import unittest
import krpctest
import krpc

class TestPartsExperiment(krpctest.TestCase):

    def setUp(self):
        self.new_save()
        self.launch_vessel_from_vab('PartsExperiment')
        self.remove_other_vessels()
        parts = self.connect().space_center.active_vessel.parts
        self.pod = parts.with_title('Mk1 Command Pod')[0].experiment
        self.goo = [x for x in parts.all if x.name == 'GooExperiment'][0].experiment

    def test_pod(self):
        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

    def test_goo_container(self):
        self.assertFalse(self.goo.deployed)
        self.assertFalse(self.goo.rerunnable)
        self.assertFalse(self.goo.inoperable)
        self.assertFalse(self.goo.has_data)
        self.assertItemsEqual([], self.goo.data)

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
        self.assertItemsEqual([0], [x.science_value for x in self.pod.data])
        self.assertItemsEqual([0], [x.transmit_value for x in self.pod.data])

        self.pod.dump()
        self.wait()

        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

    def test_run_and_transmit_data(self):
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
        self.assertItemsEqual([0], [x.science_value for x in self.pod.data])
        self.assertItemsEqual([0], [x.transmit_value for x in self.pod.data])

        self.pod.transmit()
        self.wait()

        self.assertFalse(self.pod.deployed)
        self.assertTrue(self.pod.rerunnable)
        self.assertFalse(self.pod.inoperable)
        self.assertFalse(self.pod.has_data)
        self.assertItemsEqual([], self.pod.data)

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

        self.assertRaises(krpc.client.RPCError, self.pod.run)

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
