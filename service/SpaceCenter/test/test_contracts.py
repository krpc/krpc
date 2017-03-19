import unittest
import krpctest


class TestContracts(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save('krpctest_career', always_load=True)
        cls.space_center = cls.connect().space_center
        cls.cm = cls.space_center.contract_manager
        cls.ContractState = cls.space_center.ContractState

    def test_contract(self):
        contract = self.cm.active_contracts[0]
        self.assertEqual(
            'FinePrint.Contracts.ExplorationContract', contract.type)
        self.assertEqual('Orbit Kerbin!', contract.title)
        self.assertEqual(
            'Shortly after disproving untrue theories on how the effects of '
            'being in microgravity were still very much unknown, the chief '
            'book-keeper visiting Kerbin World-Firsts Record-Keeping Society '
            'realized that it was truly hard for us to tell if some myths '
            'about what really happens to things in orbit really were true. '
            'So we now need your help.',
            contract.description)
        self.assertEqual('', contract.notes)
        self.assertEqual('We need to achieve a stable orbit around Kerbin. '
                         'All you should need to do is throw yourself at '
                         'the ground and miss.', contract.synopsis)
        self.assertItemsEqual([], contract.keywords)
        self.assertEqual(self.ContractState.active, contract.state)
        self.assertTrue(contract.seen)
        self.assertTrue(contract.read)
        self.assertTrue(contract.active)
        self.assertFalse(contract.failed)
        self.assertFalse(contract.can_be_canceled)
        self.assertFalse(contract.can_be_declined)
        self.assertFalse(contract.can_be_failed)
        self.assertEquals(16800, contract.funds_advance)
        self.assertEquals(31200, contract.funds_completion)
        self.assertEquals(16800, contract.funds_failure)
        self.assertEquals(10, contract.reputation_completion)
        self.assertEquals(0, contract.reputation_failure)
        self.assertEquals(6, contract.science_completion)
        self.assertEqual(1, len(contract.parameters))
        parameter = contract.parameters[0]
        self.assertEqual('Achieve goal', parameter.title)
        self.assertEqual(
            'Fly a vessel up and out of the atmosphere and accelerate '
            'parallel with the surface until you are in a stable orbit to '
            'achieve this goal.',
            parameter.notes)
        self.assertItemsEqual([], parameter.children)
        self.assertFalse(parameter.completed)
        self.assertFalse(parameter.failed)
        self.assertFalse(parameter.optional)
        self.assertEquals(48000, parameter.funds_completion)
        self.assertEquals(0, parameter.funds_failure)
        self.assertEquals(10, parameter.reputation_completion)
        self.assertEquals(0, parameter.reputation_failure)
        self.assertEquals(6, parameter.science_completion)

    def test_child_parameters(self):
        contract = self.cm.all_contracts[6]
        self.assertEqual('Test RT-5 "Flea" Solid Fuel Booster '
                         'at the Launch Site.', contract.title)
        self.assertEqual(1, len(contract.parameters))
        parameter = contract.parameters[0]
        self.assertEqual(
            'Test RT-5 "Flea" Solid Fuel Booster', parameter.title)
        self.assertEqual(2, len(parameter.children))
        self.assertEqual(['Kerbin', 'Launch Site'],
                         [x.title for x in parameter.children])

    def test_types(self):
        self.assertItemsEqual(
            ['FinePrint.Contracts.ISRUContract',
             'FinePrint.Contracts.ExplorationContract',
             'Contracts.Templates.PlantFlag',
             'FinePrint.Contracts.BaseContract',
             'Contracts.Templates.RecoverAsset',
             'Contracts.Templates.GrandTour',
             'FinePrint.Contracts.ARMContract',
             'FinePrint.Contracts.SatelliteContract',
             'FinePrint.Contracts.StationContract',
             'Contracts.Templates.CollectScience',
             'FinePrint.Contracts.SurveyContract',
             'FinePrint.Contracts.TourismContract',
             'Contracts.Templates.PartTest'],
            self.cm.types)

    def test_all_contracts(self):
        self.assertItemsEqual(
            ['Conduct a focused observational survey of Kerbin.']*5 +
            ['Gather scientific data from Kerbin.',
             'Test RT-5 "Flea" Solid Fuel Booster at the Launch Site.',
             'Escape the atmosphere!',
             'Haul Mk16 Parachute into flight above Kerbin.',
             'Haul RT-10 "Hammer" Solid Fuel Booster '
             'into flight above Kerbin.',
             'Orbit Kerbin!',
             'Launch our first vessel!',
             'Test Mk16 Parachute in flight over Kerbin.',
             'Test RT-10 "Hammer" Solid Fuel Booster at the Launch Site.'],
            [x.title for x in self.cm.all_contracts])

    def test_active_contracts(self):
        contracts = self.cm.active_contracts
        self.assertItemsEqual(
            ['Orbit Kerbin!'],
            [x.title for x in contracts])
        contract = contracts[0]
        self.assertEqual(
            'FinePrint.Contracts.ExplorationContract', contract.type)
        self.assertEqual('Orbit Kerbin!', contract.title)
        self.assertEqual(self.ContractState.active, contract.state)
        self.assertTrue(contract.seen)
        self.assertTrue(contract.read)
        self.assertTrue(contract.active)
        self.assertFalse(contract.failed)
        self.assertFalse(contract.can_be_canceled)
        self.assertFalse(contract.can_be_declined)
        self.assertFalse(contract.can_be_failed)

    def test_offered_contracts(self):
        contracts = self.cm.offered_contracts
        self.assertItemsEqual(
            ['Conduct a focused observational survey of Kerbin.']*5 +
            ['Gather scientific data from Kerbin.',
             'Test RT-5 "Flea" Solid Fuel Booster at the Launch Site.',
             'Escape the atmosphere!',
             'Haul Mk16 Parachute into flight above Kerbin.',
             'Haul RT-10 "Hammer" Solid Fuel Booster into '
             'flight above Kerbin.'],
            [x.title for x in contracts])
        contract = contracts[0]
        self.assertEqual('FinePrint.Contracts.SurveyContract', contract.type)
        self.assertEqual('Conduct a focused observational survey of Kerbin.',
                         contract.title)
        self.assertEqual(self.ContractState.offered, contract.state)
        self.assertTrue(contract.seen)
        self.assertFalse(contract.read)
        self.assertFalse(contract.active)
        self.assertFalse(contract.failed)
        self.assertTrue(contract.can_be_canceled)
        self.assertTrue(contract.can_be_declined)
        self.assertTrue(contract.can_be_failed)

    def test_completed_contracts(self):
        contracts = self.cm.completed_contracts
        self.assertItemsEqual(
            ['Launch our first vessel!'],
            [x.title for x in contracts])
        contract = contracts[0]
        self.assertEqual(
            'FinePrint.Contracts.ExplorationContract', contract.type)
        self.assertEqual('Launch our first vessel!', contract.title)
        self.assertEqual(self.ContractState.completed, contract.state)
        self.assertTrue(contract.seen)
        self.assertTrue(contract.read)
        self.assertFalse(contract.active)
        self.assertFalse(contract.failed)
        self.assertFalse(contract.can_be_canceled)
        self.assertFalse(contract.can_be_declined)
        self.assertFalse(contract.can_be_failed)

    def test_failed_contracts(self):
        # TODO: fail a contract to test this
        self.assertItemsEqual(
            [],
            [x.title for x in self.cm.failed_contracts])

if __name__ == '__main__':
    unittest.main()
